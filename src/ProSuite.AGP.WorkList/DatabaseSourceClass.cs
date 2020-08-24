using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList
{
	public class DatabaseSourceClass : ISourceClass
	{
		private readonly GdbTableIdentity _identity;

		public DatabaseSourceClass(GdbTableIdentity identity,
		                           DatabaseStatusSchema statusSchema,
		                           IAttributeReader attributeReader)
		{
			_identity = identity;
			StatusSchema = statusSchema;
			AttributeReader = attributeReader;
		}

		public long Id => _identity.Id;

		public bool Uses(Table table)
		{
			return _identity.Equals(new GdbTableIdentity(table));
		}

		public string Name => _identity.Name;

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
