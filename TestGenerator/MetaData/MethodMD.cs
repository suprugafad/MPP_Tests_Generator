using System.Collections.Generic;

namespace TestGenerator.MetaData
{
    public class MethodMD
    {
        public string Name { get; }
        public Dictionary<string, string> Parameters { get; }
        public string ReturnType { get; }

        public MethodMD(Dictionary<string, string> parameters, string name, string returnType)
        {
            ReturnType = returnType;
            Name = name;
            Parameters = parameters;
        }
    }
}