using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using CommandLine;
using Grpc.Core;
using Grpc.HealthCheck;
using ProSuite.Commons;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Definitions.Geometry;
using ProSuite.Microservices.Server.AO;
using ProSuite.Microservices.Server.AO.Geometry.RemoveOverlaps;

namespace ProSuite.Microservices.Server.Geometry.Console
{
	[UsedImplicitly]
	public class Program
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private const string _configFileName = "prosuite.microserver.geometry_processing.xml";

		private const string _logConfigFileName =
			"prosuite.logging.microservice.geometry_processing.xml";

		private const string _verboseLoggingEnvVar = "PROSUITE_MICROSERVICE_VERBOSE_LOGGING";

		[STAThread]
		static void Main(string[] args)
		{
			try
			{
				IServiceHealth health;
				Grpc.Core.Server server = Run(args, out health);

				_msg.Info("Type Q(uit) to stop the server.");

				while (true)
				{
					// Avoid mindless spinning
					Thread.Sleep(100);

					if (System.Console.KeyAvailable)
					{
						if (System.Console.ReadKey(true).Key == ConsoleKey.Q)
						{
							_msg.Warn("Shutting down due to user input");
							break;
						}
					}

					// TODO: Uncomment after next pull
					//if (health.IsAnyServiceUnhealthy())
					//{
					//	_msg.Warn("Shutting down due to service state NOT_SERVING");
					//	break;
					//}
				}

				if (server != null)
					GrpcServerUtils.GracefullyStop(server);
			}
			catch (Exception ex)
			{
				_msg.Error("An error occurred in microservice.", ex);
				Environment.ExitCode = -1;
			}
			finally
			{
				_msg.Debug("License released, shutting down...");
			}
		}

		private static Grpc.Core.Server Run(string[] args,
		                                    out IServiceHealth health)
		{
			Grpc.Core.Server server;

			try
			{
				MicroserverArguments arguments;
				string configFilePath;
				if (! ConfigurationUtils.TryGetArgumentsFromConfigFile(
					    args, _configFileName, out arguments, out configFilePath))
				{
					var parsedArgs = Parser.Default.ParseArguments<MicroserverArguments>(args);

					parsedArgs.WithParsed(a => { arguments = a; });
				}

				ConfigureLogging(arguments.VerboseLogging, _logConfigFileName);

				if (configFilePath != null)
				{
					_msg.InfoFormat("Using service configuration defined in {0}", configFilePath);
				}
				else
				{
					_msg.DebugFormat(
						"Program was called with the following command line arguments: {0}{1}",
						Environment.NewLine, arguments);
				}

				_msg.DebugFormat("Host: {0}", arguments.HostName);
				_msg.DebugFormat("Port: {0}", arguments.Port);

				_msg.InfoFormat("Checking out ArcGIS license...");

				ArcGISLicenses.InitializeAo11();

				EnvironmentUtils.SetConfigurationDirectoryProvider(
					ConfigurationUtils.GetAppDataConfigDirectoryProvider());

				server = StartServer(arguments, out health);
			}
			catch (Exception ex)
			{
				_msg.Error("An error occurred in QA microservice startup.", ex);
				throw;
			}

			return server;
		}

		public static Grpc.Core.Server StartServer([NotNull] MicroserverArguments arguments,
		                                           out IServiceHealth health)
		{
			// TODO: Move to ProSuite
			var healthService = new HealthServiceImpl();

			health = null; // new ServiceHealth(healthService);

			int maxThreadCount = arguments.MaxParallel;

			if (maxThreadCount <= 0)
			{
				maxThreadCount = Environment.ProcessorCount - 1;
			}

			var taskScheduler = new StaTaskScheduler(maxThreadCount);

			var removeOverlapsServiceImpl = new RemoveOverlapsGrpcImpl(taskScheduler)
			                                {
				                                //Health = health
			                                };

			//health.SetStatus(removeOverlapsServiceImpl.GetType(), true);

			ServerCredentials serverCredentials =
				GrpcServerUtils.GetServerCredentials(arguments.Certificate,
				                                     arguments.PrivateKeyFile);

			var oneGb = (int) Math.Pow(1024, 3);

			IList<ChannelOption> channelOptions = GrpcServerUtils.CreateChannelOptions(oneGb);

			var server =
				new Grpc.Core.Server(channelOptions)
				{
					Services =
					{
						RemoveOverlapsGrpc.BindService(removeOverlapsServiceImpl),
						//Health.BindService(healthService)
					},
					Ports =
					{
						new ServerPort(arguments.HostName, arguments.Port, serverCredentials)
					}
				};

			server.Start();

			_msg.InfoFormat("Service is listening on host {0}, port {1}.", arguments.HostName,
			                arguments.Port);

			return server;
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

			Assembly executingAssembly = Assembly.GetExecutingAssembly();

			string bitness = Environment.Is64BitProcess ? "64 bit" : "32 bit";

			_msg.InfoFormat("Logging configured for {0} ({1}) version {2}",
			                executingAssembly.Location, bitness,
			                executingAssembly.GetName().Version);

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.Debug("Environment variables:");

				foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
					_msg.DebugFormat("  {0} = {1}", de.Key, de.Value);
			}
		}

		private static IEnumerable<string> GetLogConfigPaths()
		{
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

	public interface IServiceHealth
	{
		// TODO: Move to Microservices.Server.AO!
	}
}