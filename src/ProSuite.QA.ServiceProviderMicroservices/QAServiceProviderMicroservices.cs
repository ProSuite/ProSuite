using ProSuite.Commons.Logging;
using ProSuite.GrpcClient;
using ProSuite.QA.ServiceManager.Interfaces;
using ProSuite.QA.ServiceManager.Types;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProSuite.QA.ServiceProviderMicroservices
{
	public class QAServiceProviderMicroservices : ProSuiteQAServiceProviderBase<ProSuiteQAServerConfiguration>, IProSuiteQAServiceProvider
	{
		private static readonly IMsg _msg = new Msg(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private ProSuiteGrpcClient _serviceClient;

		public ProSuiteQAServiceType ServiceType => ProSuiteQAServiceType.gRPC;

		public event EventHandler<ProSuiteQAServiceEventArgs> OnStatusChanged;

		public QAServiceProviderMicroservices(ProSuiteQAServerConfiguration parameters) : base(parameters)
		{
			_serviceClient = new ProSuiteGrpcClient(parameters.ServiceName, Convert.ToInt32(parameters.ServiceConnection));
			_serviceClient.OnServiceResponseReceived += ServiceResponseReceived;
		}

		private void ServiceResponseReceived(object sender, ProSuiteGrpcEventArgs e)
		{
			OnStatusChanged?.Invoke(this, ParseArguments(e));
		}

		public async Task<ProSuiteQAResponse> StartQAAsync(ProSuiteQARequest request, CancellationToken token)
		{
			await _serviceClient.CallServer(new ProSuiteGrpcServerRequest { }, token);
			return new ProSuiteQAResponse { };
		}

		public ProSuiteQAResponse StartQASync(ProSuiteQARequest request, CancellationToken token)
		{
			throw new NotImplementedException();
		}

		public void UpdateConfig(ProSuiteQAServerConfiguration serviceConfig)
		{
			throw new NotImplementedException();
		}

		private ProSuiteQAServiceEventArgs ParseArguments(ProSuiteGrpcEventArgs serviceArguments)
		{
			var serviceResponse = serviceArguments?.Response;
			return new ProSuiteQAServiceEventArgs(ParseState(serviceResponse?.Status), serviceResponse?.ResponseData);
		}

		// TODO too many state parsings - common state enum could solve this
		private ProSuiteQAServiceState ParseState(ProSuiteGrpcServerResponseStatus? status)
		{
			if (status == null) return ProSuiteQAServiceState.Other;

			switch (status)
			{
				case ProSuiteGrpcServerResponseStatus.Progress:
					return ProSuiteQAServiceState.ProgressPos;
				case ProSuiteGrpcServerResponseStatus.Finished:
					return ProSuiteQAServiceState.Finished;
			}
			return ProSuiteQAServiceState.Other;
		}
	}
}
