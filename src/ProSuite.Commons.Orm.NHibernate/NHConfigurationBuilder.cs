using System;
using System.Collections.Generic;
using NHibernate.Cfg;
using NHibernate.Dialect;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using Environment = System.Environment;

namespace ProSuite.Commons.Orm.NHibernate
{
	/// <summary>
	/// Default imlementation of <see cref="INHConfigurationBuilder"/>
	/// </summary>
	[UsedImplicitly]
	public abstract class NHConfigurationBuilder : INHConfigurationBuilder
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IMappingConfigurator _mappingConfigurator;

		private global::NHibernate.Dialect.Dialect _dialect;

		protected NHConfigurationBuilder(
			string defaultSchema,
			string connectionString,
			string dialect,
			string showSql,
			string useSecondLevelCache,
			IMappingConfigurator mappingConfigurator,
			[CanBeNull] string ddxEnvironmentName = null)
		{
			DefaultSchema = defaultSchema;
			ConnectionString = connectionString;
			Dialect = dialect;
			ShowSql = showSql;

			const string envVarSecondLevelCache = "PROSUITE_NH_SECONDLEVEL_CACHE_PROVIDER";

			string alternateCacheProviderClass =
				Environment.GetEnvironmentVariable(envVarSecondLevelCache);

			if (! string.IsNullOrEmpty(alternateCacheProviderClass))
			{
				_msg.DebugFormat(
					"Alternate second level cache defined in environment variable {0}: {1}",
					envVarSecondLevelCache, alternateCacheProviderClass);

				if (string.Equals(alternateCacheProviderClass, "NONE",
				                  StringComparison.InvariantCultureIgnoreCase))
				{
					_msg.DebugFormat("Disabling second level cache...");
					useSecondLevelCache = "false";
				}
				else
				{
					CacheProviderClass = alternateCacheProviderClass;
				}
			}

			UseSecondLevelCache = useSecondLevelCache;

			DdxEnvironmentName = ddxEnvironmentName;

			_mappingConfigurator = mappingConfigurator;
		}

		protected string DefaultSchema { get; }
		protected string ConnectionString { get; }
		protected string Dialect { get; }
		protected string ShowSql { get; }
		protected string UseSecondLevelCache { get; }

		protected string CacheProviderClass { get; set; } =
			"NHibernate.Caches.SysCache.SysCacheProvider, NHibernate.Caches.SysCache";

		public bool DatabaseSupportsSequence =>
			// TODO: Also check if the sequence exists. 
			! IsSQLite;

		public bool IsSQLite => _dialect is SQLiteDialect;

		public bool IsPostgreSQL =>
			StringUtils.Contains(Dialect, "PostgreSQL",
			                     StringComparison.InvariantCultureIgnoreCase);

		public bool IsSqlServer =>
			StringUtils.Contains(Dialect, "MsSql",
			                     StringComparison.InvariantCultureIgnoreCase);

		public bool IsOracle =>
			StringUtils.Contains(Dialect, "Oracle",
			                     StringComparison.InvariantCultureIgnoreCase);

		public string DdxEnvironmentName { get; protected set; }

		public string DdxSchemaName => DefaultSchema;

		/// <summary>
		/// Builds the Configuration object from the specified configuration
		/// </summary>
		/// <returns></returns>
		public Configuration GetConfiguration()
		{
			Dictionary<string, string> props = GetDefaultNHibConfiguration();

			AddCustomConfigurationProperties(props);

			// Allow for schema-version specific mapping:
			DetermineDdxVersion(props);

			_dialect = global::NHibernate.Dialect.Dialect.GetDialect(props);

			Configuration cfg = new Configuration().SetProperties(props);

			_mappingConfigurator?.ConfigureMapping(cfg);

			cfg.SetInterceptor(new NHibernateInterceptor());

			AddCustomConfiguration(cfg);

			return cfg;
		}

		/// <summary>
		/// Allows for custom configuration properties, such as the connection.driver_class for
		/// oracle:
		/// - NHibernate.Driver.OracleDataClientDriver (ODP.Net, must be included with installer and
		///   requires correct oracle client installation (including gac-redirect if versions do not
		///   match)
		/// - NHibernate.Driver.OracleClientDriver (the long-deprecated .net client)
		/// - NHibernate.Driver.OracleManagedDataClientDriver (oracle managed driver, must be included
		///   with the installer but no oracle client installation is needed).
		/// </summary>
		/// <param name="props"></param>
		protected abstract void AddCustomConfigurationProperties(Dictionary<string, string> props);

		protected virtual void AddCustomConfiguration(Configuration cfg) { }

		/// <summary>
		/// Allows implementors to determine the DDX version and initialize nh-mapping infrastructure.
		/// </summary>
		/// <param name="props"></param>
		/// <returns></returns>
		protected virtual Version DetermineDdxVersion(Dictionary<string, string> props)
		{
			return null;
		}

		private Dictionary<string, string> GetDefaultNHibConfiguration()
		{
			Dictionary<string, string> props = new Dictionary<string, string>();

			props.Add("default_schema", DefaultSchema);
			props.Add("connection.connection_string", ConnectionString);

			props.Add("dialect", Dialect);

			props.Add("connection.isolation", "ReadCommitted");
			props.Add("connection.provider",
			          "ProSuite.Commons.Orm.NHibernate.EncryptedDriverConnectionProvider, ProSuite.Commons.Orm.NHibernate");
			props.Add("cache.use_second_level_cache", UseSecondLevelCache);
			props.Add("cache.provider_class", CacheProviderClass);
			props.Add("show_sql", ShowSql);

			return props;
		}
	}
}
