using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
