
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Progress;
using System.Collections.Generic;

namespace ProSuite.DomainServices.AO.QA
{
	public interface ISubverificationObserver
	{
		void CreatedSubverification(
			int idSubverification,
			QualityConditionExecType execType,
			[NotNull] IList<string> QualityConditionNames,
			[CanBeNull] IEnvelope area);
		void Finished(int id, ServiceCallStatus failed);
		void Started(int id, string workerAddress);
	}
}