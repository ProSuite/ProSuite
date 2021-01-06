using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NHibernate.Cfg;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Orm.NHibernate
{
	[UsedImplicitly]
	public class MappingConfigurator : IMappingConfigurator
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly string _assemblyWithEmbeddedFiles;
		private readonly List<string> _embeddedFiles;
		private readonly List<string> _assemblyNames;

		/// <summary>
		/// Initializes a new instance of the <see cref="MappingConfigurator"/> class.
		/// </summary>
		/// <param name="assemblyNames">The list of (optional) assemblies that contain
		/// NHibernate mappings.</param>
		public MappingConfigurator(List<string> assemblyNames)
		{
			_assemblyNames = assemblyNames;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MappingConfigurator"/> class.
		/// </summary>
		/// <param name="assembly">The (non-optional) assembly containing the mappings.</param>
		/// <param name="embeddedFiles">The embedded mapping files.</param>
		public MappingConfigurator(string assembly,
		                           List<string> embeddedFiles)
		{
			_assemblyWithEmbeddedFiles = assembly;
			_embeddedFiles = embeddedFiles;
		}

		public void ConfigureMapping(Configuration nhConfiguration)
		{
			if (_assemblyNames != null)
			{
				foreach (string assembly in _assemblyNames)
				{
					// Consider assemblies with mappings to be optional - if the assembly 
					// cannot be loaded, it will presumably not execute any code either.

					if (! AssemblyExists(assembly))
					{
						_msg.DebugFormat(
							"Assembly {0} cannot be found and therefore its NHibernate mappings will not be added.",
							assembly);

						continue;
					}

					_msg.DebugFormat("Trying to add assembly {0}", assembly);

					try
					{
						nhConfiguration.AddAssembly(assembly);
					}
					catch (Exception e)
					{
						_msg.Debug($"Error loading assembly {assembly}", e);
						throw;
					}
				}
			}

			if (! string.IsNullOrEmpty(_assemblyWithEmbeddedFiles))
			{
				Assembly assembly;
				try
				{
					// If a (single) assembly with embedded files is configured, consider
					// it mandatory.
					assembly = Assembly.Load(_assemblyWithEmbeddedFiles);
				}
				catch (Exception e)
				{
					_msg.Debug($"Error loading assembly {_assemblyWithEmbeddedFiles}", e);
					throw;
				}

				foreach (string embeddedFile in _embeddedFiles)
				{
					nhConfiguration.AddInputStream(
						assembly.GetManifestResourceStream(embeddedFile));
				}
			}
		}

		private static bool AssemblyExists(string assembly)
		{
			var currentAssemblyDir =
				Assert.NotNullOrEmpty(
					Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

			string assemblySearchPattern = $"{assembly}.*";

			return Directory.EnumerateFiles(currentAssemblyDir, assemblySearchPattern).Any();
		}
	}
}
