using System;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestContainer
{
	internal interface ITestProgress
	{
		void OnProgressChanged(Step step, int current, int total,
		                       [NotNull] IEnvelope currentEnvelope,
		                       [NotNull] IEnvelope allBox);

		void OnProgressChanged(Step step, int current, int total, object tag);

		[NotNull]
		IDisposable UseProgressWatch(Step startStep, Step endStep,
		                             int current, int total,
		                             object tag);
	}
}
