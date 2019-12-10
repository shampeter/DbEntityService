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
				args.Length == 1
			)
			{
				switch(args[0].ToLower())
				{
					case @"--verify":
						Verify();
						break;
					case @"--debug":
						Debug();
						break;
				}
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
		private static void Debug()
		{
			var benchmark = new BenchmarkMain();
			benchmark.GlobalSetup();

			var testCases = new (string desc, Func<int> test)[]
			{
				("Baseline", benchmark.BaseLine),
				("Query with No Child", benchmark.QueryByEntityWithoutChild),
				("Query with only Market Loss", benchmark.QueryByEntityWithOnlyMktLoss),
				("Query with only User Session", benchmark.QueryByEntityWithOnlyUserSessn),
				("Query with no View Model", benchmark.QueryByEntityWithoutVM)
			};

			for(int i = 1; i <= testCases.Length; i++)
			{
				Console.WriteLine("{0,3} {1}", i, testCases[i - 1].desc);
			}
			var choice = ConsoleEnterInt(1, testCases.Length);
			var loop = ConsoleEnterInt(1, 10);
			for(int i = 1; i <= loop; i++)
			{
				testCases[choice - 1].test();
			}
			Pause();
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
		private static int ConsoleEnterInt(int start, int end)
		{
			bool correct = false;
			int choice = -1;
			while (! correct)
			{
				Console.Write($"Enter choice between {start} and {end}: ");
				var entry = Console.ReadLine();
				if (int.TryParse(entry, out choice) && start <= choice && choice <= end)
				{
					correct = true;
				}
			}
			return choice;
		}
		private static void Pause()
		{
			Console.Write("Press Enter to continue"); Console.ReadLine();
		}
	}
}
