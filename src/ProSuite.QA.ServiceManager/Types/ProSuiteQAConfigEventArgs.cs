using System;

namespace ProSuite.QA.ServiceManager.Types
{
	public class ProSuiteQAConfigEventArgs : EventArgs
	{
		public object Data { get; }

		public ProSuiteQAConfigEventArgs(object data)
		{
			Data = data;
		}
	}
}
