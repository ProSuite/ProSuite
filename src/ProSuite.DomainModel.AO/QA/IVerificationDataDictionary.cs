using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.Workflow;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainModel.AO.QA
{
	/// <summary>
	/// Provides coarse-granular access to data dictionary entities, including extra domain logic
	/// for converting input parameters and looking up intermediate entities.
	/// </summary>
	public interface IVerificationDataDictionary<TModel> where TModel : ProductionModel
	{
		IList<QualitySpecification> GetQualitySpecifications(
			[NotNull] IList<int> datasetIds,
			bool includeHidden);

		[CanBeNull]
		QualitySpecification GetQualitySpecification(int qualitySpecificationId);

		// TODO: Use IGdbTable (In Commons.Gdb) instead of IObjectClass, wrap IObjectClass
		IList<ProjectWorkspaceBase<Project<TModel>, TModel>> GetProjectWorkspaceCandidates(
			[NotNull] IList<IObjectClass> objectClasses);

		QualityCondition GetQualityCondition(string conditionName);

		IList<Dataset> GetDatasets(IList<int> datasetIds);

		IList<Association> GetAssociations(IList<int> referencedDatasetIds);
	}
}
