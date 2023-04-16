using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainServices.AO.QA.VerifiedDataModel
{
	public interface IVerifiedModelFactory
	{
		/// <summary>
		/// Creates the model and harvests all the datasets that are supported by the current environment.
		/// No spatial reference descriptor is assigned!
		/// </summary>
		/// <param name="workspace"></param>
		/// <param name="name"></param>
		/// <param name="databaseName"></param>
		/// <param name="schemaOwner"></param>
		/// <param name="usedDatasetNames"></param>
		/// <returns></returns>
		[NotNull]
		Model CreateModel([NotNull] IWorkspace workspace,
		                  [NotNull] string name,
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
			[NotNull] Model model,
			[NotNull] IEnumerable<Dataset> usedDatasets);
	}
}
