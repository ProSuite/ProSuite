using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Text;

namespace ProSuite.DomainModel.AO.Geodatabase
{
	public class SdeDirectOsaConnectionProvider : SdeDirectConnectionProvider
	{
		#region Constructors

		/// <remarks>Required for NHibernate</remarks>
		public SdeDirectOsaConnectionProvider() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="SdeDirectOsaConnectionProvider"/> class.
		/// </summary>
		/// <param name="databaseType">Type of the database.</param>
		/// <param name="databaseName">Name of the database.</param>
		public SdeDirectOsaConnectionProvider(DatabaseType databaseType,
		                                      string databaseName)
			: this(databaseType, databaseName, DefaultRepositoryName) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="SdeDirectOsaConnectionProvider"/> class.
		/// </summary>
		/// <param name="databaseType">Type of the database.</param>
		/// <param name="databaseName">Name of the database.</param>
		/// <param name="repositoryName">Name of the SDE repository.</param>
		public SdeDirectOsaConnectionProvider(DatabaseType databaseType,
		                                      string databaseName,
		                                      string repositoryName)
			: base(GetDefaultName(databaseName, repositoryName),
			       databaseType, databaseName, repositoryName) { }

		#endregion

		protected override IFeatureWorkspace OpenWorkspaceCore(string versionName = null,
		                                                       int hWnd = 0)
		{
			return (IFeatureWorkspace) WorkspaceUtils.OpenSDEWorkspace(
				RepositoryName,
				GetDirectConnectDriver(),
				DatabaseName,
				StringUtils.IsNotEmpty(versionName)
					? versionName
					: VersionName);
		}

		private static string GetDefaultName(string databaseName, string repositoryName)
		{
			return $"{databaseName}:{repositoryName}:OSA";
		}

		public override string TypeDescription => "SDE Direct OSA Connection";
	}
}
