using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList
{
	public class DatabaseSourceClass
	{
		public DatabaseSourceClass(string name, DatabaseStatusSchema statusSchema,
		                           IAttributeReader attributeReader)
		{
			Name = name;
			StatusSchema = statusSchema;
			AttributeReader = attributeReader;
		}

		public string Name { get; set; }
		public DatabaseStatusSchema StatusSchema { get; }
		public IAttributeReader AttributeReader { get; }

		[CanBeNull]
		public Table OpenTable([NotNull] Geodatabase geodatabase)
		{
			Assert.ArgumentNotNull(geodatabase, nameof(geodatabase));

			return geodatabase.OpenDataset<Table>(Name);
		}

		[CanBeNull]
		public FeatureClass OpenFeatureClass([NotNull] Geodatabase geodatabase)
		{
			Assert.ArgumentNotNull(geodatabase, nameof(geodatabase));

			return geodatabase.OpenDataset<FeatureClass>(Name);
		}

		public IEnumerable<PluginField> GetFields([NotNull] Geodatabase geodatabase)
		{
			Assert.ArgumentNotNull(geodatabase, nameof(geodatabase));

			using (var definition = geodatabase.GetDefinition<FeatureClassDefinition>(Name))
			{
				return definition.GetFields()
				                 .Select(field => new PluginField(
					                         field.Name, field.AliasName, field.FieldType));
			}
		}

		public FeatureClassDefinition GetDefinition([NotNull] Geodatabase geodatabase)
		{
			Assert.ArgumentNotNull(geodatabase, nameof(geodatabase));

			return geodatabase.GetDefinition<FeatureClassDefinition>(Name);
		}
	}
}
