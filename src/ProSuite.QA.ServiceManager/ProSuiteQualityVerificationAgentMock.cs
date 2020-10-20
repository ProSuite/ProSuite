using System;
using System.Threading;
using System.Threading.Tasks;
using ProSuite.QA.ServiceManager.Types;

namespace ProSuite.QA.ServiceManager
{
	// TODO temporary for testing here !!
	
	// this class is reponsible for quality verfication: ProSuite or 

	// TODO rename ProSuiteQARe.... to QualityVerficationRe...
	public interface IQualityVerificationAgent
	{
		event EventHandler<ProSuiteQAServiceEventArgs> OnStatusChanged;
		event EventHandler<ProSuiteQAServiceEventArgs> OnCompleted;
		event EventHandler<ProSuiteQAServiceEventArgs> OnError;
		ProSuiteQAResponse DoQualityVerification(ProSuiteQARequest request);
	}

	public class ProSuiteQualityVerificationAgentMock : IQualityVerificationAgent
	{
		public event EventHandler<ProSuiteQAServiceEventArgs> OnStatusChanged;
		public event EventHandler<ProSuiteQAServiceEventArgs> OnCompleted;
		public event EventHandler<ProSuiteQAServiceEventArgs> OnError;

		public ProSuiteQAResponse DoQualityVerification(ProSuiteQARequest request)
		{
			// TODO try catch OnError
			for (int i = 1; i < 5; i++)
			{
				var resp = PerformQualityVerificationStep();
				OnStatusChanged?.Invoke(
					this,
					new ProSuiteQAServiceEventArgs(
						ProSuiteQAServiceState.Progress,
						resp));
			}

			var response = new ProSuiteQAResponse() { };
			OnCompleted?.Invoke(this, new ProSuiteQAServiceEventArgs(ProSuiteQAServiceState.Finished, response));
			return response;
		}


		private ProSuiteQAResponse PerformQualityVerificationStep()
		{
			Thread.Sleep(1000);
			return new ProSuiteQAResponse();
		}
	}

}
