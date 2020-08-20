using System;

namespace ProSuite.Commons.QA.ServiceManager.Types
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
		public ProSuiteQAServiceState State { get; set; }
		public object Data { get; set; }

		public ProSuiteQAServiceEventArgs( ProSuiteQAServiceState state, object data)
		{
			this.State = state;
			this.Data = data;
		}

	}
}
