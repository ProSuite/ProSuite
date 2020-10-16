using System;
using System.Threading;

namespace ProSuite.GrpcClient.TestConsole
{
    class Program
    {
		private static readonly int Port = 30021;
		private static readonly string Host = "localhost";

		static void Main(string[] args)
        {
			var client = new ProSuiteGrpcClient(Host, Port);
			client.OnServiceResponseReceived += ServiceResponseReceived;

			var tokenSource = new CancellationTokenSource();
			client.CallServer(
				new ProSuiteGrpcServerRequest {
					ServiceType = ProSuiteGrpcServiceType.VerifyQuality,
					RequestData = {}
					}, tokenSource.Token).Wait();
		}

		private static void ServiceResponseReceived(object sender, ProSuiteGrpcEventArgs e)
		{
			var response = e?.Response;
			if (response == null)
				return;

			Console.WriteLine($"{response.ResponseMessage}");
		}
	}
}
