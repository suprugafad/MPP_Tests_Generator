using System.Collections.Generic;

namespace TestGenerator.MetaData
{
    public class ClassMD
    {
        public List<MethodMD> Methods { get; }
        public string ClassName { get; }
        public List<ConstructorMD> Constructors { get; }

        public ClassMD(List<MethodMD> methods, List<ConstructorMD> constructors, string className)
        {
            Methods = methods;
            Constructors = constructors;
            ClassName = className;
        }
    }
}