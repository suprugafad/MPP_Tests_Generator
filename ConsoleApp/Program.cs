using System;
using System.Collections.Generic;
using TestGenerator;

namespace ConsoleApp
{
    class Program
    {
        private const string SourceFilePath1 =
            "C:\\ARXIV\\University\\СПП\\MPP_TESTS_GENERATOR\\TestClassLib\\ClassExample1.cs";

        private const string SourceFilePath2 =
            "C:\\ARXIV\\University\\СПП\\MPP_TESTS_GENERATOR\\TestClassLib\\ClassExample3.cs";

        private const string OutDirPath = "C:\\ARXIV\\University\\СПП\\MPP_TESTS_GENERATOR\\GenClasses";

        private const int MaxParallelTasksCount = 3;


        static void Main(string[] args)
        {
            var testsGenerator = new TestsGenerator(MaxParallelTasksCount);
            testsGenerator.Generate(new List<string>() {SourceFilePath1, SourceFilePath2}, OutDirPath).Wait();
        }
    }
}