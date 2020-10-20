using System;

namespace ProSuite.QA.ServiceManager.Types
{
	public enum ProSuiteQAServiceState
	{
		Started,
		Validated,
		Finished,
		//ProgressMessage,
		Progress,
		Other,
		//ResultsReceived,
		Info,
		Failed
	}

	public class ProSuiteQAServiceEventArgs : EventArgs
	{
		public ProSuiteQAServiceState State { get; }
		public object Data { get; }

		public ProSuiteQAServiceEventArgs(ProSuiteQAServiceState state, object data)
		{
			State = state;
			Data = data;
		}
	}
}
