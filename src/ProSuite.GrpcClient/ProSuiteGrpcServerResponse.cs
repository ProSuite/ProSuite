namespace ProSuite.GrpcClient
{
	public enum ProSuiteGrpcServerResponseStatus
	{
		Started,
		Progress,
		Finished,
		Done,
		Failed,
		Info,
		Other
	}

	public class ProSuiteGrpcServerResponse
	{
		public ProSuiteGrpcServiceType RequestType { get; set; }

		public ProSuiteGrpcServerResponseStatus Status { get; set; }

		public string ResponseMessage { get; set; }		

		public object ResponseData { get; set; }
	}
}
