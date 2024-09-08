using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;


namespace MicroBenchmarkORT
{
    [ShortRunJob]
    [NativeMemoryProfiler]
    [MemoryDiagnoser]
    public class DemoOrtBenchmark
    {
        private InferenceSession? _session;
        private Dictionary<string, NamedOnnxValue>? _inputNameToNamedOnnxValue;

        [GlobalSetup]
        public void Setup()
        {
            //var options = new SessionOptions();
            //options.AppendExecutionProvider_CPU(0);
            _session = new InferenceSession("model/mnist-8.onnx");

            var inputMetadata = _session.InputMetadata;

            var inputNameToTensor = inputMetadata.ToDictionary(p => p.Key, p =>
            {
                // There are 3 leading dims, [-1,1,1]
                var dimensions = p.Value.Dimensions;
                dimensions[0] = 1; // Batch-size 1
                var tensor = new DenseTensor<float>(dimensions);
                var buffer = tensor.Buffer;
                return tensor;
            });

            _inputNameToNamedOnnxValue = inputNameToTensor.ToDictionary(p => p.Key,
                p => NamedOnnxValue.CreateFromTensor(p.Key, p.Value));
        }

        [Benchmark]
        public float Run()
        {
            if (_session == null || _inputNameToNamedOnnxValue == null)
            {
                throw new InvalidOperationException("Session is not initialized");
            }
            using var outputs = _session.Run(_inputNameToNamedOnnxValue.Values);
            // Should be IReadOnlyList to allow indexing
            var output = outputs.Single();
            var outputTensor = output.AsTensor<float>();
            Span<int> index = stackalloc int[outputTensor.Rank];
            return outputTensor[index];
        }


        [GlobalCleanup]
        public void Cleanup()
        {
            _session?.Dispose();
        }

    }
}
