using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public interface IWorkspaceContext : IDatasetContext
	{
		[NotNull]
		IWorkspace Workspace { get; }

		[NotNull]
		IFeatureWorkspace FeatureWorkspace { get; }

		/// <summary>
		/// Gets the association for a fully qualified relationship class name.
		/// </summary>
		/// <param name="relationshipClassName">Name of the relationship class.</param>
		/// <returns></returns>
		[CanBeNull]
		Association GetAssociationByRelationshipClassName(
			[NotNull] string relationshipClassName);

		/// <summary>
		/// Gets the dataset by its fully qualified geodatabase name.
		/// </summary>
		/// <param name="gdbDatasetName">Database name of the dataset (in the master database)</param>
		/// <returns></returns>
		/// <remarks>must be called in a domain transaction</remarks>
		[CanBeNull]
		Dataset GetDatasetByGdbName([NotNull] string gdbDatasetName);

		bool Contains([NotNull] IDdxDataset dataset);

		bool Contains([NotNull] Association association);

		/// <summary>
		/// Gets the dataset by its model name.
		/// </summary>
		/// <param name="modelDatasetName">Model name of the dataset.</param>
		/// <returns></returns>
		/// <remarks>
		/// must be called in a domain transaction
		/// </remarks>
		[CanBeNull]
		Dataset GetDatasetByModelName([NotNull] string modelDatasetName);

		[CanBeNull]
		Association GetAssociationByModelName([NotNull] string associationName);
	}
}
