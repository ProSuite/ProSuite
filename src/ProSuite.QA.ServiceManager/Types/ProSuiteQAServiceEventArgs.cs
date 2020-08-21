using System;

namespace ProSuite.QA.ServiceManager.Types
{
	public enum ProSuiteQAServiceState
	{
		Idle,
		Started,
		Validated,
		Finished,
		ProgressMessage,
		ProgressPos,
		Other,
		ResultsReceived,
		Info
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
