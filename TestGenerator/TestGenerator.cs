using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TestGenerator
{
    public class TestsGenerator
    {
        private readonly ExecutionDataflowBlockOptions _executionOptions = new() {MaxDegreeOfParallelism = 5};
        private static readonly DataflowLinkOptions LinkOptions = new() {PropagateCompletion = true};

        public TestsGenerator()
        {
        }

        public TestsGenerator(int maxParallelTasks)
        {
            _executionOptions = new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = maxParallelTasks};
        }

        public Task Generate(IEnumerable<string> classFilesPaths, string outDirPath)
        {
            Directory.CreateDirectory(outDirPath);

            var readFileBlock = BuildReadFileBlock();

            var generateTestsBlock = BuildGenerateTestsBlock();

            var writeFileBlock = BuildWriteFileBlock(outDirPath);

            readFileBlock.LinkTo(generateTestsBlock, LinkOptions);
            generateTestsBlock.LinkTo(writeFileBlock, LinkOptions);
            foreach (var file in classFilesPaths)
            {
                readFileBlock.Post(file);
            }

            readFileBlock.Complete();
            return writeFileBlock.Completion;
        }

        private TransformBlock<string, string> BuildReadFileBlock()
        {
            var readFileBlock = new TransformBlock<string, string>
            (
                async filePath =>
                {
                    using var reader = new StreamReader(filePath);
                    return await reader.ReadToEndAsync();
                },
                _executionOptions
            );
            return readFileBlock;
        }

        private TransformManyBlock<string, KeyValuePair<string, string>> BuildGenerateTestsBlock()
        {
            var generateTestsBlock = new TransformManyBlock<string, KeyValuePair<string, string>>
            (
                async sourceCode =>
                {
                    var fileInfo = await Task.Run(() => MetaDataParser.GetFileMetaData(sourceCode));
                    return await Task.Run(() => GenerationUtils.GenerateTests(fileInfo));
                },
                _executionOptions
            );
            return generateTestsBlock;
        }

        private ActionBlock<KeyValuePair<string, string>> BuildWriteFileBlock(string outputDir)
        {
            var writeFileBlock = new ActionBlock<KeyValuePair<string, string>>
            (
                async fileName =>
                {
                    await using var writer = new StreamWriter(outputDir + "/" + fileName.Key + ".cs");
                    await writer.WriteAsync(fileName.Value);
                },
                _executionOptions
            );
            return writeFileBlock;
        }
    }
}