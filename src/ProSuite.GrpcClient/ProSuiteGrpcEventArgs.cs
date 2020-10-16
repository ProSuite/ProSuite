using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProSuite.GrpcClient
{
	public class ProSuiteGrpcEventArgs : EventArgs
	{
		public object Data { get; }

		public ProSuiteGrpcEventArgs(object data)
		{
			Data = data;
		}
	}
}
