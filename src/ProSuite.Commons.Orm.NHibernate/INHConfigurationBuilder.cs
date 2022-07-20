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
	}
}
