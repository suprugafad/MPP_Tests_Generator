using System.Collections.Generic;

namespace TestClassLib
{
    public class ClassExample3
    {
        public IEnumerable<int> InterfaceField { get; private set; }

        public ClassExample3(IEnumerable<int> d)
        {
        }

        public void Func1(int a, string str)
        {
        }

        public void Func2()
        {
        }
    }
}