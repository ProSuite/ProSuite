using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainServices.AO.QA.VerifiedDataModel
{
	public interface IVerifiedModelFactory
	{
		/// <summary>
		/// Creates the model and harvests all the datasets that are supported by the current environment.
		/// Depending on the implementation, no spatial reference descriptor is assigned. In that case,
		/// <see cref="AssignMostFrequentlyUsedSpatialReference"/> could be used.
		/// </summary>
		/// <param name="workspace">The model's workspace</param>
		/// <param name="modelName">The model name</param>
		/// <param name="modelId">The model's original (unique) Id which will be maintained to clearly identify it.</param>
		/// <param name="databaseName"></param>
		/// <param name="schemaOwner"></param>
		/// <param name="usedDatasetNames">The dataset names that are actually going to be used. Other dataset names
		/// do not need to be harvested.</param>
		/// <returns></returns>
		[NotNull]
		DdxModel CreateModel([NotNull] IWorkspace workspace,
		                     [NotNull] string modelName,
		                     int modelId,
		                     [CanBeNull] string databaseName,
		                     [CanBeNull] string schemaOwner,
		                     [CanBeNull] IList<string> usedDatasetNames = null);

		/// <summary>
		/// Assigns the spatial reference of the most frequently used datasets. The specified
		/// datasets enumerable contains all usages of the datasets in the model.
		/// </summary>
		/// <param name="model"></param>
		/// <param name="usedDatasets"></param>
		/// <returns></returns>
		void AssignMostFrequentlyUsedSpatialReference(
			[NotNull] DdxModel model,
			[NotNull] IEnumerable<Dataset> usedDatasets);
	}
}
