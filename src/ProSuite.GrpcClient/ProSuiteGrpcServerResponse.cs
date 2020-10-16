using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProSuite.GrpcClient
{
	public class ProSuiteGrpcServerResponse
	{
		public ProSuiteGrpcServiceType RequestType { get; set; }

		public string ResponseMessage { get; set; }		

		public object ResponseData { get; set; }
	}
}
