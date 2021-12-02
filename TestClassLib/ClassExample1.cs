using System;
using System.Collections.Generic;

namespace TestClassLib
{
    public class ClassExample1
    {
        public IEnumerable<int> InterfaceField { get; private set; }

        public ClassExample1(IDisposable disp, ICloneable clon, int a, string str)
        {
        }

        public int Func1(int j, int k)
        {
            return 0;
        }

        public void Func2()
        {
        }
    }

    public class ClassExample2
    {
        public IEnumerable<int> InterfaceField { get; private set; }

        public void Funс1()
        {
        }

        public void Func2()
        {
        }
    }
}