using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Mapping.ByCode;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Orm.NHibernate
{
	/// <summary>
	/// Mapping configurator that adds NHibernate mapping-by-code classes from the specified
	/// assemblies. Optionally, embedded XML files will be added as well.
	/// </summary>
	public class MappingByCodeConfigurator : IMappingConfigurator
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IEnumerable<Assembly> _assemblies;

		public MappingByCodeConfigurator(IEnumerable<Assembly> assemblies)
		{
			_assemblies = assemblies;
		}

		/// <summary>
		/// To allow mixed mapping by code and by XML files, set this property to true.
		/// </summary>
		public bool IncludeXmlFiles { get; set; }

		public void ConfigureMapping(Configuration nhConfiguration)
		{
			if (IncludeXmlFiles)
			{
				AddXmlMappingFiles(nhConfiguration, _assemblies.First());
			}

			ModelMapper mapper = new ModelMapper();

			foreach (Assembly assembly in _assemblies)
			{
				mapper.AddMappings(assembly.GetExportedTypes());
			}

			HbmMapping mapping = mapper.CompileMappingForAllExplicitlyAddedEntities();

			nhConfiguration.AddMapping(mapping);

#if DEBUG
			//This will write all the XML into the bin/mappings folder
			mapper.CompileMappingForEachExplicitlyAddedEntity().WriteAllXmlMapping();
#endif
		}

		private static void AddXmlMappingFiles(Configuration nhConfiguration,
		                                       Assembly assembly)
		{
			string assemblyName = assembly.FullName;

			// Consider assemblies with mappings to be optional - if the assembly 
			// cannot be loaded, it will presumably not execute any code either.

			try
			{
				nhConfiguration.AddAssembly(assemblyName);
			}
			catch (Exception e)
			{
				_msg.Debug("Error in NH mapping-by-code", e);
				throw;
			}
		}
	}
}
