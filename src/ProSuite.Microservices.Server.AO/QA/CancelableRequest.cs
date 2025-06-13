using ESRI.ArcGIS.esriSystem;

namespace ProSuite.Microservices.Server.AO.QA
{
	public class CancelableRequest
	{
		public CancelableRequest(string requestUserName,
		                         string environment,
		                         ITrackCancel trackCancel)
		{
			RequestUserName = requestUserName;
			Environment = environment;
			TrackCancel = trackCancel;
		}

		public string RequestUserName { get; }
		public string Environment { get; }
		public ITrackCancel TrackCancel { get; }
	}
}
