using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TestGenerator.MetaData;

namespace TestGenerator
{
    public static class MetaDataParser
    {
        public static FileMD GetFileMetaData(string code)
        {
            var root = CSharpSyntaxTree.ParseText(code).GetCompilationUnitRoot();
            var classes = new List<ClassMD>();
            foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                classes.Add(GetClassMetaData(classDeclaration));
            }

            return new FileMD(classes);
        }


        private static ClassMD GetClassMetaData(ClassDeclarationSyntax classDeclaration)
        {
            var methods = new List<MethodMD>();
            foreach (var method in classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>()
                .Where((methodDeclaration) =>
                    methodDeclaration.Modifiers.Any((modifier) => modifier.IsKind(SyntaxKind.PublicKeyword))))
            {
                methods.Add(GetMethodMetaData(method));
            }

            var constructors = new List<ConstructorMD>();
            foreach (var constructor in classDeclaration.DescendantNodes().OfType<ConstructorDeclarationSyntax>()
                .Where((constructorDeclaration) =>
                    constructorDeclaration.Modifiers.Any((modifier) => modifier.IsKind(SyntaxKind.PublicKeyword))))
            {
                constructors.Add(GetConstructorMetaData(constructor));
            }

            return new ClassMD(methods, constructors, classDeclaration.Identifier.ValueText);
        }


        private static MethodMD GetMethodMetaData(MethodDeclarationSyntax method)
        {
            var parameters = new Dictionary<string, string>();
            foreach (var parameter in method.ParameterList.Parameters)
            {
                if (parameter.Type != null) parameters.Add(parameter.Identifier.Text, parameter.Type.ToString());
            }

            return new MethodMD(parameters, method.Identifier.ValueText, method.ReturnType.ToString());
        }

        private static ConstructorMD GetConstructorMetaData(ConstructorDeclarationSyntax constructor)
        {
            var parameters = new Dictionary<string, string>();
            foreach (var parameter in constructor.ParameterList.Parameters)
            {
                if (parameter.Type != null) parameters.Add(parameter.Identifier.Text, parameter.Type.ToString());
            }

            return new ConstructorMD(parameters, constructor.Identifier.ValueText);
        }
    }
}