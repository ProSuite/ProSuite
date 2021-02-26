using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public interface IDatasetLookup
	{
		/// <summary>
		/// Gets the vector dataset for a given gdb feature.
		/// </summary>
		/// <param name="feature">The feature to get the dataset for.</param>
		/// <returns>VectorDataset instance, or null if no dataset is found 
		/// for the feature class that the feature belongs to.</returns>
		VectorDataset GetDataset([NotNull] IFeature feature);

		/// <summary>
		/// Gets the vector dataset for a given gdb feature class.
		/// </summary>
		/// <param name="featureClass">The feature class to get the dataset for.</param>
		/// <returns>VectorDataset instance, or null if no dataset is found 
		/// for the given feature class.</returns>
		VectorDataset GetDataset([NotNull] IFeatureClass featureClass);

		/// <summary>
		/// Gets the object dataset for a given gdb object.
		/// </summary>
		/// <param name="obj">The object to get the dataset for.</param>
		/// <returns>ObjectDataset instance, or null if no dataset is found 
		/// for the object class that the object belongs to.</returns>
		ObjectDataset GetDataset([NotNull] IObject obj);

		/// <summary>
		/// Gets the object dataset for a given gdb object class.
		/// </summary>
		/// <param name="objectClass">The object class to get the dataset for.</param>
		/// <returns>ObjectDataset instance, or null if no dataset is found 
		/// for the given object class.</returns>
		ObjectDataset GetDataset([NotNull] IObjectClass objectClass);

		/// <summary>
		/// Gets the dataset for a fully qualified name within a feature workspace
		/// </summary>
		/// <param name="workspace">The workspace the named dataset belongs to.</param>
		/// <param name="gdbDatasetName">The full name of the dataset.</param>
		/// <returns></returns>
		Dataset GetDataset([NotNull] IFeatureWorkspace workspace,
		                   [NotNull] string gdbDatasetName);

		/// <summary>
		/// Gets the attributed association for a row in an attributed relationship class.
		/// </summary>
		/// <param name="relationshipRow">The relationship row.</param>
		/// <returns></returns>
		AttributedAssociation GetAttributedAssociation([NotNull] IRow relationshipRow);

		/// <summary>
		/// Gets the association for a fully qualified relationship class name within a feature workspace
		/// </summary>
		/// <param name="workspace">The workspace the named association belongs to.</param>
		/// <param name="relationshipClassName">The full name of the relationship class.</param>
		/// <returns></returns>
		Association GetAssociation([NotNull] IFeatureWorkspace workspace,
		                           [NotNull] string relationshipClassName);

		/// <summary>
		/// Gets the ddx dataset that a given dataset name corresponds to.
		/// </summary>
		/// <param name="datasetName">The dataset name.</param>
		/// <returns>The corresponding ddx dataset, or null if the dataset name 
		/// refers to a workspace other than the model workspace, or if the dataset
		/// is not registered with the model.</returns>
		Dataset GetDataset([NotNull] IDatasetName datasetName);
	}
}
