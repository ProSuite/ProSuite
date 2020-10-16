namespace ProSuite.GrpcClient
{
	public enum ProSuiteGrpcServiceType
	{
		VerifyQuality,
		Reconcile
	}

	public class ProSuiteGrpcServerRequest
	{
		public ProSuiteGrpcServiceType ServiceType { get; set; }

		public object RequestData { get; set; }
	}



}
