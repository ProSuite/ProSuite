using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.Geodatabase
{
	public interface IGdbWorkspaceProvider
	{
		/// <summary>
		/// Opens the workspace as specified by the provider properties. Always opens the
		/// DEFAULT version for the specified repository.
		/// </summary>
		/// <returns></returns>
		[NotNull]
		IWorkspace OpenWorkspace();

		/// <summary>
		/// Opens the feature workspace as specified by the provider properties. Always opens the
		/// DEFAULT version for the specified repository.
		/// </summary>
		/// <returns></returns>
		[NotNull]
		IFeatureWorkspace OpenFeatureWorkspace();

		void EnableSchemaCache();

		void DisableSchemaCache();

		DirectConnectDriver DirectConnectDriver { get; set; }

		/// <summary>
		/// Gets or sets the name of the SDE repository to use. Default value is SDE. Allows
		/// specifying a non-default SDE repository. 
		/// </summary>
		/// <value>The name of the repository.</value>
		string RepositoryName { get; set; }
	}
}
