using System;

namespace ProSuite.GrpcClient
{
	public class ProSuiteGrpcEventArgs : EventArgs
	{
		public ProSuiteGrpcServerResponse Response { get; }

		public ProSuiteGrpcEventArgs(ProSuiteGrpcServerResponse response)
		{
			Response = response;
		}
	}
}
