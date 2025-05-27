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
	/// Mapping configurator that adds NHibernate mapping-by-code classes from
	/// the specified assemblies. Optionally, embedded XML files will be added
	/// as well. Subclass may adapt the created (or loaded) mappings.
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

			// Just an idea: using event handlers as sketched below (there are
			// many more), we could create much of our mapping by convention
			//mapper.BeforeMapClass += Mapper_BeforeMapClass;
			//mapper.AfterMapClass += Mapper_AfterMapClass;

			foreach (Assembly assembly in _assemblies)
			{
				mapper.AddMappings(assembly.GetExportedTypes());
			}

			HbmMapping mapping = mapper.CompileMappingForAllExplicitlyAddedEntities();

			AdaptMapping(mapping);

			nhConfiguration.AddMapping(mapping);

#if DEBUG
			// This will write all the XML into the bin/mappings folder
			var mappings = mapper.CompileMappingForEachExplicitlyAddedEntity();
			AdaptMappings(mappings).WriteAllXmlMapping();
#endif
		}

		/// <summary>
		/// Override in subclass to modify the compiled NHibernate mapping.
		/// </summary>
		/// <param name="mapping">The compiled mapping-by-code</param>
		/// <remarks>Reasoning: The mapping by code API is essentially
		/// write-only, and the mapping classes are instantiated through
		/// Activator.CreateInstance(), so there is no place where we can
		/// change the mapping based on outside information (without ugly
		/// access to global variables). Therefore, use this method to
		/// inspect and modify the compiled mapping.</remarks>
		protected virtual void AdaptMapping(HbmMapping mapping)
		{
			// subclass may override to adapt the mapping(s)
		}

		private IEnumerable<HbmMapping> AdaptMappings(IEnumerable<HbmMapping> mappings)
		{
			if (mappings is null) yield break;

			foreach (var mapping in mappings)
			{
				AdaptMapping(mapping);
				yield return mapping;
			}
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
