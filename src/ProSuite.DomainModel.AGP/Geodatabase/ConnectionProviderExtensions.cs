using System;
using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.Geodatabase;

namespace ProSuite.DomainModel.AGP.Geodatabase
{
	public static class ConnectionProviderExtensions
	{
		public static ArcGIS.Core.Data.Geodatabase OpenGeodatabase(
			this ConnectionProvider connectionProvider)
		{
			Connector connector = GetConnector(connectionProvider);

			Datastore datastore = WorkspaceUtils.OpenDatastore(connector);

			return datastore as ArcGIS.Core.Data.Geodatabase ??
			       throw new InvalidOperationException("Not a Geodatabase instance");
		}

		[NotNull]
		private static Connector GetConnector(ConnectionProvider connectionProvider)
		{
			if (connectionProvider is null)
				throw new ArgumentNullException(nameof(connectionProvider));

			if (connectionProvider is FileGdbConnectionProvider fileGdbProvider)
			{
				var path = new Uri(fileGdbProvider.Path);
				return new FileGeodatabaseConnectionPath(path);
			}

			if (connectionProvider is MobileGdbConnectionProvider mobileGdbProvider)
			{
				var path = new Uri(mobileGdbProvider.Path);
				return new MobileGeodatabaseConnectionPath(path);
			}

			if (connectionProvider is ConnectionFileConnectionProvider connectionFileProvider)
			{
				var path = new Uri(connectionFileProvider.Path);
				return new DatabaseConnectionFile(path);
			}

			if (connectionProvider is SdeDirectConnectionProvider sdeDirectConnectionProvider)
			{
				var properties = GetDatabaseConnectionProperties(sdeDirectConnectionProvider);
				return properties;
			}

			throw new NotSupportedException(
				$"Connection provider type not supported: {connectionProvider.GetType().Name}");
		}

		[NotNull]
		private static DatabaseConnectionProperties GetDatabaseConnectionProperties(
			[NotNull] SdeDirectConnectionProvider dcConnectionProvider)
		{
			if (dcConnectionProvider is null)
				throw new ArgumentNullException(nameof(dcConnectionProvider));

			EnterpriseDatabaseType enterpriseDatabaseType =
				ToEnterpriseDatabaseType(dcConnectionProvider.DatabaseType);

			var result = new DatabaseConnectionProperties(enterpriseDatabaseType);

			AuthenticationMode authMode;
			string userName;
			string password;

			if (dcConnectionProvider is SdeDirectDbUserConnectionProvider
			    sdeDirectDbUserConnectionProvider)
			{
				authMode = AuthenticationMode.DBMS;
				userName = sdeDirectDbUserConnectionProvider.UserName;
				password = sdeDirectDbUserConnectionProvider.PlainTextPassword;
			}
			else
			{
				authMode = AuthenticationMode.OSA;
				userName = null;
				password = null;
			}

			result.AuthenticationMode = authMode;
			result.User = userName;
			result.Password = password;

			result.Instance = dcConnectionProvider.DatabaseName;
			result.Database = dcConnectionProvider.RepositoryName;
			result.Version = dcConnectionProvider.VersionName;

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
