using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Data.Exceptions;
using ArcGIS.Core.Data.PluginDatastore;
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

		[CanBeNull]
		public static string GetAliasName(Table table)
		{
			if (table != null && table.IsJoinedTable())
			{
				return StringUtils.Concatenate(GetDatabaseTables(table).Select(GetAliasName), "/");
			}

			using (var definition = table?.GetDefinition())
			{
				return GetAliasName(definition);
			}
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

		[CanBeNull]
		public static int? GetDefaultSubtypeCode(Table table)
		{
			if (table is null) return null;

			using (var definition = table.GetDefinition())
			{
				return GetDefaultSubtypeCode(definition);
			}
		}

		[CanBeNull]
		public static int? GetDefaultSubtypeCode(TableDefinition definition)
		{
			if (definition is null) return null;

			try
			{
				// GetDefaultSubtypeCode() returns -1 if no subtypes
				int defaultSubtypeCode = definition.GetDefaultSubtypeCode();

				if (defaultSubtypeCode < 0)
				{
					return null;
				}

				return defaultSubtypeCode;
			}
			catch (NotSupportedException)
			{
				// Shapefiles have no subtypes and throw NotSupportedException
				return null;
			}
		}

		[CanBeNull]
		public static Subtype GetDefaultSubtype(Table table)
		{
			if (table is null) return null;

			using (var definition = table.GetDefinition())
			{
				return GetDefaultSubtype(definition);
			}
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

			using (TableDefinition definition = table.GetDefinition())
			{
				return GetSubtype(definition, subtypeCode);
			}
		}

		[CanBeNull]
		public static Subtype GetSubtype(TableDefinition definition, int? subtypeCode)
		{
			if (definition is null) return null;
			if (subtypeCode == null) return null;

			try
			{
				// GetSubtypes() returns an empty list if no subtypes
				var subtypes = definition.GetSubtypes();
				return subtypes.FirstOrDefault(st => st.GetCode() == subtypeCode);
			}
			catch (NotSupportedException)
			{
				// Shapefiles have no subtypes and throw NotSupportedException
				return null;
			}
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

		public static T GetDatasetDefinition<T>(Datastore datastore, string datasetName)
			where T : Definition
		{
			if (datastore is ArcGIS.Core.Data.Geodatabase geodatabase)
			{
				return geodatabase.GetDefinition<T>(datasetName);
			}

			if (datastore is FileSystemDatastore fsDatastore)
			{
				return fsDatastore.GetDefinition<T>(datasetName);
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

				if (datastore is PluginDatastore pluginDatastore)
				{
					Table table = pluginDatastore.OpenTable(datasetName);
					return Assert.NotNull(table as T);
				}
			}
			catch (GeodatabaseTableException ex)
			{
				// dataset does not exist
				string displayText = WorkspaceUtils.GetDatastoreDisplayText(datastore);
				_msg.Debug($"Failed to open {datasetName} from {displayText}: {ex.Message}", ex);
				throw;
			}
			catch (AssertionException ex)
			{
				string displayText = WorkspaceUtils.GetDatastoreDisplayText(datastore);
				_msg.Debug(
					$"Failed to open {datasetName} from {displayText}: Invalid Dataset type for PluginDatastore: {nameof(T)}",
					ex);
				throw;
			}

			throw new ArgumentOutOfRangeException(
				$"Unsupported datastore type: {datastore.GetConnectionString()}");
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

		public static FeatureClass OpenOrCreateFeatureClass(
			[NotNull] ArcGIS.Core.Data.Geodatabase geodatabase,
			[NotNull] string datasetName,
			[NotNull] List<FieldDescription> fieldDescription,
			[NotNull] GeometryType geometryType,
			[NotNull] ArcGIS.Core.Geometry.SpatialReference spatialReference)
		{
			TryOpenDataset<FeatureClass>(geodatabase, datasetName, out var fc);
			if (fc != null)
			{
				if (!ValidateSchema(fc, geometryType, fieldDescription, spatialReference))
				{
					throw new ArgumentException(
						$"Feature class {datasetName} already exists but has an incompatible schema.");
				}
				return fc;
			}

			return CreateFeatureClass(geodatabase, datasetName, fieldDescription, geometryType,
			                          spatialReference);
		}

		private static bool ValidateSchema(FeatureClass featureClass,
		                                  GeometryType expectedGeometryType,
		                                  List<FieldDescription> expectedFields,
		                                  ArcGIS.Core.Geometry.SpatialReference spatialReference)
		{
			using (FeatureClassDefinition definition = featureClass.GetDefinition())
			{
				if (definition.GetShapeType() != expectedGeometryType)
				{
					return false;
				}
				if (!definition.GetSpatialReference().IsEqual(spatialReference))
				{
					return false;
				}

				var existingFields = definition.GetFields();

				foreach (var expectedField in expectedFields)
				{
					var existingField = existingFields.FirstOrDefault(f =>
						f.Name.Equals(expectedField.Name, StringComparison.OrdinalIgnoreCase));

					if (existingField == null || existingField.FieldType != expectedField.FieldType)
					{
						return false;
					}
				}
				return true;
			}
		}

		public static FeatureClass CreateFeatureClass(
			[NotNull] ArcGIS.Core.Data.Geodatabase geodatabase,
			[NotNull] string datasetName,
			[NotNull] List<FieldDescription> fieldDescription,
			[NotNull] GeometryType geometryType,
			[NotNull] ArcGIS.Core.Geometry.SpatialReference spatialReference)
		{
			var shapeFieldDescription = new ShapeDescription(geometryType, spatialReference);
			var featureClassDescription = new FeatureClassDescription(datasetName,
				fieldDescription,
				shapeFieldDescription);

			SchemaBuilder schemaBuilder = new SchemaBuilder(geodatabase);
			schemaBuilder.Create(featureClassDescription);
			bool success = schemaBuilder.Build();

			if (!success)
			{
				throw new Exception($"Failed to create feature class {datasetName}");
			}

			// Open and return the newly created feature class
			return geodatabase.OpenDataset<FeatureClass>(datasetName);
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

		[Obsolete($"use {nameof(RelationshipClassUtils)}")]
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

		[Obsolete($"use {nameof(RelationshipClassUtils)}")]
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

		[Obsolete($"use {nameof(RelationshipClassUtils)}")]
		public static RelationshipClass OpenRelationshipClass(
			[NotNull] ArcGIS.Core.Data.Geodatabase geodatabase,
			[NotNull] string relClassName)
		{
			return OpenDataset<RelationshipClass>(geodatabase, relClassName);
		}

		public static bool IsSameTable(Table fc1, Table fc2)
		{
			if (ReferenceEquals(fc1, fc2)) return true;

			if (fc1 == null || fc2 == null) return false;

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
			TableDefinition definition = tableWithJoin.GetDefinition();

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
			string shapeField = featureClassWithJoin.GetDefinition().GetShapeField();

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

			using (var definition = featureClass.GetDefinition())
			{
				return GetLengthFieldName(definition);
			}
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
			using (var definition = table.GetDefinition())
			{
				return GetSubtypeFieldName(definition);
			}
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

		/// <summary>
		/// Returns the index of the field in <paramref name="fields"/> matching the 
		/// <paramref name="field"/> or -1 if no match was found.
		/// </summary>
		/// <param name="field">The field to match</param>
		/// <param name="fields">The fields to search</param>
		/// <param name="requireMatchingDomainNames"> Whether the name of the domain must match too.
		/// Only the main domain of the field will be compared. Domains varying by subtype are ignored.</param>
		/// <returns></returns>
		public static int FindMatchingFieldIndex([NotNull] Field field,
		                                         [NotNull] IReadOnlyList<Field> fields,
		                                         bool requireMatchingDomainNames = true)
		{
			return FindMatchingFieldIndex(field, fields, string.Empty, requireMatchingDomainNames);
		}

		/// <summary>
		/// Returns the index of the field in <paramref name="fields"/> matching the 
		/// <paramref name="field"/> or -1 if no match was found.
		/// </summary>
		/// <param name="field">The field whose properties must match.</param>
		/// <param name="fields">The fields in which the field is searched.</param>
		/// <param name="tableName">Only match fields in the fields list, that start with the
		/// specified table name.</param>
		/// <param name="requireMatchingDomainNames"> Whether the name of the domain must match too.
		/// Only the main domain of the field will be compared. Domains varying by subtype are
		/// ignored.</param>
		/// <returns></returns>
		public static int FindMatchingFieldIndex([NotNull] Field field,
		                                         [NotNull] IReadOnlyList<Field> fields,
		                                         [CanBeNull] string tableName,
		                                         bool requireMatchingDomainNames = true)
		{
			Assert.ArgumentNotNull(field, nameof(field));
			Assert.ArgumentNotNull(fields, nameof(fields));

			FieldType fieldType = field.FieldType;

			string targetFieldName;
			if (string.IsNullOrEmpty(tableName))
			{
				targetFieldName = field.Name;
			}
			else
			{
				targetFieldName = tableName + "." + field.Name;
			}

			int fieldCount = fields.Count;
			for (var index = 0; index < fieldCount; index++)
			{
				Field sourceField = fields[index];

				if (! targetFieldName.Equals(sourceField.Name,
				                             StringComparison.OrdinalIgnoreCase))
				{
					// names don't match -> no match
					continue;
				}

				if (fieldType != sourceField.FieldType)
				{
					if (string.IsNullOrEmpty(tableName))
					{
						// no join, types don't match --> no match
						continue;
					}

					if (sourceField.FieldType == FieldType.OID &&
					    fieldType == FieldType.Integer)
					{
						// consider as match
					}
					else
					{
						continue;
					}
				}

				if (! requireMatchingDomainNames)
				{
					return index;
				}

				Domain fieldDomain = field.GetDomain();
				Domain sourceFieldDomain = sourceField.GetDomain();

				if (fieldDomain == null && sourceFieldDomain == null)
				{
					return index;
				}

				if (fieldDomain != null && sourceFieldDomain != null &&
				    fieldDomain.GetName().Equals(sourceFieldDomain.GetName(),
				                                 StringComparison.OrdinalIgnoreCase))
				{
					return index;
				}
			}

			return -1;
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
