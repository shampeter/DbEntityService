using System;

namespace AXAXL.DbEntity.Benchmarks
{
	class Program
	{
		static void Main(string[] args)
		{
			var benchmark = new BenchmarkMain();
			benchmark.GlobalSetup();
			var count = benchmark.BaseLine();
			Console.WriteLine($"Total records found from SQL = {count}");
			count = benchmark.DbServiceBenchmark();
			Console.WriteLine($"Total records found from DbServer = {count}");
		}
	}
}
