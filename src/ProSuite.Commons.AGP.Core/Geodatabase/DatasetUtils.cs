using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AGP.Core.Geodatabase
{
	public static class DatasetUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		public static string GetTableDisplayName([NotNull] Table table)
		{
			TableDefinition definition = table.GetDefinition();
			string name = definition.GetName();
			string alias = GetAliasName(definition);

			if (string.Equals(name, alias, StringComparison.CurrentCultureIgnoreCase))
			{
				// the alias name is equal to the name (but may have different case)
				// un-qualify the alias name to preserve it's case.
				using (var datastore = table.GetDatastore())
				{
					var sqlSyntax = datastore.GetSQLSyntax();
					// TODO why using alias here and not name?
					if (sqlSyntax == null) return alias;
					var parts = sqlSyntax.ParseTableName(alias);
					return parts.Item3;
				}
			}

			return alias;
		}

		public static string GetName(Table table)
		{
			// TODO Why not just table.GetName()?
			if (table is null) return null;
			// TODO By documentation, Definition object must be disposed!
			return table.GetDefinition()?.GetName();
		}

		public static string GetAliasName([NotNull] Table table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			return GetAliasName(table.GetDefinition());
		}

		[NotNull]
		public static string GetAliasName([NotNull] TableDefinition definition)
		{
			Assert.ArgumentNotNull(definition, nameof(definition));

			try
			{
				string aliasName = definition.GetAliasName();

				return StringUtils.IsNotEmpty(aliasName)
					       ? aliasName
					       : definition.GetName();
			}
			catch (NotSupportedException e)
			{
				// Shapefiles throw a NotSupportedException
				_msg.Debug("Unable to get alias. Using name", e);
				return definition.GetName();
			}
		}

		[CanBeNull]
		public static SpatialReference GetSpatialReference([NotNull] Feature feature)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));

			return GetSpatialReference(feature.GetTable());
		}

		[CanBeNull]
		public static SpatialReference GetSpatialReference([NotNull] this FeatureClass featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			return featureClass.GetDefinition()?.GetSpatialReference();
		}

		public static GeometryType GetShapeType([NotNull] FeatureClass featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			return featureClass.GetDefinition().GetShapeType();
		}

		public static T GetDatasetDefinition<T>(Datastore datastore, string name)
			where T : Definition
		{
			if (datastore is ArcGIS.Core.Data.Geodatabase geodatabase)
			{
				return geodatabase.GetDefinition<T>(name);
			}

			if (datastore is FileSystemDatastore fsDatastore)
			{
				return fsDatastore.GetDefinition<T>(name);
			}

			throw new ArgumentOutOfRangeException(
				$"Unsupported datastore type: {datastore.GetConnectionString()}.");
		}

		public static T OpenDataset<T>(Datastore datastore, string datasetName) where T : Dataset
		{
			if (datastore is ArcGIS.Core.Data.Geodatabase geodatabase)
			{
				return geodatabase.OpenDataset<T>(datasetName);
			}

			if (datastore is FileSystemDatastore fsDatastore)
			{
				return fsDatastore.OpenDataset<T>(datasetName);
			}

			throw new ArgumentOutOfRangeException(
				$"Unsupported datastore: {datastore.GetConnectionString()}");
		}

		public static IEnumerable<Table> OpenTables(ArcGIS.Core.Data.Geodatabase geodatabase,
		                                            ICollection<string> tableNames)
		{
			foreach (string name in geodatabase.GetDefinitions<TableDefinition>()
			                                   .Select(definition => definition.GetName())
			                                   .Where(tableNames.Contains))
			{
				yield return geodatabase.OpenDataset<Table>(name);
			}

			foreach (string name in geodatabase.GetDefinitions<FeatureClassDefinition>()
			                                   .Select(definition => definition.GetName())
			                                   .Where(tableNames.Contains))
			{
				yield return geodatabase.OpenDataset<Table>(name);
			}
		}

		public static bool IsSameTable(Table fc1, Table fc2)
		{
			if (ReferenceEquals(fc1, fc2)) return true;
			if (Equals(fc1.Handle, fc2.Handle)) return true;

			var id1 = fc1.GetID();
			var id2 = fc2.GetID();
			if (id1 != id2) return false;
			if (id1 >= 0) return true;

			// table id is negative for tables not registered with the Geodatabase
			// compare table name and workspace -- for now, give up and assume not same

			return false;
		}

		public static IEnumerable<Table> Distinct(
			IEnumerable<Table> tables)
		{
			return tables.Distinct(new TableComparer());
		}
	}
}
