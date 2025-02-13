using ArcGIS.Desktop.Core.Geoprocessing;
using ProSuite.Commons.Logging;
using ProSuite.QA.ServiceManager.Interfaces;
using ProSuite.QA.ServiceManager.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ProSuite.QA.ServiceProviderArcGIS
{
	public enum ProSuiteQAToolType
	{
		Basic,
		Xml
	}

	// TODO - is REST an alternative (faster) solution for GPService? 
	// https://vsdev2414.esri-de.com/server/rest/services/PROSUITE_QA/verification/GPServer/verifydataset/execute?object_class=%5C%5Cvsdev2414%5Cprosuite_server_trials%5Ctestdata.gdb%5Cpolygons&tile_size=10000&parameters=&verification_extent=&env%3AoutSR=&env%3AprocessSR=&returnZ=false&returnM=false&returnTrueCurves=false&returnFeatureCollection=false&context=&f=json

	public class QAServiceProviderGP : ProSuiteQAServiceProviderBase<ProSuiteQAServerConfiguration>, IProSuiteQAServiceProvider
	{
		private static readonly IMsg _msg = new Msg(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private string _toolpath;
		private readonly Regex _regex = new Regex("(?<=>)(.*?)(?=<)", RegexOptions.Singleline);

		public ProSuiteQAServiceType ServiceType { get; }

		public QAServiceProviderGP(ProSuiteQAServerConfiguration parameters) : base(parameters)
		{
			ServiceType = parameters.ServiceType;
			_toolpath = BuildToolPath(parameters);// $"{parameters.ServiceConnection}\\{parameters.ServiceName}";
		}

		private string BuildToolPath(ProSuiteQAServerConfiguration parameters)
		{
			if (string.IsNullOrEmpty(parameters.ServiceConnection) || string.IsNullOrEmpty(parameters.ServiceConnection)) return String.Empty;

			return $"{parameters.ServiceConnection}\\{parameters.ServiceName}";
		}

		public event EventHandler<ProSuiteQAServiceEventArgs> OnStatusChanged;

		public async Task<ProSuiteQAResponse> StartQAAsync(ProSuiteQARequest parameters)
		{
			var cts = new CancellationTokenSource();

			var args = PrepareGPToolParameters(parameters, ProSuiteQAToolType.Xml);
			if (args == null)
			{
				return new ProSuiteQAResponse()
				       {
					       Error = ProSuiteQAError.ServiceFailed
				       };
			}

			// background thread
			GPExecuteToolFlags flags = GPExecuteToolFlags.GPThread;

			// silent call of GP Server Toolbox
			var result = await Geoprocessing.ExecuteToolAsync(_toolpath, args, null, cts.Token, GPEventHandler, flags);
			return FormatProSuiteResponse(result);
		}

		private void GPEventHandler(string eventName, object o)
		{
			switch (eventName)
			{
				case "OnValidate":
					OnStatusChanged?.Invoke(this, new ProSuiteQAServiceEventArgs(ProSuiteQAServiceState.Validated, null));
					break;

				case "OnProgressMessage":
					OnStatusChanged?.Invoke(this, new ProSuiteQAServiceEventArgs(ProSuiteQAServiceState.ProgressPos, (string)o));
					break;

				case "OnProgressPos":
					OnStatusChanged?.Invoke(this, new ProSuiteQAServiceEventArgs(ProSuiteQAServiceState.ProgressPos, (int)o));
					break;

				case "OnEndExecute":
					var result = o as IGPResult;
					OnStatusChanged?.Invoke(this, new ProSuiteQAServiceEventArgs(ProSuiteQAServiceState.Finished, result?.Values?.First()));
					break;

				case "OnStartExecute":
					OnStatusChanged?.Invoke(this, new ProSuiteQAServiceEventArgs(ProSuiteQAServiceState.Started, null));
					break;

				case "OnMessage":
					var messageText = o.ToString();
					var match = _regex.Match(messageText);
					if (match.Success)
						messageText = match.Value;
					OnStatusChanged?.Invoke(this, new ProSuiteQAServiceEventArgs(ProSuiteQAServiceState.Info, messageText));
					break;

				default:
					OnStatusChanged?.Invoke(this, new ProSuiteQAServiceEventArgs(ProSuiteQAServiceState.Other, eventName));
					break;

			}

		}

		public ProSuiteQAResponse StartQASync(ProSuiteQARequest parameters)
		{
			var args = PrepareGPToolParameters(parameters, ProSuiteQAToolType.Xml);
			if (args == null)
			{
				return new ProSuiteQAResponse()
				       {
					       Error = ProSuiteQAError.ServiceFailed
				       };
			}

			Geoprocessing.OpenToolDialog(_toolpath, args, null, false,
				(event_name, o) =>
				{
					if (event_name == "OnEndExecute")
					{
						var result = o as IGPResult;
						OnStatusChanged?.Invoke(this, new ProSuiteQAServiceEventArgs(ProSuiteQAServiceState.Finished, result?.Values?.First()));
					}
					else // TODO other events than "OnStartExecute" ?
					{

					}

				});
			return null;
		}

		// every service provider is responsible for converting of input/output data (common data format converters?)
		#region Data converters

		private IReadOnlyList<string> PrepareGPToolParameters(ProSuiteQARequest parameters, ProSuiteQAToolType toolType)
		{
			try
			{
				var toolParams = parameters.RequestData?.ToString();
				if ( toolParams == null)
					return null;

				var localParams = toolParams.Split(',');
				return Geoprocessing.MakeValueArray(localParams);
			}
			catch (Exception ex)
			{
				_msg.Error(ex.Message);
			}
			return null;
		}

		private ProSuiteQAResponse FormatProSuiteResponse(IGPResult result)
		{
			// TODO return response data by Finished event or here?
			return new ProSuiteQAResponse
			{
				Error = ParseGPResultError(result),
				ErrorMessage = result.ErrorMessages.FirstOrDefault()?.Text,
				ResponseData = ParseGPResultValues(result)
			};
		}

		// TODO temporary extract results only from known QA XML GP Service 
		// - first vales is path to zip
		// - second will be error counts (not implemented)
		private object ParseGPResultValues(IGPResult result)
		{
			if (result?.ReturnValue == null) return null;

			// TODO - should set return data type
			return result.Values.FirstOrDefault();
		}

		private ProSuiteQAError ParseGPResultError(IGPResult result)
		{
			if (result == null) return ProSuiteQAError.ServiceFailed;

			if (result.ErrorCode == 0) return ProSuiteQAError.None;
			if (result.IsFailed) return ProSuiteQAError.ServiceFailed;
			if (result.IsCanceled) return ProSuiteQAError.Canceled;
			return ProSuiteQAError.ServiceFailed;
		}

		public void UpdateConfig(ProSuiteQAServerConfiguration serviceConfig)
		{
			if (ServiceType == serviceConfig.ServiceType)
			{
				_toolpath = BuildToolPath(serviceConfig);
			}
		}

		#endregion
	}


}
