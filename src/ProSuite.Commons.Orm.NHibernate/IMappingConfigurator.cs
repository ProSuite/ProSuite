using NHibernate.Cfg;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Orm.NHibernate
{
	public interface IMappingConfigurator
	{
		void ConfigureMapping([NotNull] Configuration nhConfiguration);
	}
}
