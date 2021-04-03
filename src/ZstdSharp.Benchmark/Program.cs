using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace ZstdSharp.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = ManualConfig.Create(DefaultConfig.Instance);
            BenchmarkRunner.Run<Benchmark>(config);
        }
    }
}
