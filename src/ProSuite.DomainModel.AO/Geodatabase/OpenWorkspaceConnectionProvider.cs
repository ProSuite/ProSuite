using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.DomainModel.Core.Geodatabase;

namespace ProSuite.DomainModel.AO.Geodatabase
{
	public class OpenWorkspaceConnectionProvider : ConnectionProvider
	{
		[NotNull] public readonly IFeatureWorkspace FeatureWorkspace;

		/// <summary>
		/// Initializes a new instance of the <see cref="OpenWorkspaceConnectionProvider"/> class.
		/// </summary>
		/// <param name="workspace">The workspace.</param>
		public OpenWorkspaceConnectionProvider([NotNull] IWorkspace workspace)
			: this((IFeatureWorkspace) workspace) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="OpenWorkspaceConnectionProvider"/> class.
		/// </summary>
		/// <param name="featureWorkspace">The feature workspace.</param>
		public OpenWorkspaceConnectionProvider([NotNull] IFeatureWorkspace featureWorkspace)
		{
			Assert.ArgumentNotNull(featureWorkspace, nameof(featureWorkspace));

			FeatureWorkspace = featureWorkspace;
		}

		#region Overrides of ConnectionProvider

		public override DbConnectionType ConnectionType => DbConnectionType.Other;

		#endregion
	}
}
