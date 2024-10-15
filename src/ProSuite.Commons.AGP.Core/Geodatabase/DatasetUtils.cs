using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.Exceptions;
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
			using var definition = table.GetDefinition();
			string name = definition.GetName();
			string alias = GetAliasName(definition);

			if (string.IsNullOrEmpty(alias))
			{
				alias = name;
			}

			if (string.Equals(name, alias, StringComparison.CurrentCultureIgnoreCase))
			{
				// Alias name equals table name (but may have different case);
				// un-qualify the alias name to preserve its case:
				using var datastore = table.GetDatastore();
				var sqlSyntax = datastore.GetSQLSyntax();
				if (sqlSyntax is null) return alias;
				var parts = sqlSyntax.ParseTableName(alias);
				return parts.Item3; // table name
			}

			return alias;
		}

		public static string GetName(Table table)
		{
			if (table != null && table.IsJoinedTable())
			{
				return StringUtils.Concatenate(GetDatabaseTables(table).Select(GetName), "/");
			}

			return table?.GetName();
		}

		public static string GetAliasName(Table table)
		{
			if (table != null && table.IsJoinedTable())
			{
				return StringUtils.Concatenate(GetDatabaseTables(table).Select(GetAliasName), "/");
			}

			using var definition = table?.GetDefinition();
			return GetAliasName(definition);
		}

		[CanBeNull]
		private static string GetAliasName(TableDefinition definition)
		{
			if (definition is null) return null;

			try
			{
				// GetAliasName() returns an empty string, if the alias is not set
				string alias = definition.GetAliasName();

				if (string.IsNullOrEmpty(alias)) alias = definition.GetName();
				return alias;
			}
			catch (NotSupportedException notSupportedException)
			{
				// Shapefiles throw a NotSupportedException
				_msg.Debug("Subtypes not supported", notSupportedException);

				return null;
			}
		}

		public static int GetDefaultSubtypeCode(Table table)
		{
			if (table is null) return -1;

			using var definition = table.GetDefinition();
			return GetDefaultSubtypeCode(definition);
		}

		public static int GetDefaultSubtypeCode(TableDefinition definition)
		{
			if (definition is null) return -1;

			try
			{
				// GetDefaultSubtypeCode() returns -1 if no subtypes
				return definition.GetDefaultSubtypeCode();
			}
			catch (NotSupportedException)
			{
				// Shapefiles have no subtypes and throw NotSupportedException
				return -1;
			}
		}

		[CanBeNull]
		public static Subtype GetDefaultSubtype(Table table)
		{
			if (table is null) return null;

			using var definition = table.GetDefinition();
			return GetDefaultSubtype(definition);
		}

		[CanBeNull]
		public static Subtype GetDefaultSubtype(TableDefinition definition)
		{
			var defaultCode = GetDefaultSubtypeCode(definition);

			return GetSubtype(definition, defaultCode);
		}

		[CanBeNull]
		public static Subtype GetSubtype(Table table, int subtypeCode)
		{
			if (table is null) return null;

			using var definition = table.GetDefinition();
			return GetSubtype(definition, subtypeCode);
		}

		[CanBeNull]
		public static Subtype GetSubtype(TableDefinition definition, int subtypeCode)
		{
			if (definition is null) return null;

			try
			{
				// GetSubtypes() returns an empty list if no subtypes
				foreach (Subtype subtype in definition.GetSubtypes())
				{
					if (subtype.GetCode() == subtypeCode)
					{
						return subtype;
					}

					subtype.Dispose();
				}
			}
			catch (NotSupportedException)
			{
				// Shapefiles have no subtypes and throw NotSupportedException
				return null;
			}

			return null;
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
		public static SpatialReference GetSpatialReference(Feature feature)
		{
			using var featureClass = feature?.GetTable();
			return GetSpatialReference(featureClass);
		}

		[CanBeNull]
		public static SpatialReference GetSpatialReference(this FeatureClass featureClass)
		{
			using var definition = featureClass?.GetDefinition();
			return definition?.GetSpatialReference();
		}

		public static GeometryType GetShapeType(this FeatureClass featureClass)
		{
			using var definition = featureClass?.GetDefinition();
			return definition?.GetShapeType() ?? GeometryType.Unknown;
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

		/// <exception cref="ArgumentOutOfRangeException">Datastore is not Geodatabase nor
		/// FileSystemDatastore (Shapefile)</exception>
		/// <exception cref="GeodatabaseTableException">Table was not found</exception>
		public static T OpenDataset<T>([NotNull] Datastore datastore, [NotNull] string datasetName)
			where T : Dataset
		{
			try
			{
				if (datastore is ArcGIS.Core.Data.Geodatabase geodatabase)
				{
					return geodatabase.OpenDataset<T>(datasetName);
				}

				if (datastore is FileSystemDatastore fsDatastore)
				{
					return fsDatastore.OpenDataset<T>(datasetName);
				}
			}
			catch (GeodatabaseTableException ex)
			{
				// dataset does not exist
				_msg.Error(ex.Message, ex);
				throw;
			}

			throw new ArgumentOutOfRangeException(
				$"Unsupported datastore: {datastore.GetConnectionString()}");
		}

		public static bool TryOpenDataset<T>([NotNull] Datastore datastore,
		                                     [NotNull] string datasetName,
		                                     [CanBeNull] out T dataset)
			where T : Dataset
		{
			dataset = null;

			try
			{
				dataset = OpenDataset<T>(datastore, datasetName);
				return true;
			}
			catch (Exception ex)
			{
				_msg.Debug(ex.Message, ex);
			}

			return false;
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

		public static IEnumerable<RelationshipClassDefinition> GetRelationshipClassDefinitions(
			[NotNull] ArcGIS.Core.Data.Geodatabase geodatabase,
			[CanBeNull] Predicate<RelationshipClassDefinition> predicate = null)
		{
			foreach (RelationshipClassDefinition definition in geodatabase
				         .GetDefinitions<RelationshipClassDefinition>())
			{
				if (predicate is null || predicate(definition))
				{
					yield return definition;
				}
			}

			foreach (AttributedRelationshipClassDefinition definition in
			         geodatabase.GetDefinitions<AttributedRelationshipClassDefinition>())
			{
				if (predicate is null || predicate(definition))
				{
					yield return definition;
				}
			}
		}

		public static IEnumerable<RelationshipClass> GetRelationshipClasses(
			[NotNull] ArcGIS.Core.Data.Geodatabase geodatabase,
			[CanBeNull] Predicate<RelationshipClassDefinition> predicate = null)
		{
			foreach (RelationshipClassDefinition definition in
			         GetRelationshipClassDefinitions(geodatabase, predicate))
			{
				yield return geodatabase.OpenDataset<RelationshipClass>(
					definition.GetName());
				
				definition.Dispose();
			}
		}

		public static RelationshipClass OpenRelationshipClass(
			[NotNull] ArcGIS.Core.Data.Geodatabase geodatabase,
			[NotNull] string relClassName)
		{
			return OpenDataset<RelationshipClass>(geodatabase, relClassName);
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
		/// Returns the actual table as it exists in the geodatabase, given a joined table.
		/// </summary>
		/// <param name="tableWithJoin"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public static Table GetDatabaseTable(Table tableWithJoin)
		{
			if (! tableWithJoin.IsJoinedTable())
			{
				return tableWithJoin;
			}

			if (tableWithJoin is FeatureClass featureClass)
			{
				return GetDatabaseFeatureClass(featureClass);
			}

			// Extract the shape's table name from the (fully qualified) shape field name:
			using TableDefinition definition = tableWithJoin.GetDefinition();

			if (! definition.HasObjectID())
			{
				throw new NotImplementedException(
					"Unable to determine the main table without OBJECTID");
			}

			string oidField = definition.GetObjectIDField();

			return GetGdbTableContainingField(tableWithJoin, oidField);
		}

		/// <summary>
		/// Returns the actual feature class as it exists in the geodatabase, given a joined
		/// feature class.
		/// </summary>
		/// <param name="featureClassWithJoin"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public static FeatureClass GetDatabaseFeatureClass(
			[NotNull] FeatureClass featureClassWithJoin)
		{
			if (! featureClassWithJoin.IsJoinedTable())
			{
				return featureClassWithJoin;
			}

			// Extract the shape's table name from the (fully qualified) shape field name:
			using FeatureClassDefinition definition = featureClassWithJoin.GetDefinition();
			string shapeField = definition.GetShapeField();

			return GetGdbTableContainingField(featureClassWithJoin, shapeField);
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

			using Join join = table.GetJoin();

			using Table originTable = join.GetOriginTable();

			foreach (Table sourceTable in GetDatabaseTables(originTable))
			{
				yield return sourceTable;
			}

			using Table destinationTable = join.GetDestinationTable();

			foreach (Table sourceTable in GetDatabaseTables(destinationTable))
			{
				yield return sourceTable;
			}
		}

		public static bool HasM(FeatureClass featureClass)
		{
			using var definition = featureClass.GetDefinition();
			return definition.HasM();
		}

		public static bool HasZ(FeatureClass featureClass)
		{
			using var definition = featureClass.GetDefinition();
			return definition.HasZ();
		}

		[CanBeNull]
		public static string GetAreaFieldName(FeatureClass featureClass)
		{
			if (featureClass is null) return null;

			using var definition = featureClass.GetDefinition();
			return GetAreaFieldName(definition);
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
		public static string GetLengthFieldName(FeatureClass featureClass)
		{
			if (featureClass is null) return null;

			using var definition = featureClass.GetDefinition();
			return GetLengthFieldName(definition);
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
				// TODO: Verify this, especially the type of exception
				// property is not implemented for feature classes from non-Gdb workspaces 
				// ("query layers")
				return null;
			}
		}

		/// <summary>
		/// Gets the name of the subtype field in a given table.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <returns>The name of the subtype field, or null
		/// if the table has no subtype field.</returns>
		[CanBeNull]
		public static string GetSubtypeFieldName([NotNull] Table table)
		{
			using var definition = table.GetDefinition();
			return GetSubtypeFieldName(definition);
		}

		[CanBeNull]
		public static string GetSubtypeFieldName(TableDefinition definition)
		{
			try
			{
				// GetSubtypeField() returns an empty string if no subtypes
				string fieldName = definition.GetSubtypeField();

				return string.IsNullOrEmpty(fieldName)
					       ? null
					       : fieldName;
			}
			catch (NotSupportedException notSupportedException)
			{
				// Shapefiles throw a NotSupportedException
				_msg.Debug("Subtypes not supported", notSupportedException);

				return null;
			}
		}

		/// <summary>
		/// Gets the index of the subtype field in a given table.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <returns>The index of the subtype field, or -1 
		/// if the table has no subtype field.</returns>
		public static int GetSubtypeFieldIndex([NotNull] Table table)
		{
			using var definition = table.GetDefinition();
			return GetSubtypeFieldIndex(definition);
		}

		public static int GetSubtypeFieldIndex(TableDefinition definition)
		{
			string subtypeFieldName = GetSubtypeFieldName(definition);

			return string.IsNullOrEmpty(subtypeFieldName)
				       ? -1
				       : definition.FindField(subtypeFieldName);
		}

		/// <summary>
		/// Gets the name of the object id field in a given table.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <returns>The name of the objectId field, or null
		/// if the table has no objectId field.</returns>
		[CanBeNull]
		public static string GetObjectIdFieldName([NotNull] Table table)
		{
			using var definition = table.GetDefinition();
			return GetObjectIdFieldName(definition);
		}

		[CanBeNull]
		public static string GetObjectIdFieldName(TableDefinition definition)
		{
			if (! definition.HasObjectID())
			{
				return null;
			}

			return definition.GetObjectIDField();
		}

		/// <summary>
		/// Deletes rows in a table based on a collection of object IDs.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <param name="oids">The oids.</param>
		public static void DeleteRows([NotNull] Table table,
		                              [NotNull] IEnumerable oids)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(oids, nameof(oids));

			const int maxLength = 1000;

			var sb = new StringBuilder();

			foreach (object oidObj in oids)
			{
				// Convert the (potentially boxed int) object:
				long oid = Convert.ToInt64(oidObj);

				if (sb.Length == 0)
				{
					sb.Append(oid);
				}
				else if (sb.Length < maxLength)
				{
					sb.AppendFormat(",{0}", oid);
				}
				else
				{
					// maximum exceeded, delete current oid list
					DeleteRowsByOIDString(table, sb.ToString());

					// clear string builder
					sb.Remove(0, sb.Length);
				}
			}

			if (sb.Length > 0)
			{
				DeleteRowsByOIDString(table, sb.ToString());
			}
		}

		private static T GetGdbTableContainingField<T>([NotNull] T joinedTable,
		                                               [NotNull] string qualifiedField)
			where T : Table
		{
			List<string> tokens = qualifiedField.Split('.').ToList();

			if (tokens.Count < 2)
			{
				throw new InvalidOperationException(
					$"The field name {qualifiedField} is not fully qualified for joined table {joinedTable.GetName()}.");
			}

			tokens.RemoveAt(tokens.Count - 1);

			string tableName = StringUtils.Concatenate(tokens, ".");

			foreach (Table databaseTable in GetDatabaseTables(joinedTable))
			{
				if (databaseTable is T dbClassTyped &&
				    dbClassTyped.GetName()
				                .Equals(tableName, StringComparison.InvariantCultureIgnoreCase))
				{
					return dbClassTyped;
				}

				databaseTable.Dispose();
			}

			throw new InvalidOperationException(
				$"No database table found for joined table " +
				$"{joinedTable.GetName()} and field name {qualifiedField}");
		}

		/// <summary>
		/// Deletes rows in a table based on a string containing a comma-separated
		/// list of object ids.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <param name="oidString">The oid string.</param>
		private static void DeleteRowsByOIDString([NotNull] Table table,
		                                          [NotNull] string oidString)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(oidString, nameof(oidString));

			string objectIdFieldName = Assert.NotNullOrEmpty(GetObjectIdFieldName(table));

			string whereClause = $"{objectIdFieldName} IN ({oidString})";

			QueryFilter filter = new QueryFilter { WhereClause = whereClause };

			Stopwatch watch = _msg.DebugStartTiming(
				"Deleting rows from {0} using where clause {1}",
				GetName(table), whereClause);

			table.DeleteRows(filter);

			_msg.DebugStopTiming(watch, "Rows deleted");
		}
	}
}
