
using System;
using System.Threading;

namespace ProSuite.GrpcClient.TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
			var client = new ProSuiteGrpcClient("localhost", 30021);
			client.OnServiceResponseReceived += ServiceResponseReceived;

			var tokenSource = new CancellationTokenSource();
			client.CallServer(
				new ProSuiteGrpcServerRequest {
					ServiceType = ProSuiteGrpcServiceType.VerifyQuality,
					RequestData = {}
					}, tokenSource).Wait();
		}

		private static void ServiceResponseReceived(object sender, ProSuiteGrpcEventArgs e)
		{
			var response = e?.Data as ProSuiteGrpcServerResponse;
			if (response == null)
				return;

			Console.WriteLine($"{response.ResponseMessage}");
		}
	}
}
