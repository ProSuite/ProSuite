using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.DomainModel.AO.Geodatabase
{
	public abstract class SdeDirectConnectionProvider : SdeConnectionProvider,
	                                                    IWorkspaceDbConnectionProvider
	{
		[UsedImplicitly] private string _databaseName;

		[UsedImplicitly] private DatabaseType _databaseType = DatabaseType.SqlServer;

		#region Constructors

		protected SdeDirectConnectionProvider() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="SdeDirectConnectionProvider"/> class.
		/// </summary>
		/// <param name="name">The name of the connection provider.</param>
		/// <param name="databaseType">The direct connect driver.</param>
		/// <param name="databaseName">Name of the database.</param>
		/// <param name="repositoryName">Name of the SDE repository.</param>
		protected SdeDirectConnectionProvider(string name,
		                                      DatabaseType databaseType,
		                                      [NotNull] string databaseName,
		                                      [NotNull] string repositoryName)
			: base(name, repositoryName)
		{
			Assert.ArgumentNotNullOrEmpty(databaseName, nameof(databaseName));
			Assert.ArgumentNotNullOrEmpty(repositoryName, nameof(repositoryName));

			_databaseType = databaseType;
			_databaseName = databaseName;
		}

		#endregion

		[UsedImplicitly]
		public DatabaseType DatabaseType
		{
			get { return _databaseType; }
			set { _databaseType = value; }
		}

		[UsedImplicitly]
		public string DatabaseName
		{
			get { return _databaseName; }
			set { _databaseName = value; }
		}

		public sealed override IFeatureWorkspace OpenWorkspace(int hWnd = 0)
		{
			return OpenWorkspace(null, hWnd);
		}

		public override IFeatureWorkspace OpenWorkspace(string versionName, int hWnd = 0)
		{
			return HasAlternateCredentials
				       ? OpenWorkspace(AlternateUserName, AlternatePassword, versionName, hWnd)
				       : OpenWorkspaceCore(versionName, hWnd);
		}

		#region IWorkspaceDbConnectionProvider Members

		public string SdeRepositoryOwner => RepositoryName;

		public esriConnectionDBMS Dbms => GetConnectionDbms(DatabaseType);

		#endregion

		[NotNull]
		protected abstract IFeatureWorkspace OpenWorkspaceCore(
			[CanBeNull] string versionName = null,
			int hWnd = 0);

		[NotNull]
		protected IFeatureWorkspace OpenWorkspace([NotNull] string userName,
		                                          [NotNull] string password,
		                                          [CanBeNull] string versionName = null,
		                                          int hWnd = 0)
		{
			Assert.ArgumentNotNullOrEmpty(userName, nameof(userName));
			Assert.ArgumentNotNullOrEmpty(password, nameof(password));

			return (IFeatureWorkspace) WorkspaceUtils.OpenSDEWorkspace(
				RepositoryName, GetDirectConnectDriver(),
				DatabaseName,
				userName, password,
				StringUtils.IsNotEmpty(versionName)
					? versionName
					: VersionName,
				hWnd);
		}

		protected DirectConnectDriver GetDirectConnectDriver()
		{
			return GetDirectConnectDriver(_databaseType);
		}

		private static esriConnectionDBMS GetConnectionDbms(DatabaseType databaseType)
		{
			switch (databaseType)
			{
				case DatabaseType.Oracle:
				case DatabaseType.Oracle9:
				case DatabaseType.Oracle10:
				case DatabaseType.Oracle11:
					return esriConnectionDBMS.esriDBMS_Oracle;

				case DatabaseType.SqlServer:
					return esriConnectionDBMS.esriDBMS_SQLServer;

				case DatabaseType.PostgreSQL:
					return esriConnectionDBMS.esriDBMS_PostgreSQL;

				default:
					throw new ArgumentOutOfRangeException(nameof(databaseType), databaseType,
					                                      @"Unsupported database type.");
			}
		}

		private static DirectConnectDriver GetDirectConnectDriver(DatabaseType databaseType)
		{
			switch (databaseType)
			{
				case DatabaseType.Oracle:
					return DirectConnectDriver.Oracle;

				case DatabaseType.Oracle9:
					return DirectConnectDriver.Oracle9i;

				case DatabaseType.Oracle10:
					return DirectConnectDriver.Oracle10g;

				case DatabaseType.Oracle11:
					return DirectConnectDriver.Oracle11g;

				case DatabaseType.SqlServer:
					return DirectConnectDriver.SqlServer;

				case DatabaseType.PostgreSQL:
					return DirectConnectDriver.PostgreSQL;

				default:
					throw new NotSupportedException(
						$"Unsupported database type: {databaseType}");
			}
		}
	}
}
