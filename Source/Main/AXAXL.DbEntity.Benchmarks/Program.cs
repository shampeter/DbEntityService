using System;
using System.Diagnostics;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Validators;
using BenchmarkDotNet.Jobs;

namespace AXAXL.DbEntity.Benchmarks
{
	class Program
	{
		static bool IsDebugging() => Debugger.IsAttached;

		static void Main(string[] args)
		{
			if (
				args != null && 
				args.Length == 1 && 
				args[0].Equals(@"--verify", StringComparison.InvariantCultureIgnoreCase)
			)
			{
				Verify();
				return;
			}

			IConfig benchmarkConfig = null;

			if (IsDebugging())
			{
				Console.WriteLine("*********************************************************");
				Console.WriteLine("*** WARNING! Debug mode would impact actual benchmark ***");
				Console.WriteLine("*********************************************************");

				benchmarkConfig = ManualConfig
					.Create(DefaultConfig.Instance)
					.With(Job.Default.WithIterationCount(3))
					.With(BenchmarkDotNet.Loggers.ConsoleLogger.Default)
					.With(ExecutionValidator.FailOnError);
			}
			var summary = BenchmarkRunner.Run<BenchmarkMain>(benchmarkConfig);
			//BenchmarkSwitcher
			//	.FromAssembly(typeof(Program).Assembly)
			//	.Run(args, benchmarkConfig);
		}

		private static void Verify()
		{
			var benchmark = new BenchmarkMain();
			benchmark.GlobalSetup();
			var count = benchmark.BaseLine();
			Console.WriteLine($"Total records found from SQL      = {count}");
			count = benchmark.DbServiceBenchmark();
			Console.WriteLine($"Total records found from DbServer = {count}");
		}
	}
}
