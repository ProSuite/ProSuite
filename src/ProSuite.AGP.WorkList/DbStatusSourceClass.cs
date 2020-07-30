using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.DataModel;

namespace ProSuite.AGP.WorkList
{
	public class DbStatusSourceClass
	{
		public DbStatusSourceClass(IObjectDataset dataset, DbStatusSchema statusSchema) : this(
			dataset.Name, statusSchema)
		{
			Dataset = dataset;
			StatusSchema = statusSchema;
		}

		public DbStatusSourceClass(string name, DbStatusSchema statusSchema)
		{
			Name = name;
			StatusSchema = statusSchema;
		}

		public string Name { get; set; }
		public IObjectDataset Dataset { get; }
		public DbStatusSchema StatusSchema { get; }

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
	}
}
