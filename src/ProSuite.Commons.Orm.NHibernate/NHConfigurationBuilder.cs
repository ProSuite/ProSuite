using System;
using System.Collections.Generic;
using NHibernate.Cfg;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Orm.NHibernate
{
	/// <summary>
	/// Default imlementation of <see cref="INHConfigurationBuilder"/>
	/// </summary>
	[UsedImplicitly]
	public abstract class NHConfigurationBuilder : INHConfigurationBuilder
	{
		private readonly IMappingConfigurator _mappingConfigurator;

		protected NHConfigurationBuilder(
			string defaultSchema,
			string connectionString,
			string dialect,
			string showSql,
			string useSecondLevelCache,
			IMappingConfigurator mappingConfigurator)
		{
			DefaultSchema = defaultSchema;
			ConnectionString = connectionString;
			Dialect = dialect;
			ShowSql = showSql;
			UseSecondLevelCache = useSecondLevelCache;

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
			// NOTE: Currently SQLite is just used for unit testing.
			// TODO: Also check if the sequence exists. 
			! Dialect.Equals("NHibernate.Dialect.SQLiteDialect",
			                 StringComparison.InvariantCultureIgnoreCase);

		/// <summary>
		/// Builds the Configuration object from the specified configuration
		/// </summary>
		/// <returns></returns>
		public Configuration GetConfiguration()
		{
			Dictionary<string, string> props = GetDefaultNHibConfiguration();

			AddCustomConfigurationProperties(props);

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
