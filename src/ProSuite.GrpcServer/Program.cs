using Grpc.Core;
using ProSuite.ProtobufClasses;
using System;
using System.Collections.Generic;
using ProSuite.Commons.Logging;
using ProSuite.QA.ServiceManager;

namespace ProSuite.GrpcServer
{
	class Program
    {
		private static readonly int Port = 30021;
		private static readonly string Host = "localhost";
		//private const string _loggingConfigFile = "prosuite.logging.grpcserver.xml";

		private static IMsg _msg = new Msg(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		static void Main(string[] args)
        {
	        //LoggingConfigurator.Configure(_loggingConfigFile);
	        //_msg.Debug("Logging configured");

			IEnumerable<ServerServiceDefinition> services = new List<ServerServiceDefinition>();
			ProSuiteGrpcServer server = new ProSuiteGrpcServer
			{
				Ports = { new ServerPort(Host, Port, ServerCredentials.Insecure) }
			};
			Console.WriteLine($"ProSuite server listening on {Host}:{Port} ");

			Console.WriteLine($"Available services:");
			server.Services.Add(VerifyQualityService.BindService(new VerifyQualityGrpcService(new ProSuiteQualityVerificationAgentMock())));
			Console.WriteLine($"VerifyQualityService.VerifyQualityGrpcService");
			Console.WriteLine($"");
			// TODO subscribe to server events
			server.Start();

			Console.WriteLine("Press any key to stop the server...");
			Console.ReadKey();

			server.ShutdownAsync().Wait();
		}
	}
}
