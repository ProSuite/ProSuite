using ProSuite.Commons.Logging;
using ProSuite.QA.ServiceManager.Interfaces;
using ProSuite.QA.ServiceManager.Types;
using System;
using System.Threading.Tasks;

namespace ProSuite.QA.ServiceProviderMicroservices
{
	public class QAServiceProviderMicroservices : ProSuiteQAServiceProviderBase<ProSuiteQAServerConfiguration>, IProSuiteQAServiceProvider
	{
		private static readonly IMsg _msg = new Msg(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		// path to medium - client.exe
		private string _clientPath = @"test_client.exe";

		//private gRPCClient _serverClient = null;
		private string _serverAdress;
		private string _serverPort;

		public ProSuiteQAServiceType ServiceType => ProSuiteQAServiceType.gRPC;

		public event EventHandler<ProSuiteQAServiceEventArgs> OnStatusChanged;

		public QAServiceProviderMicroservices(ProSuiteQAServerConfiguration parameters) : base(parameters)
		{
			_serverAdress = parameters.ServiceConnection;
			_serverPort = parameters.ServiceName;

			// TODO algr: async start
			InitializeClient(_clientPath);
		}

		private void InitializeClient(string path)
		{
			//_serverClient = new gRPCClient(path);
			//_serverClient.Start();
		}

		public Task<ProSuiteQAResponse> StartQAAsync(ProSuiteQARequest request)
		{
			throw new NotImplementedException();
		}

		public ProSuiteQAResponse StartQASync(ProSuiteQARequest request)
		{
			throw new NotImplementedException();
		}

		public void UpdateConfig(ProSuiteQAServerConfiguration serviceConfig)
		{
			throw new NotImplementedException();
		}
	}
}
