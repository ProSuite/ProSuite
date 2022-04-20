using System.Collections.Generic;
using System.Reflection;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Mapping.ByCode;

namespace ProSuite.Commons.Orm.NHibernate
{
	/// <summary>
	/// Mapping configurator that adds NHibernate mapping-by-code classes
	/// from the specified assemblies.
	/// </summary>
	public class MappingByCodeConfigurator : IMappingConfigurator
	{
		private readonly IEnumerable<Assembly> _assemblies;

		public MappingByCodeConfigurator(IEnumerable<Assembly> assemblies)
		{
			_assemblies = assemblies;
		}

		public void ConfigureMapping(Configuration nhConfiguration)
		{
			ModelMapper mapper = new ModelMapper();

			foreach (Assembly assembly in _assemblies)
			{
				mapper.AddMappings(assembly.GetExportedTypes());
			}

			HbmMapping mapping = mapper.CompileMappingForAllExplicitlyAddedEntities();

			nhConfiguration.AddMapping(mapping);
		}
	}
}
