using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine;
using Grpc.Core;
using Grpc.Health.V1;
using Grpc.HealthCheck;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Xml;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Server.AO.QA;
using Quaestor.LoadReporting;

namespace ProSuite.Microservices.Server.AO
{
	public static class ServerSetupUtils
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private const string _verboseLoggingEnvVar = "PROSUITE_MICROSERVICE_VERBOSE_LOGGING";
		private const string _configDirectoryVariableName = "PROSUITE_CONFIG_DIR";

		/// <summary>
		/// Starts the grpc server, binds the <see cref="QualityVerificationGrpcImpl"/> together
		/// with the <see cref="IServiceHealth"/> implementation and returns a handle for both.
		/// </summary>
		/// <param name="arguments">The microserver command line / config arguments.</param>
		/// <param name="inputsFactoryMethod">The factory method that creates the
		/// <see cref="IBackgroundVerificationInputs"/> instance. If no factory is proveded, only
		/// stand-alone verification (such as XML) can be used.</param>
		/// <param name="checkout3dAnalyst"></param>
		/// <param name="markUnhealthyOnExceptions"></param>
		/// <returns></returns>
		public static StartedGrpcServer StartVerificationServer(
			[NotNull] MicroserverArguments arguments,
			[CanBeNull]
			Func<VerificationRequest, IBackgroundVerificationInputs> inputsFactoryMethod,
			bool checkout3dAnalyst,
			bool markUnhealthyOnExceptions)
		{
			var healthService = new HealthServiceImpl();

			IServiceHealth health = new ServiceHealth(healthService);

			LoadReportingGrpcImpl loadReporting = new LoadReportingGrpcImpl();

			ServiceLoad serviceLoad = new ServiceLoad(2);

			loadReporting.AllowMonitoring(nameof(QualityVerificationGrpc), serviceLoad);

			var wuVerificationServiceImpl =
				new QualityVerificationGrpcImpl(inputsFactoryMethod,
				                                arguments.MaxParallel)
				{
					Checkout3DAnalyst = checkout3dAnalyst,
					CurrentLoad = serviceLoad
				};

			if (markUnhealthyOnExceptions)
			{
				wuVerificationServiceImpl.Health = health;
			}

			health.SetStatus(wuVerificationServiceImpl.GetType(), true);

			ServerCredentials serverCredentials =
				GrpcServerUtils.GetServerCredentials(arguments.Certificate,
				                                     arguments.PrivateKeyFile,
				                                     arguments.EnforceMutualTls);

			var oneGb = (int) Math.Pow(1024, 3);

			IList<ChannelOption> channelOptions = GrpcServerUtils.CreateChannelOptions(oneGb);

			var server =
				new Grpc.Core.Server(channelOptions)
				{
					Services =
					{
						QualityVerificationGrpc.BindService(wuVerificationServiceImpl),
						Health.BindService(healthService),
						LoadReportingGrpc.BindService(loadReporting)
					},
					Ports =
					{
						new ServerPort(arguments.HostName, arguments.Port, serverCredentials)
					}
				};

			server.Start();

			_msg.InfoFormat("Service is listening on host {0}, port {1}.", arguments.HostName,
			                arguments.Port);

			return new StartedGrpcServer(server, health);
		}

		[CanBeNull]
		public static MicroserverArguments GetStartParameters([NotNull] string[] args,
		                                                      [CanBeNull] string configFilePath,
		                                                      int defaultPort = 5151)
		{
			MicroserverArguments result = null;

			if (args.Length == 0)
			{
				// Still parse the args, in case of --help or --version
				Parser.Default.ParseArguments<MicroserverArguments>(args);

				_msg.InfoFormat("No arguments provided. For help, start with --help");

				if (configFilePath != null && File.Exists(configFilePath))
				{
					_msg.InfoFormat("Getting server parameters from {0}", configFilePath);

					XmlSerializationHelper<MicroserverArguments> helper =
						new XmlSerializationHelper<MicroserverArguments>();

					result = helper.ReadFromFile(configFilePath);
				}
				else
				{
					_msg.InfoFormat("No configuration file found at {0}. Using default parameters.",
					                configFilePath);

					result = new MicroserverArguments
					         {
						         HostName = "localhost",
						         Port = defaultPort
					         };
				}
			}
			else
			{
				var parsedArgs = Parser.Default.ParseArguments<MicroserverArguments>(args);

				parsedArgs.WithParsed(arguments => { result = arguments; });

				bool helpArg = args.Any(
					a => a != null &&
					     a.Equals("--help", StringComparison.InvariantCultureIgnoreCase));

				if (helpArg)
				{
					// Lets exit after printing the help:
					return null;
				}
			}

			return result;
		}

		public static void ConfigureLogging(string[] commandLineArgs,
		                                    [NotNull] string logConfigFileName)
		{
			bool verboseArg = commandLineArgs.Any(
				a => a != null &&
				     (a.Equals("-v", StringComparison.InvariantCultureIgnoreCase) ||
				      a.Equals("--verbose", StringComparison.InvariantCultureIgnoreCase)));

			ConfigureLogging(verboseArg, logConfigFileName);
		}

		public static void ConfigureLogging(bool verboseRequired,
		                                    [NotNull] string logConfigFileName)
		{
			int processId = Process.GetCurrentProcess().Id;

			LoggingConfigurator.SetGlobalProperty("LogFileSuffix", $"PID_{processId}");

			LoggingConfigurator.Configure(logConfigFileName,
			                              GetLogConfigPaths(),
			                              useDefaultConfiguration: true);

			_msg.ReportMemoryConsumptionOnError = true;

			bool verboseLogging =
				verboseRequired ||
				EnvironmentUtils.GetBooleanEnvironmentVariableValue(_verboseLoggingEnvVar);

			if (verboseLogging)
			{
				_msg.DebugFormat("Verbose logging configured by env var ({0})",
				                 _verboseLoggingEnvVar);

				_msg.IsVerboseDebugEnabled = true;
			}

			Assembly exeAssembly = Assembly.GetEntryAssembly();

			string bitness = Environment.Is64BitProcess ? "64 bit" : "32 bit";

			_msg.InfoFormat("Logging configured for {0} ({1}) version {2}",
			                exeAssembly.Location, bitness,
			                exeAssembly.GetName().Version);

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.Debug("Environment variables:");

				foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
					_msg.DebugFormat("  {0} = {1}", de.Key, de.Value);
			}
		}

		private static IEnumerable<string> GetLogConfigPaths()
		{
			string dirPath = Environment.GetEnvironmentVariable(_configDirectoryVariableName);

			if (! string.IsNullOrEmpty(dirPath))
			{
				yield return dirPath;
			}

			const string configDir = "Config";

			string assemblyPath = Assembly.GetExecutingAssembly().Location;

			string binDir = Assert.NotNullOrEmpty(Path.GetDirectoryName(assemblyPath));

			DirectoryInfo parentDir = Directory.GetParent(binDir);

			if (parentDir != null)
			{
				yield return Path.Combine(parentDir.FullName, configDir);
			}

			yield return binDir;
			yield return Path.Combine(binDir, configDir);
		}
	}
}
