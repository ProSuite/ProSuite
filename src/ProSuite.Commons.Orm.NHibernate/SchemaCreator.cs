using System;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using ProSuite.Commons.Essentials.CodeAnnotations;
using Environment = System.Environment;

namespace ProSuite.Commons.Orm.NHibernate
{
	[UsedImplicitly]
	public class SchemaCreator
	{
		[NotNull] [UsedImplicitly] private readonly ISessionFactory _sessionFactory;
		[NotNull] private readonly Configuration _configuration;

		[CLSCompliant(false)]
		public SchemaCreator([NotNull] INHConfigurationBuilder configurationBuilder)
		{
			_configuration = configurationBuilder.GetConfiguration();
			_sessionFactory = _configuration.BuildSessionFactory();
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

			const bool export = true;
			schemaExport.Create(writeToConsole, export);
		}
	}
}