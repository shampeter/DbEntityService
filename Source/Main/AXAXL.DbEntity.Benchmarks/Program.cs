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
			int count = 0;

			count = benchmark.BaseLine();
			Console.WriteLine($"Total records found from SQL                             = {count}");
			count = benchmark.DirectQueryForTop200();
			Console.WriteLine($"Total records found from SQL for Top 200                 = {count}");
			count = benchmark.QueryByEntityWithVM();
			Console.WriteLine($"Total records found from Entity Query with VM            = {count}");
			count = benchmark.QueryByEntityWithVMForTop200();
			Console.WriteLine($"Total records found from Entity Query with VM Top 200    = {count}");
			count = benchmark.QueryByEntityWithoutVM();
			Console.WriteLine($"Total records found from Entity Query without VM         = {count}");
			count = benchmark.QueryByEntityWithoutChild();
			Console.WriteLine($"Total records found from Entity Query with no child      = {count}");
			count = benchmark.QueryByEntityWithOnlyMktLoss();
			Console.WriteLine($"Total records found from Entity Query with only Mkt Loss = {count}");
			count = benchmark.QueryByEntityWithOnlyUserSessn();
			Console.WriteLine($"Total records found from Entity Query with only Usr Sess = {count}");
			count = benchmark.QueryByExecCmd();
			Console.WriteLine($"Total records found from Exec Cmd                        = {count}");
			count = benchmark.InnerJoinQuery();
			Console.WriteLine($"Total records found from Inner Join Query                = {count}");
			count = benchmark.QueryByEntityWithInnerJoin();
			Console.WriteLine($"Total records found from Entity Query with InnerJoin     = {count}");

		}
	}
}
