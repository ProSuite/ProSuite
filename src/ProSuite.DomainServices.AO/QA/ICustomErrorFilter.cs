using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA
{
	/// <summary>
	/// Extra error filtering used for very specific topgis requirements.
	/// </summary>
	public interface ICustomErrorFilter
	{
		bool IsRelevantDueToErrorGeometry([CanBeNull] IGeometry qaErrorGeometry,
		                                  [CanBeNull] IGeometry testPerimeter);

		bool IsRelevantDueToReferenceGeometry(
			[NotNull] QaError qaError,
			[NotNull] QualityCondition qualityCondition,
			[NotNull] IQualityConditionObjectDatasetResolver datasetResolver,
			[CanBeNull] IGeometry referenceGeometry,
			[CanBeNull] IGeometry testPerimeter,
			IVerificationContext verificationContext,
			[CanBeNull] ILocationBasedQualitySpecification locationBasedQualitySpecification);
	}
}
