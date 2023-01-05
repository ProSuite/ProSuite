using NHibernate.Cfg;

namespace ProSuite.Commons.Orm.NHibernate
{
	public interface INHConfigurationBuilder
	{
		/// <summary>
		/// Builds the Configuration object from the specifed configuration
		/// </summary>
		Configuration GetConfiguration();

		/// <summary>
		/// Whether or not the currently configured database supports sequences that allow using
		/// nHibernate's native identifier generators.
		/// using the 
		/// </summary>
		bool DatabaseSupportsSequence { get; }

		/// <summary>
		/// The DDX environment name relevant for multi-DDX setups. It is used to uniquely identify
		/// a specific DDX and look up its connection details in servers that serve requests from
		/// various DDX environments.
		/// </summary>
		string DdxEnvironmentName { get; }

		bool IsSQLite { get; }
		bool IsPostgreSQL { get; }
		bool IsSqlServer { get; }
		bool IsOracle { get; }
	}
}
