using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ProSuite.Commons.Logging;

namespace ProSuite.Microservices.Server.AO.QA
{
	public class RequestAdmin : IRequestAdmin
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly List<CancelableRequest> _requests = new List<CancelableRequest>();

		#region Implementation of IRequestAdmin

		public void CancelAllRequests()
		{
			foreach (CancelableRequest request in _requests)
			{
				request.TrackCancel.Cancel();
			}
		}

		public void CancelRequest(string requestUserName, string environment)
		{
			foreach (CancelableRequest request in _requests)
			{
				if (request.RequestUserName == requestUserName &&
				    request.Environment == environment)
				{
					_msg.WarnFormat(
						"Canceling request for user '{0}' in environment '{1}'",
						request.RequestUserName, request.Environment);
					request.TrackCancel.Cancel();
				}
			}
		}

		public CancelableRequest RegisterRequest(string requestUserName, string environment,
		                                         ITrackCancel trackCancel)
		{
			var request = new CancelableRequest(requestUserName, environment, trackCancel);

			_requests.Add(request);

			return request;
		}

		public void UnregisterRequest(CancelableRequest request)
		{
			_requests.Remove(request);
		}

		#endregion
	}
}
