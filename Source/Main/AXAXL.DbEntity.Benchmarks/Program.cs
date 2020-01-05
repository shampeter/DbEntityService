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
				("Query with no View Model base", benchmark.QueryByEntityWithVMWithNoOptimization),
				("Query with no View Model Opt1", benchmark.QueryByEntityWithVMWithOptimization1),
				("Query with no View Model Opt2", benchmark.QueryByEntityWithVMWithOptimization2),
				("Query on CLR User Session", benchmark.QueryByEntityOnCLRUserSession),
				("SQL on CLR User Session", benchmark.DirectSQLOnCLRUserSession)
			};

			for(int i = 1; i <= testCases.Length; i++)
			{
				Console.WriteLine("{0,3} {1}", i, testCases[i - 1].desc);
			}
			var choice = ConsoleEnterInt(1, testCases.Length);
			var loop = ConsoleEnterInt(1, 10);
			for(int i = 1; i <= loop; i++)
			{
				Console.WriteLine("{0,30} = {1}", testCases[choice - 1].desc, testCases[choice - 1].test());
			}
			Pause();
		}
		private static void Verify()
		{
			var benchmark = new BenchmarkMain();
			benchmark.GlobalSetup();
			int count = 0;

			var testcases = new (string desc, Func<int> testcase)[]
			{
				("From SQL", benchmark.BaseLine),
				("From SQL for Top 200", benchmark.DirectQueryForTop200),
				//("From Entity Query with VM", benchmark.QueryByEntityWithVM),
				("From Entity Query with VM Top 200", benchmark.QueryByEntityWithVMForTop200),
				("From Entity Query without VM base", benchmark.QueryByEntityWithVMWithNoOptimization),
				("From Entity Query without VM Opt1", benchmark.QueryByEntityWithVMWithOptimization1),
				("From Entity Query without VM Opt2", benchmark.QueryByEntityWithVMWithOptimization2),
				("From Entity Query with no child", benchmark.QueryByEntityWithoutChild),
				("From Entity Query with only Mkt Loss", benchmark.QueryByEntityWithOnlyMktLoss),
				("From Entity Query with only Usr Sess", benchmark.QueryByEntityWithOnlyUserSessn),
				("From Exec Cmd", benchmark.QueryByExecCmd),
				("From Inner Join Query", benchmark.InnerJoinQuery),
				("From Entity Query with InnerJoin", benchmark.QueryByEntityWithInnerJoin),
				("From Entity Query on CLR User Session", benchmark.QueryByEntityOnCLRUserSession),
				("From SQL on CLR User Session", benchmark.DirectSQLOnCLRUserSession)
			};

			foreach(var eachTest in testcases)
			{
				count = eachTest.testcase();
				Console.WriteLine(@"{0,-40}={1}", eachTest.desc, count);
			}
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
