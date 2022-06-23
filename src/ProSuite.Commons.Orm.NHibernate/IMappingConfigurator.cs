using NHibernate.Cfg;

namespace ProSuite.Commons.Orm.NHibernate
{
	public interface IMappingConfigurator
	{
		void ConfigureMapping(Configuration nhConfiguration);
	}
}
