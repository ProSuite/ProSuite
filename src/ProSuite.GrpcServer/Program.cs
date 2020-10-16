using Grpc.Core;
using ProSuite.ProtobufClasses;
using System;
using System.Collections.Generic;

namespace ProSuite.GrpcServer
{
	class Program
    {
		private static readonly int Port = 30021;
		private static readonly string Host = "localhost";

		static void Main(string[] args)
        {
			IEnumerable<ServerServiceDefinition> services = new List<ServerServiceDefinition>();
			ProSuiteGrpcServer server = new ProSuiteGrpcServer
			{
				Ports = { new ServerPort(Host, Port, ServerCredentials.Insecure) }
			};
			Console.WriteLine($"ProSuite server listening on {Host}:{Port} ");

			Console.WriteLine($"Available services:");
			server.Services.Add(VerifyQualityService.BindService(new VerifyQualityGrpcService()));
			Console.WriteLine($"VerifyQualityService.VerifyQualityGrpcService");

			// TODO subscribe to server events
			server.Start();

			Console.WriteLine("Press any key to stop the server...");
			Console.ReadKey();

			server.ShutdownAsync().Wait();
		}
	}
}
