using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using CommandLine;
using Grpc.Core;
using Grpc.Health.V1;
using Grpc.HealthCheck;
using log4net.Util;
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
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private const string _verboseLoggingEnvVar = "PROSUITE_MICROSERVICE_VERBOSE_LOGGING";

		public static string ConfigDirectoryVariableName { get; set; } = "PROSUITE_CONFIG_DIR";

		/// <summary>
		/// Starts the grpc server, binds the <see cref="QualityVerificationGrpcImpl"/> together
		/// with the <see cref="IServiceHealth"/> implementation and returns a handle for both.
		/// </summary>
		/// <param name="arguments">The microserver command line / config arguments.</param>
		/// <param name="inputsFactory">The factory method that creates the
		/// <see cref="IBackgroundVerificationInputs"/> instance. If no factory is proveded, only
		/// stand-alone verification (such as XML) can be used.</param>
		/// <param name="markUnhealthyOnExceptions"></param>
		/// <returns></returns>
		public static StartedGrpcServer<QualityVerificationGrpcImpl> StartVerificationServer(
			[NotNull] MicroserverArguments arguments,
			[CanBeNull] Func<VerificationRequest, IBackgroundVerificationInputs> inputsFactory,
			bool markUnhealthyOnExceptions)
		{
			var healthService = new HealthServiceImpl();

			IServiceHealth health = new ServiceHealth(healthService);

			LoadReportingGrpcImpl loadReporting = new LoadReportingGrpcImpl();

			int maxThreadCount = arguments.MaxParallel < 0
				                     ? Environment.ProcessorCount - 1
				                     : arguments.MaxParallel;

			QualityVerificationGrpcImpl verificationServiceImpl =
				CreateQualityVerificationGrpc(inputsFactory, loadReporting,
				                              markUnhealthyOnExceptions ? health : null,
				                              maxThreadCount);

			health.SetStatus(verificationServiceImpl.GetType(), true);

			Grpc.Core.Server server =
				StartGrpcServer(arguments, verificationServiceImpl, healthService, loadReporting);

			_msg.InfoFormat("Service is listening on host {0}, port {1}.", arguments.HostName,
			                arguments.Port);

			return new StartedGrpcServer<QualityVerificationGrpcImpl>(
				server, verificationServiceImpl, health);
		}

		public static QualityVerificationGrpcImpl CreateQualityVerificationGrpc(
			[NotNull] MicroserverArguments arguments,
			[CanBeNull] Func<VerificationRequest, IBackgroundVerificationInputs> inputsFactory,
			[CanBeNull] LoadReportingGrpcImpl loadReporting,
			[CanBeNull] IServiceHealth health)
		{
			int maxThreadCount = arguments.MaxParallel < 0
				                     ? Environment.ProcessorCount - 1
				                     : arguments.MaxParallel;

			return CreateQualityVerificationGrpc(inputsFactory, loadReporting, health,
			                                     maxThreadCount);
		}

		public static QualityVerificationGrpcImpl CreateQualityVerificationGrpc(
			[CanBeNull] Func<VerificationRequest, IBackgroundVerificationInputs> inputsFactory,
			[CanBeNull] LoadReportingGrpcImpl loadReporting,
			[CanBeNull] IServiceHealth health,
			int maxThreadCount)
		{
			ServiceLoad serviceLoad = null;

			if (loadReporting != null)
			{
				serviceLoad = new ServiceLoad(maxThreadCount);

				loadReporting.AllowMonitoring(nameof(QualityVerificationGrpc), serviceLoad);
			}

			var verificationServiceImpl =
				new QualityVerificationGrpcImpl(inputsFactory, maxThreadCount)
				{
					CurrentLoad = serviceLoad
				};

			if (health != null)
			{
				verificationServiceImpl.Health = health;
			}

			return verificationServiceImpl;
		}

		private static Grpc.Core.Server StartGrpcServer(
			MicroserverArguments arguments,
			[NotNull] QualityVerificationGrpcImpl verificationServiceImpl,
			[NotNull] HealthServiceImpl healthService,
			[NotNull] LoadReportingGrpcImpl loadReporting)
		{
			var services = new List<ServerServiceDefinition>(
				new[]
				{
					QualityVerificationGrpc.BindService(verificationServiceImpl),
					Health.BindService(healthService),
					LoadReportingGrpc.BindService(loadReporting)
				});

			Grpc.Core.Server result = StartGrpcServer(services, arguments);

			if (arguments.Port == 0)
			{
				// 0 means use any free port
				ServerPort serverPort = result.Ports.FirstOrDefault();

				if (serverPort != null)
				{
					arguments.Port = serverPort.BoundPort;
				}
			}

			return result;
		}

		/// <summary>
		/// Starts the grpc server at the address specified in the microserver arguments parameter
		/// with the specified services.
		/// </summary>
		/// <param name="services">The list of service definitions to be hosted by the server.</param>
		/// <param name="arguments">The microserver arguments containing the connection details.</param>
		/// <returns></returns>
		public static Grpc.Core.Server StartGrpcServer(
			[NotNull] ICollection<ServerServiceDefinition> services,
			[NotNull] MicroserverArguments arguments)
		{
			return StartGrpcServer(services, arguments.HostName, arguments.Port,
			                       arguments.Certificate, arguments.PrivateKeyFile,
			                       arguments.EnforceMutualTls);
		}

		/// <summary>
		/// Starts the grpc server at the specified address with the specified services.
		/// </summary>
		/// <param name="services">The list of service definitions to be hosted by the server.</param>
		/// <param name="hostName">The host name.</param>
		/// <param name="port">The port.</param>
		/// <param name="certificate">The certificate store's certificate (subject or thumbprint)
		/// or the PEM file containing the certificate chain.</param>
		/// <param name="privateKeyFilePath">The PEM file containing the private key (only if the
		/// certificate was provided by a PEM file.</param>
		/// <param name="enforceMutualTls">Enforce client authentication.</param>
		/// <returns></returns>
		public static Grpc.Core.Server StartGrpcServer(
			[NotNull] ICollection<ServerServiceDefinition> services,
			[NotNull] string hostName,
			int port,
			string certificate,
			string privateKeyFilePath,
			bool enforceMutualTls = false)
		{
			ServerCredentials serverCredentials =
				GrpcServerUtils.GetServerCredentials(certificate,
				                                     privateKeyFilePath,
				                                     enforceMutualTls);

			// Enough for large geometries
			var oneGb = (int) Math.Pow(1024, 3);

			IList<ChannelOption> channelOptions = GrpcServerUtils.CreateChannelOptions(oneGb);

			var server =
				new Grpc.Core.Server(channelOptions)
				{
					Ports =
					{
						new ServerPort(hostName, port, serverCredentials)
					}
				};

			foreach (ServerServiceDefinition serviceDefinition in services)
			{
				server.Services.Add(serviceDefinition);
			}

			_msg.DebugFormat("Starting grpc server on {0} with the {1} services...",
			                 ToHttpUrl(hostName, port, certificate != null), services.Count);

			server.Start();

			return server;
		}

		private static string ToHttpUrl(string hostName, int port, bool useTls)
		{
			string protocol = useTls ? "https" : "http";

			return $"{protocol}://{hostName}:{port}";
		}

		[CanBeNull]
		public static MicroserverArguments GetStartParameters([NotNull] string[] args,
		                                                      [CanBeNull] string configFilePath,
		                                                      int defaultPort = 5151)
		{
			MicroserverArguments result = null;
			_msg.InfoFormat($"Running process {Process.GetCurrentProcess().Id}");

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
				_msg.InfoFormat(
					"Using arguments from command line (config file is ignored if it exists).");

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

		public static void ConfigureLogging(
			string[] commandLineArgs,
			[NotNull] string logConfigFileName,
			[CanBeNull] ConfigurationDirectorySearcher configDirSearcher = null)
		{
			bool verboseArg = commandLineArgs.Any(
				a => a != null &&
				     (a.Equals("-v", StringComparison.InvariantCultureIgnoreCase) ||
				      a.Equals("--verbose", StringComparison.InvariantCultureIgnoreCase)));

			var parsedArgs = Parser.Default.ParseArguments<MicroserverArguments>(commandLineArgs);

			int port = -1;
			parsedArgs.WithParsed(a => port = a.Port);

			ConfigureLogging(verboseArg, logConfigFileName, configDirSearcher, port);
		}

		public static void ConfigureLogging(
			bool verboseRequired,
			[NotNull] string logConfigFileName,
			[CanBeNull] ConfigurationDirectorySearcher configDirSearcher = null,
			int port = -1)
		{
			int processId = Process.GetCurrentProcess().Id;

			string suffix = port < 0 ? $"PID_{processId}" : $"Port_{port}_PID_{processId}";

			LoggingConfigurator.SetGlobalProperty("LogFileSuffix", suffix);

			LoggingConfigurator.Configure(logConfigFileName,
			                              configDirSearcher?.GetSearchPaths() ??
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

			Assembly executingAssembly = Assert.NotNull(Assembly.GetExecutingAssembly());

			if (exeAssembly == null)
			{
				// e.g. unit tests
				exeAssembly = executingAssembly;
			}

			string bitness = Environment.Is64BitProcess ? "64 bit" : "32 bit";

			_msg.InfoFormat("Logging configured for {0} ({1}) version {2}",
			                exeAssembly.Location, bitness,
			                executingAssembly.GetName().Version);

			_msg.DebugFormat("Loaded log4net version: {0}",
			                 typeof(GlobalContextProperties).Assembly.FullName);

			string frameworkDescription = RuntimeInformation.FrameworkDescription;

			_msg.DebugFormat("Currently used .NET Runtime: {0}", frameworkDescription);

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.Debug("Environment variables:");

				foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
					_msg.DebugFormat("  {0} = {1}", de.Key, de.Value);
			}
		}

		private static IEnumerable<string> GetLogConfigPaths()
		{
			string dirPath = Environment.GetEnvironmentVariable(ConfigDirectoryVariableName);

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
