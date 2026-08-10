using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.Distributed
{
	/// <summary>
	/// High-level interface that defines the replica content. Used by client code to configure
	/// an IReplicaDescription object. The replica content definition. Implementors need to
	/// provide the root datasets.
	/// The related datasets will be determined automatically through relationship classes
	/// referenced by the root datasets and implementors can configure filters for them.
	/// </summary>
	public interface IReplicaContent
	{
		/// <summary>
		/// The vector datasets to be included in the replica, which indirectly determine the
		/// other, related, datasets to be included.
		/// </summary>
		[NotNull]
		IEnumerable<IDataset> RootDatasets { get; }

		/// <summary>
		/// The master workspace from which the replica is created.
		/// </summary>
		[NotNull]
		IFeatureWorkspace MasterWorkspace { get; }

		/// <summary>
		/// Configures the filter options for a related table based on the specified relationship
		/// class information.
		/// </summary>
		/// <param name="relClassInfo">The relationship class information used to configure the
		/// relationship. Cannot be <see langword="null"/>.</param>
		void ConfigureRelation([NotNull] IRelationshipClassInfo relClassInfo);

		/// <summary>
		/// Configures the replica filter for a given dataset.
		/// </summary>
		/// <param name="datasetName">dataset name.</param>
		/// <param name="filter">The replica filter to configure.</param>
		/// <returns><c>true</c> if the dataset is to be included in the replica, 
		/// <c>false</c> if it should be excluded.</returns>
		bool ConfigureFilter([NotNull] IDatasetName datasetName,
		                     [NotNull] IDatasetReplicaFilter filter);
	}
}
