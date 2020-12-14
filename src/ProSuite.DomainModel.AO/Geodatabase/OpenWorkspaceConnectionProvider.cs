using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.Geodatabase
{
	[CLSCompliant(false)]
	public class OpenWorkspaceConnectionProvider : ConnectionProvider
	{
		private readonly IFeatureWorkspace _featureWorkspace;

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

			_featureWorkspace = featureWorkspace;
		}

		#region Overrides of ConnectionProvider

		public override IFeatureWorkspace OpenWorkspace(int hWnd = 0)
		{
			return _featureWorkspace;
		}

		#endregion
	}
}
