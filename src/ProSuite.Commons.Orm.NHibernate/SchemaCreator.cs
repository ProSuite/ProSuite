using System;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using ProSuite.Commons.Essentials.CodeAnnotations;
using Environment = System.Environment;

namespace ProSuite.Commons.Orm.NHibernate
{
	[UsedImplicitly]
	public class SchemaCreator
	{
		[NotNull] private readonly Configuration _configuration;

		[CLSCompliant(false)]
		public SchemaCreator([NotNull] INHConfigurationBuilder configurationBuilder)
		{
			_configuration = configurationBuilder.GetConfiguration();
		}

		public void CreateSchema([CanBeNull] string outputFile = null,
		                         bool writeToConsole = false)
		{
			var schemaExport = new SchemaExport(_configuration);

			if (! string.IsNullOrEmpty(outputFile))
			{
				schemaExport.SetOutputFile(outputFile);
			}

			schemaExport.SetDelimiter(Environment.NewLine);

			const bool execute = true;
			schemaExport.Create(writeToConsole, execute);
		}

		/// <summary>
		/// Creates the script that can be used to create the schema.
		/// </summary>
		/// <param name="outputFile"></param>
		/// <param name="writeToConsole"></param>
		public void CreateSchemaScript([NotNull] string outputFile,
		                               bool writeToConsole = false)
		{
			var schemaExport = new SchemaExport(_configuration);

			schemaExport.SetOutputFile(outputFile);
			schemaExport.SetDelimiter(Environment.NewLine);

			schemaExport.Create(writeToConsole, false);
		}
	}
}
