using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.Commons.AO.Geodatabase.Distributed
{
	public interface IReplicaDescription
	{
		IGeometry FilterGeometry { get; set; }

		/// <summary>
		/// The replica's dataset names, i.e. the feature datasets and standalone datasets
		/// to be included in the replica.
		/// </summary>
		ICollection<IDataset> Datasets { get; }

		/// <summary>
		/// Returns the relevant relationship class infos which can be configured by client code.
		/// </summary>
		/// <returns></returns>
		IList<IRelationshipClassInfo> GetRelationshipClassInfos();

		/// <summary>
		/// Returns the dataset filter to be configured for the specified dataset.
		/// </summary>
		/// <param name="dataset"></param>
		/// <returns></returns>
		IDatasetReplicaFilter GetDatasetFilter(IDataset dataset);

		/// <summary>
		/// Excludes the specified dataset from the replica.
		/// </summary>
		/// <param name="dataset">The dataset to evaluate for exclusion. Cannot be null.</param>
		/// <returns><see langword="true"/> if the dataset was successfully removed from the
		/// list of datasets; otherwise, <see langword="false"/>.</returns>
		bool ExcludeTable(IDataset dataset);
	}
}
