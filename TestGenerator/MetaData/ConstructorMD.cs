using System.Collections.Generic;

namespace TestGenerator.MetaData
{
    public class ConstructorMD
    {
        public string Name { get; }
        public Dictionary<string, string> Parameters { get; }

        public ConstructorMD(Dictionary<string, string> parameters, string name)
        {
            Name = name;
            Parameters = parameters;
        }
    }
}