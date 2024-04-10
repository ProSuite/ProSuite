using System;
using System.Collections.Generic;
using System.Globalization;
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
		public static int? GetDefaultSubtypeCode([NotNull] Table table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			var classDefinition = table.GetDefinition();

			int? code = null;
			try
			{
				code = classDefinition.GetDefaultSubtypeCode();
			}
			catch (NotSupportedException notSupportedException)
			{
				// Shapefiles throw a NotSupportedException
				_msg.Debug("Subtypes not supported", notSupportedException);
			}

			return code;
		}

		[CanBeNull]
		public static Subtype GetDefaultSubtype([NotNull] Table table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			int? defaultSubtypeCode = GetDefaultSubtypeCode(table);

			if (! defaultSubtypeCode.HasValue)
			{
				return null;
			}

			Subtype subtype = null;
			try
			{
				subtype = table.GetDefinition().GetSubtypes()
				               .FirstOrDefault(st => st.GetCode() == defaultSubtypeCode.Value);
			}
			catch (NotSupportedException notSupportedException)
			{
				// Shapefiles throw a NotSupportedException
				_msg.Debug("Subtypes not supported", notSupportedException);
			}

			return subtype;
		}

		[NotNull]
		public static string ToString([NotNull] Subtype subtype)
		{
			string name;
			try
			{
				name = subtype.GetName();
			}
			catch (Exception e)
			{
				name = $"[error getting Name: {e.Message}]";
			}

			string code;
			try
			{
				code = subtype.GetCode().ToString(CultureInfo.InvariantCulture);
			}
			catch (Exception e)
			{
				code = $"[error getting Code: {e.Message}]";
			}

			return $"name={name} code={code}";
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

		/// <summary>
		/// Returns the actual database tables from a joined table or, if the table is not a joined
		/// table, the table itself.
		/// </summary>
		/// <param name="table">The potentially joined table</param>
		/// <returns></returns>
		public static IEnumerable<Table> GetDatabaseTables([NotNull] Table table)
		{
			if (! table.IsJoinedTable())
			{
				yield return table;
				yield break;
			}

			Join join = table.GetJoin();

			Table originTable = join.GetOriginTable();

			foreach (Table sourceTable in GetDatabaseTables(originTable))
			{
				yield return sourceTable;
			}

			Table destinationTable = join.GetDestinationTable();

			foreach (Table sourceTable in GetDatabaseTables(destinationTable))
			{
				yield return sourceTable;
			}
		}

		public static bool HasM(FeatureClass featureClass)
		{
			return featureClass.GetDefinition().HasM();
		}

		public static bool HasZ(FeatureClass featureClass)
		{
			return featureClass.GetDefinition().HasZ();
		}

		[CanBeNull]
		public static string GetAreaFieldName([NotNull] FeatureClass featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			return GetAreaFieldName(featureClass.GetDefinition());
		}

		[CanBeNull]
		public static string GetAreaFieldName(
			[NotNull] FeatureClassDefinition featureClassDefinition)
		{
			Assert.ArgumentNotNull(featureClassDefinition, nameof(featureClassDefinition));

			try
			{
				string areaFieldName = featureClassDefinition.GetAreaField();

				return areaFieldName;
			}
			catch (NotImplementedException)
			{
				// TODO: Verify this
				// property is not implemented for feature classes from non-Gdb workspaces 
				// ("query layers")
				return null;
			}
		}

		[CanBeNull]
		public static string GetLengthFieldName([NotNull] FeatureClass featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			return GetLengthFieldName(featureClass.GetDefinition());
		}

		public static string GetLengthFieldName(FeatureClassDefinition featureClassDefinition)
		{
			Assert.ArgumentNotNull(featureClassDefinition, nameof(featureClassDefinition));

			try
			{
				string lengthFieldName = featureClassDefinition.GetLengthField();

				return lengthFieldName;
			}
			catch (NotImplementedException)
			{
				// TODO: Verify this
				// property is not implemented for feature classes from non-Gdb workspaces 
				// ("query layers")
				return null;
			}
		}
	}
}
