using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProSuite.Commons.QA.ServiceManager.Types
{
	public class ProSuiteQAConfigEventArgs : EventArgs
	{
		public object Data { get; set; }

		public ProSuiteQAConfigEventArgs(object data)
		{
			this.Data = data;
		}
	}
}
