using NHibernate.Cfg;

namespace ProSuite.Commons.Orm.NHibernate
{
	public interface INHConfigurationBuilder
	{
		/// <summary>
		/// Builds the Configuration object from the specifed configuration
		/// </summary>
		Configuration GetConfiguration();
	}
}
