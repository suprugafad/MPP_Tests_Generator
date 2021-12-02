using System.Collections.Generic;

namespace TestGenerator.MetaData
{
    public class FileMD
    {
        public List<ClassMD> Classes { get; private set; }

        public FileMD(List<ClassMD> classes)
        {
            Classes = classes;
        }
    }
}