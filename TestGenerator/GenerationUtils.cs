using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TestGenerator.MetaData;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace TestGenerator
{
    public static class GenerationUtils
    {
        private static readonly SyntaxToken PublicModifier = Token(SyntaxKind.PublicKeyword);
        private static readonly TypeSyntax VoidReturnType = ParseTypeName("void");
        private static readonly AttributeSyntax SetupAttribute = Attribute(ParseName("SetUp"));
        private static readonly AttributeSyntax MethodAttribute = Attribute(ParseName("Test"));
        private static readonly AttributeSyntax ClassAttribute = Attribute(ParseName("TestFixture"));
        private static readonly ExpressionStatementSyntax FailExpression = CreateFailExpression();

        public static Dictionary<string, string> GenerateTests(FileMD fileMd)
        {
            var fileNameCode = new Dictionary<string, string>();
            foreach (var classInfo in fileMd.Classes)
            {
                var classDeclaration = GenerateClass(classInfo);
                var compilationUnit = CompilationUnit()
                    .AddUsings(UsingDirective(ParseName("System")))
                    .AddUsings(UsingDirective(ParseName("NUnit.Framework")))
                    .AddUsings(UsingDirective(ParseName("Moq")))
                    .AddUsings(UsingDirective(ParseName("System.Collections.Generic")))
                    .AddMembers(classDeclaration);
                fileNameCode.Add(classInfo.ClassName + "Test",
                    compilationUnit.NormalizeWhitespace().ToFullString());
            }

            return fileNameCode;
        }

        private static ClassDeclarationSyntax GenerateClass(ClassMD classMd)
        {
            var fields = new List<FieldDeclarationSyntax>();
            VariableDeclarationSyntax variable;
            ConstructorMD constructor = null;
            if (classMd.Constructors.Count > 0)
            {
                constructor = FindConstructorWithLargestNumOfParams(classMd.Constructors);
                var interfaces = GetCustomTypeVariables(constructor.Parameters);
                foreach (var (key, value) in interfaces)
                {
                    variable = GenerateVariable("_" + key, $"Mock<{value}>");
                    fields.Add(GenerateField(variable));
                }
            }

            variable = GenerateVariable(GetCheckedClassVariable(classMd.ClassName), classMd.ClassName);
            fields.Add(GenerateField(variable));
            var methods = new List<MethodDeclarationSyntax> {GenerateSetUpMethod(constructor, classMd.ClassName)};
            foreach (var methodInfo in classMd.Methods)
            {
                methods.Add(GenerateMethod(methodInfo, classMd.ClassName));
            }

            return ClassDeclaration(classMd.ClassName + "Test")
                .AddMembers(fields.ToArray())
                .AddMembers(methods.ToArray())
                .AddAttributeLists(
                    SyntaxFactory.AttributeList(SyntaxFactory.AttributeList().Attributes.Add(ClassAttribute)));
        }

        private static ConstructorMD FindConstructorWithLargestNumOfParams(List<ConstructorMD> constructors)
        {
            var constructor = constructors[0];
            foreach (var temp in constructors)
            {
                if (constructor.Parameters.Count < temp.Parameters.Count)
                {
                    constructor = temp;
                }
            }

            return constructor;
        }

        private static Dictionary<string, string> GetCustomTypeVariables(Dictionary<string, string> parameters)
        {
            var res = new Dictionary<string, string>();
            foreach (var (key, value) in parameters)
            {
                if (value[0] == 'I')
                {
                    res.Add(key, value);
                }
            }

            return res;
        }

        private static VariableDeclarationSyntax GenerateVariable(string varName, string typeName)
        {
            return VariableDeclaration(ParseTypeName(typeName))
                .AddVariables(VariableDeclarator(varName));
        }

        private static string GetCheckedClassVariable(string className)
        {
            return "_" + className[0].ToString().ToLower() + className.Remove(0, 1);
        }

        private static FieldDeclarationSyntax GenerateField(VariableDeclarationSyntax var)
        {
            return FieldDeclaration(var)
                .AddModifiers(Token(SyntaxKind.PrivateKeyword));
        }

        private static MethodDeclarationSyntax GenerateSetUpMethod(ConstructorMD constructorMd, string className)
        {
            List<StatementSyntax> body = new List<StatementSyntax>();
            if (constructorMd != null)
            {
                var baseTypeVars = GetBaseTypeVariables(constructorMd.Parameters);
                foreach (var (key, value) in baseTypeVars)
                {
                    body.Add(GenerateBasesTypesAssignStatement(key, value));
                }

                var customVars = GetCustomTypeVariables(constructorMd.Parameters);
                foreach (var (key, value) in customVars)
                {
                    body.Add(GenerateCustomsTypesAssignStatement("_" + key, $"Mock<{value}>", ""));
                }
            }

            body.Add(GenerateCustomsTypesAssignStatement(GetCheckedClassVariable(className), className,
                constructorMd != null ? ConvertParametersToStringRepresentation(constructorMd.Parameters) : ""));
            return MethodDeclaration(VoidReturnType, "SetUp")
                .AddModifiers(PublicModifier)
                .AddAttributeLists(AttributeList(AttributeList().Attributes.Add(SetupAttribute)))
                .WithBody(Block(body));
            ;
        }

        private static Dictionary<string, string> GetBaseTypeVariables(Dictionary<string, string> parameters)
        {
            var res = new Dictionary<string, string>();
            foreach (var parameter in parameters)
            {
                if (parameter.Value[0] != 'I')
                {
                    res.Add(parameter.Key, parameter.Value);
                }
            }

            return res;
        }

        private static StatementSyntax GenerateBasesTypesAssignStatement(string varName, string varType)
        {
            return ParseStatement($"var {varName} = default({varType});");
        }

        private static ExpressionStatementSyntax CreateFailExpression()
        {
            return ExpressionStatement(
                InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("Assert"),
                            IdentifierName("Fail")))
                    .WithArgumentList(
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(
                                    LiteralExpression(
                                        SyntaxKind.StringLiteralExpression,
                                        Literal("autogenerated")))))));
        }

        private static string ConvertParametersToStringRepresentation(Dictionary<string, string> parameters)
        {
            var s = "";
            foreach (var pair in parameters)
            {
                s += pair.Value[0] == 'I' ? $"_{pair.Key}.Object" : $"{pair.Key}";
                s += ", ";
            }

            return s.Length > 0 ? s.Remove(s.Length - 2, 2) : "";
        }

        private static StatementSyntax GenerateCustomsTypesAssignStatement(string varName, string constructorName,
            string invokeArgs = "")
        {
            return ParseStatement($"{varName} = new {constructorName}({invokeArgs});");
        }

        private static MethodDeclarationSyntax GenerateMethod(MethodMD methodMd, string checkedClassVar)
        {
            List<StatementSyntax> body = new List<StatementSyntax>();
            GenerateArrangePart(body, methodMd.Parameters);
            GenerateActPart(body, methodMd, checkedClassVar);
            if (methodMd.ReturnType != "void")
            {
                GenerateAssertPart(body, methodMd.ReturnType);
            }

            body.Add(FailExpression);
            return MethodDeclaration(VoidReturnType, methodMd.Name)
                .AddModifiers(PublicModifier)
                .AddAttributeLists(AttributeList(AttributeList().Attributes.Add(MethodAttribute)))
                .WithBody(Block(body));
            ;
        }

        private static void GenerateArrangePart(List<StatementSyntax> body, Dictionary<string, string> parameters)
        {
            var baseTypeVars = GetBaseTypeVariables(parameters);
            foreach (var var in baseTypeVars)
            {
                body.Add(GenerateBasesTypesAssignStatement(var.Key, var.Value));
            }
        }

        private static void GenerateActPart(List<StatementSyntax> body, MethodMD methodMd, string checkedClassVariable)
        {
            body.Add(methodMd.ReturnType != "void"
                ? GenerateFunctionCall("actual", GetCheckedClassVariable(checkedClassVariable) + "." + methodMd.Name,
                    ConvertParametersToStringRepresentation(methodMd.Parameters))
                : GenerateVoidFunctionCall(GetCheckedClassVariable(checkedClassVariable) + "." + methodMd.Name,
                    ConvertParametersToStringRepresentation(methodMd.Parameters)));
        }

        private static StatementSyntax GenerateFunctionCall(string varName, string funcName, string invokeArgs = "")
        {
            return ParseStatement($"var {varName} = {funcName}({invokeArgs});");
        }

        private static StatementSyntax GenerateVoidFunctionCall(string funcName, string invokeArgs = "")
        {
            return ParseStatement($"{funcName}({invokeArgs});");
        }

        private static void GenerateAssertPart(List<StatementSyntax> body, string returnType)
        {
            body.Add(GenerateBasesTypesAssignStatement("expected", returnType));
            var invocationExpression = GenerateExpression("Assert", "That");
            var secondPart = GenerateExpression("Is", "EqualTo").WithArgumentList(ArgumentList(
                SeparatedList<ArgumentSyntax>(
                    new SyntaxNodeOrToken[]
                    {
                        Argument(IdentifierName("expected"))
                    })));
            var argList = ArgumentList(SeparatedList<ArgumentSyntax>(
                new SyntaxNodeOrToken[]
                {
                    Argument(IdentifierName("actual")),
                    Token(SyntaxKind.CommaToken),
                    Argument(IdentifierName(secondPart.ToString()))
                }));

            var s = ExpressionStatement(invocationExpression.WithArgumentList(argList));
            body.Add(s);
        }

        private static InvocationExpressionSyntax GenerateExpression(string firstCall, string secondCall)
        {
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(firstCall),
                    IdentifierName(secondCall)));
        }
    }
}