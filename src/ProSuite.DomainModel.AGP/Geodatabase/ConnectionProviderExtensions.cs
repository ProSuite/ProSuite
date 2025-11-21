using System;
using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.Geodatabase;

namespace ProSuite.DomainModel.AGP.Geodatabase
{
	public static class ConnectionProviderExtensions
	{
		public static ArcGIS.Core.Data.Geodatabase OpenGeodatabase(
			this ConnectionProvider connectionProvider)
		{
			if (connectionProvider is null)
				throw new ArgumentNullException(nameof(connectionProvider));

			Connector connector;

			switch (connectionProvider)
			{
				case FileGdbConnectionProvider fileGdbConnectionProvider:
					connector =
						new FileGeodatabaseConnectionPath(new Uri(fileGdbConnectionProvider.Path));
					break;
				case MobileGdbConnectionProvider mobileGdbConnectionProvider:
					connector =
						new MobileGeodatabaseConnectionPath(
							new Uri(mobileGdbConnectionProvider.Path));
					break;
				case ConnectionFileConnectionProvider connectionFileConnectionProvider:
					connector =
						new DatabaseConnectionFile(new Uri(connectionFileConnectionProvider.Path));
					break;
				case SdeDirectConnectionProvider sdeDirectConnectionProvider:
					connector = GetDatabaseConnectionProperties(sdeDirectConnectionProvider);
					break;
				default:
					throw new NotSupportedException(
						$"Connection provider type not supported: {connectionProvider.GetType().Name}");
			}

			return (ArcGIS.Core.Data.Geodatabase) WorkspaceUtils.OpenDatastore(
				Assert.NotNull(connector));
		}

		private static DatabaseConnectionProperties GetDatabaseConnectionProperties(
			[NotNull] SdeDirectConnectionProvider dcConnectionProvider)
		{
			EnterpriseDatabaseType enterpriseDatabaseType =
				ToEnterpriseDatabaseType(dcConnectionProvider.DatabaseType);

			var result = new DatabaseConnectionProperties(enterpriseDatabaseType);

			AuthenticationMode authMode;
			string user = null, plainTextPassword = null;

			if (dcConnectionProvider is SdeDirectDbUserConnectionProvider
			    sdeDirectDbUserConnectionProvider)
			{
				authMode = AuthenticationMode.DBMS;
				user = sdeDirectDbUserConnectionProvider.UserName;
				plainTextPassword = sdeDirectDbUserConnectionProvider.PlainTextPassword;
			}
			else
			{
				authMode = AuthenticationMode.OSA;
			}

			result.AuthenticationMode = authMode;
			result.Instance = dcConnectionProvider.DatabaseName;
			result.Database = dcConnectionProvider.RepositoryName;

			result.Version = dcConnectionProvider.VersionName;
			result.User = user;
			result.Password = plainTextPassword;

			return result;
		}

		private static EnterpriseDatabaseType ToEnterpriseDatabaseType(DatabaseType databaseType)
		{
			switch (databaseType)
			{
				case DatabaseType.SqlServer:
					return EnterpriseDatabaseType.SQLServer;

				case DatabaseType.PostgreSQL:
					return EnterpriseDatabaseType.PostgreSQL;

				case DatabaseType.Oracle:

				case DatabaseType.Oracle9:

				case DatabaseType.Oracle10:

				case DatabaseType.Oracle11:
					return EnterpriseDatabaseType.Oracle;

				default:
					throw new ArgumentOutOfRangeException(nameof(databaseType), databaseType, null);
			}
		}
	}
}
