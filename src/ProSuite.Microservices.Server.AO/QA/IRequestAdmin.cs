using ESRI.ArcGIS.esriSystem;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Microservices.Server.AO.QA
{
	public interface IRequestAdmin
	{
		void CancelAllRequests();

		void CancelRequest([NotNull] string requestUserName,
		                   [NotNull] string environment);

		CancelableRequest RegisterRequest(string requestUserName,
		                                  string environment,
		                                  ITrackCancel trackCancel);

		void UnregisterRequest(CancelableRequest request);
	}
}
