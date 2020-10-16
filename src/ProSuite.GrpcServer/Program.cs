using Grpc.Core;
using System;
using ProSuite.ProtobufClasses;
using System.Collections;
using System.Collections.Generic;

namespace ProSuite.GrpcServer
{
	class Program
    {
		private static readonly int Port = 30021;
		private static readonly string Host = "127.0.0.1";

		static void Main(string[] args)
        {
			ProSuiteGrpcServer server = new ProSuiteGrpcServer
			{
				Services = { VerifyQualityService.BindService(new VerifyQualityGrpcService()) },
				Ports = { new ServerPort(Host, Port, ServerCredentials.Insecure) }
			};

			// TODO subscribe to server events

			server.Start();

			Console.WriteLine($"ProSuite server listening on {Host}:{Port} ");
			Console.WriteLine($"Available services: VerifyQualityService");

			Console.WriteLine("Press any key to stop the server...");
			Console.ReadKey();

			server.ShutdownAsync().Wait();
		}
	}
}
