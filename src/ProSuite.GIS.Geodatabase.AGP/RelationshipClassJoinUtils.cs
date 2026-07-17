using System;
using System.Collections.Generic;
using System.Text;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.GIS.Geodatabase.API;
using ITable = ProSuite.GIS.Geodatabase.API.ITable;
using JoinType = ProSuite.Commons.GeoDb.JoinType;
using QueryDef = ArcGIS.Core.Data.QueryDef;

namespace ProSuite.GIS.Geodatabase.AGP
{
	/// <summary>
	/// Builds the <see cref="QueryDef"/> used to materialize a relationship-class join into a
	/// geodatabase query table (see <see cref="ArcWorkspace.OpenQueryTable"/>). This is a port of
	/// the ArcObjects <c>TableJoinUtils.CreateQueryTable</c> /
	/// <c>RelationshipClassJoinDefinition</c> logic used by Topgis, working on the platform-
	/// agnostic <see cref="IRelationshipClass"/> abstraction:
	/// <list type="bullet">
	/// <item>1:1 / 1:n (foreign key on the destination table): a two-table join.</item>
	/// <item>Attributed or m:n relationship classes: a three-table join via the bridge
	/// (relationship) table.</item>
	/// </list>
	/// </summary>
	/// <remarks>
	/// The join condition is expressed as an ANSI JOIN inside the <see cref="QueryDef.Tables"/>
	/// expression. This is the syntax understood by enterprise geodatabases (SQL Server,
	/// PostgreSQL, Oracle 12c+); it is not supported on file geodatabases or feature services -
	/// clients targeting those should fall back to the InMemoryJoin transformer.
	/// </remarks>
	public static class RelationshipClassJoinUtils
	{
		/// <summary>
		/// The maximum length of a generated query table name
		/// (see AO <c>TableJoinUtils._maxNameLength</c>).
		/// </summary>
		private const int _maxQueryTableNameLength = 59;

		/// <summary>
		/// Adapts the join type, which is defined with respect to the requested table list order,
		/// to the origin -> destination direction of the relationship class. Port of the AO
		/// <c>RelationshipClassUtils.AdaptJoinTypeToRelationshipDirection</c>.
		/// </summary>
		/// <param name="relationshipClass">The relationship class.</param>
		/// <param name="tables">The requested tables in their list order (the join type refers to
		/// this order). Empty or null means origin/destination order is assumed.</param>
		/// <param name="joinType">The join type w.r.t. the list order of the tables.</param>
		public static JoinType AdaptJoinTypeToRelationshipDirection(
			[NotNull] IRelationshipClass relationshipClass,
			[CanBeNull] IList<string> tables,
			JoinType joinType)
		{
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));

			if (tables == null || tables.Count == 0)
			{
				return joinType;
			}

			ValidateTables(relationshipClass, tables);

			if (joinType == JoinType.InnerJoin ||
			    IsSameTable(tables[0], relationshipClass.OriginClass))
			{
				// return as is
				return joinType;
			}

			// switch the outer join side
			switch (joinType)
			{
				case JoinType.LeftJoin:
					return JoinType.RightJoin;

				case JoinType.RightJoin:
					return JoinType.LeftJoin;

				default:
					throw new ArgumentOutOfRangeException(
						nameof(joinType), joinType, "Unhandled join type");
			}
		}

		/// <summary>
		/// Creates the <see cref="QueryDef"/> that joins the tables participating in the
		/// specified relationship class. Port of the AO <c>TableJoinUtils.CreateQueryDef</c>.
		/// </summary>
		/// <param name="relationshipClass">The relationship class.</param>
		/// <param name="joinType">The join type with respect to the origin -> destination
		/// direction (see <see cref="AdaptJoinTypeToRelationshipDirection"/>).</param>
		/// <param name="whereClause">An optional additional where clause.</param>
		/// <param name="primaryKeyField">The qualified field to be used to manufacture the
		/// ObjectIDs of the query table (AO: <c>IQueryName2.PrimaryKey</c>;
		/// Pro: <see cref="QueryTableDescription.PrimaryKeys"/>).</param>
		/// <param name="shapeFieldName">The qualified name of the query table's shape field
		/// (<c>&lt;baseFeatureClass&gt;.&lt;shapeField&gt;</c>, the last subfield), or
		/// <c>null</c> for a table-only join (no participating feature class). It is known a
		/// priori, so the wrapper does not have to search for it (see
		/// <see cref="ArcWorkspace.OpenQueryTable"/>).</param>
		[NotNull]
		public static QueryDef CreateQueryDef([NotNull] IRelationshipClass relationshipClass,
		                                      JoinType joinType,
		                                      [CanBeNull] string whereClause,
		                                      [NotNull] out string primaryKeyField,
		                                      [CanBeNull] out string shapeFieldName)
		{
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));

			ITable bridgeTable = UsesRelationshipTable(relationshipClass)
				                     ? OpenBridgeTable(relationshipClass)
				                     : null;

			IFeatureClass baseFeatureClass = GetBaseFeatureClass(relationshipClass, joinType);

			primaryKeyField = GetPrimaryKeyField(relationshipClass, joinType,
			                                     baseFeatureClass, bridgeTable);

			// The shape field is the last subfield appended in GetSubFields (see below); it is
			// the qualified shape field of the base feature class, or null when no feature class
			// participates in the join.
			shapeFieldName = baseFeatureClass != null
				                 ? QualifyFieldName(baseFeatureClass,
				                                    baseFeatureClass.ShapeFieldName)
				                 : null;

			var queryDef = new QueryDef
			               {
				               Tables = GetJoinedTablesExpression(relationshipClass, joinType,
					               bridgeTable),
				               SubFields = GetSubFields(relationshipClass, baseFeatureClass,
				                                        primaryKeyField, bridgeTable)
			               };

			if (! string.IsNullOrEmpty(whereClause))
			{
				queryDef.WhereClause = whereClause;
			}

			return queryDef;
		}

		/// <summary>
		/// Generates the query table name from the relationship class name. Port of the AO
		/// <c>TableJoinUtils.GenerateQueryTableName</c> (unqualified name + "_JOIN", clipped
		/// to the maximum table name length).
		/// </summary>
		[NotNull]
		public static string GenerateQueryTableName(
			[NotNull] IRelationshipClass relationshipClass)
		{
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));

			string name = $"{GetUnqualifiedName(relationshipClass.Name)}_JOIN";

			return name.Length <= _maxQueryTableNameLength
				       ? name
				       : name.Substring(0, _maxQueryTableNameLength);
		}

		[NotNull]
		public static string GetSqlJoinExpression(JoinType joinType)
		{
			switch (joinType)
			{
				case JoinType.InnerJoin:
					return "INNER JOIN";

				case JoinType.LeftJoin:
					return "LEFT OUTER JOIN";

				case JoinType.RightJoin:
					return "RIGHT OUTER JOIN";

				default:
					throw new ArgumentOutOfRangeException(nameof(joinType), joinType, null);
			}
		}

		public static bool UsesRelationshipTable(
			[NotNull] IRelationshipClass relationshipClass)
		{
			return relationshipClass.IsAttributed ||
			       relationshipClass.Cardinality ==
			       esriRelCardinality.esriRelCardinalityManyToMany;
		}

		#region Tables expression

		/// <summary>
		/// Builds the ANSI-JOIN tables expression. Port of the AO
		/// <c>RelationshipClassJoinDefinition.GetTableJoinStatement</c> (foreign-key joins) and
		/// <c>GetBridgeTableJoinCondition</c> (bridge-table joins). A right join is expressed as
		/// a left join with the join order inverted, so the driving (first) table is always the
		/// one whose rows are retained.
		/// </summary>
		[NotNull]
		private static string GetJoinedTablesExpression(
			[NotNull] IRelationshipClass relationshipClass,
			JoinType joinType,
			[CanBeNull] ITable bridgeTable)
		{
			return bridgeTable != null
				       ? GetBridgeTableJoinExpression(relationshipClass, joinType, bridgeTable)
				       : GetForeignKeyJoinExpression(relationshipClass, joinType);
		}

		[NotNull]
		private static string GetForeignKeyJoinExpression(
			[NotNull] IRelationshipClass relationshipClass,
			JoinType joinType)
		{
			esriRelCardinality cardinality = relationshipClass.Cardinality;

			if (cardinality != esriRelCardinality.esriRelCardinalityOneToOne &&
			    cardinality != esriRelCardinality.esriRelCardinalityOneToMany)
			{
				throw new NotSupportedException(
					$"Unsupported cardinality '{cardinality}' for non-attributed " +
					$"relationship class: {relationshipClass.Name}");
			}

			string originPK = GetOriginPK(relationshipClass);
			string destinationFK = GetDestinationFK(relationshipClass);

			string firstTable;
			string joinTable;
			JoinType useJoin = joinType;

			if (joinType != JoinType.RightJoin)
			{
				firstTable = relationshipClass.OriginClass.Name;
				joinTable = relationshipClass.DestinationClass.Name;
			}
			else
			{
				// invert statement
				firstTable = relationshipClass.DestinationClass.Name;
				joinTable = relationshipClass.OriginClass.Name;
				useJoin = JoinType.LeftJoin;
			}

			// NOTE: the ON condition is invariant w.r.t. the join direction; it always equates
			// the origin's primary key with the destination's foreign key.
			return $"{firstTable} {GetSqlJoinExpression(useJoin)} {joinTable} " +
			       $"ON {originPK} = {destinationFK}";
		}

		[NotNull]
		private static string GetBridgeTableJoinExpression(
			[NotNull] IRelationshipClass relationshipClass,
			JoinType joinType,
			[NotNull] ITable bridgeTable)
		{
			string firstTable;
			string joinTable;
			JoinType useJoin = joinType;

			string firstPK;
			string joinPK;
			string firstFK;
			string joinFK;

			if (joinType != JoinType.RightJoin)
			{
				firstTable = relationshipClass.OriginClass.Name;
				joinTable = relationshipClass.DestinationClass.Name;

				firstPK = GetOriginPK(relationshipClass);
				joinPK = GetDestinationPK(relationshipClass);

				firstFK = GetBridgeOriginFK(relationshipClass, bridgeTable);
				joinFK = GetBridgeDestinationFK(relationshipClass, bridgeTable);
			}
			else
			{
				// invert statement
				useJoin = JoinType.LeftJoin;

				firstTable = relationshipClass.DestinationClass.Name;
				joinTable = relationshipClass.OriginClass.Name;

				firstPK = GetDestinationPK(relationshipClass);
				joinPK = GetOriginPK(relationshipClass);

				firstFK = GetBridgeDestinationFK(relationshipClass, bridgeTable);
				joinFK = GetBridgeOriginFK(relationshipClass, bridgeTable);
			}

			var sb = new StringBuilder();

			sb.Append(firstTable);

			sb.Append($" {GetSqlJoinExpression(useJoin)} {bridgeTable.Name}");
			sb.Append($" ON {firstPK} = {firstFK}");

			sb.Append($" {GetSqlJoinExpression(useJoin)} {joinTable}");
			sb.Append($" ON {joinFK} = {joinPK}");

			return sb.ToString();
		}

		#endregion

		#region Base feature class / primary key

		/// <summary>
		/// The feature class whose shape field the query table uses (null for a plain joined
		/// table). Port of the AO <c>TableJoinUtils.GetBaseFeatureClass</c>.
		/// </summary>
		[CanBeNull]
		private static IFeatureClass GetBaseFeatureClass(
			[NotNull] IRelationshipClass relationshipClass,
			JoinType joinType)
		{
			switch (joinType)
			{
				case JoinType.InnerJoin:
					// prefer the origin feature class
					return relationshipClass.OriginClass as IFeatureClass ??
					       relationshipClass.DestinationClass as IFeatureClass;

				case JoinType.LeftJoin:
					// must be the origin class (origin is left, destination is right)
					return relationshipClass.OriginClass as IFeatureClass;

				case JoinType.RightJoin:
					// must be the destination class (origin is left, destination is right)
					return relationshipClass.DestinationClass as IFeatureClass;

				default:
					throw new ArgumentOutOfRangeException(nameof(joinType));
			}
		}

		/// <summary>
		/// The qualified field used to manufacture the query table's ObjectIDs. Port of the AO
		/// <c>TableJoinUtils.GetPrimaryKey</c> / <c>GetPrimaryKeyTable</c>.
		/// </summary>
		[NotNull]
		private static string GetPrimaryKeyField(
			[NotNull] IRelationshipClass relationshipClass,
			JoinType joinType,
			[CanBeNull] IFeatureClass baseFeatureClass,
			[CanBeNull] ITable bridgeTable)
		{
			IClass table = GetPrimaryKeyTable(relationshipClass, joinType,
			                                  baseFeatureClass, bridgeTable);

			return QualifyFieldName(table, table.OIDFieldName);
		}

		[NotNull]
		private static IClass GetPrimaryKeyTable(
			[NotNull] IRelationshipClass relationshipClass,
			JoinType joinType,
			[CanBeNull] IFeatureClass baseFeatureClass,
			[CanBeNull] ITable bridgeTable)
		{
			switch (joinType)
			{
				case JoinType.InnerJoin:
					if (bridgeTable != null)
					{
						// use the RID from the relationship table
						return bridgeTable;
					}

					return baseFeatureClass != null &&
					       InvolvesObjectClass(relationshipClass, baseFeatureClass)
						       ? baseFeatureClass
						       : relationshipClass.DestinationClass;

				case JoinType.LeftJoin:
					// origin is left
					return relationshipClass.OriginClass;

				case JoinType.RightJoin:
					// destination is right
					return relationshipClass.DestinationClass;

				default:
					throw new ArgumentOutOfRangeException(nameof(joinType));
			}
		}

		private static bool InvolvesObjectClass(
			[NotNull] IRelationshipClass relationshipClass,
			[NotNull] IObjectClass objectClass)
		{
			return IsSameTable(objectClass.Name, relationshipClass.OriginClass) ||
			       IsSameTable(objectClass.Name, relationshipClass.DestinationClass);
		}

		#endregion

		#region Subfields

		/// <summary>
		/// Assembles the qualified subfields list. Port of the AO
		/// <c>TableJoinUtils.GetSubFieldsString</c> (with
		/// <c>includeOnlyOIDFields = false</c> and
		/// <c>includeAllRelationshipTableFields = true</c>). Field order matters (known driver
		/// issues, e.g. NIM050134): the primary key comes before any other OID field, the base
		/// feature class OID directly precedes the shape field, and the shape field comes last.
		/// </summary>
		[NotNull]
		private static string GetSubFields([NotNull] IRelationshipClass relationshipClass,
		                                   [CanBeNull] IFeatureClass baseFeatureClass,
		                                   [NotNull] string primaryKeyField,
		                                   [CanBeNull] ITable bridgeTable)
		{
			// If there is a valid base feature class and more than one feature class is
			// involved: keep only the OID of the base feature class in the subfields list.
			IObjectClass exclusiveOIDFieldClass =
				baseFeatureClass != null &&
				relationshipClass.OriginClass is IFeatureClass &&
				relationshipClass.DestinationClass is IFeatureClass
					? baseFeatureClass
					: null;

			var subfields = new List<string>();

			// add the non-geometry fields of the joined classes
			foreach (IObjectClass objectClass in new[]
			                                     {
				                                     relationshipClass.OriginClass,
				                                     relationshipClass.DestinationClass
			                                     })
			{
				bool excludeOIDField = exclusiveOIDFieldClass != null &&
				                       ! IsSameTable(objectClass.Name, exclusiveOIDFieldClass);

				AddFields(subfields, objectClass, excludeOIDField);
			}

			// In inner joins it's important that the RID comes after the OBJECTID of the
			// non-baseFeatureClass!
			if (bridgeTable != null)
			{
				// append all bridge table fields
				AddFields(subfields, bridgeTable, excludeOIDField: false);
			}

			string lastFieldBeforeShape =
				baseFeatureClass != null
					? QualifyFieldName(baseFeatureClass, baseFeatureClass.OIDFieldName)
					: primaryKeyField;

			var sb = new StringBuilder();

			// The primary key field must come before any other OID field:
			if (lastFieldBeforeShape.Equals(primaryKeyField,
			                                StringComparison.OrdinalIgnoreCase))
			{
				AppendField(sb, lastFieldBeforeShape);
			}

			foreach (string subfield in subfields)
			{
				if (lastFieldBeforeShape.Equals(subfield, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				AppendField(sb, subfield);
			}

			// add the base feature class OID *after* the other non-shape fields and *before*
			// the shape field
			if (! lastFieldBeforeShape.Equals(primaryKeyField,
			                                  StringComparison.OrdinalIgnoreCase))
			{
				AppendField(sb, lastFieldBeforeShape);
			}

			// add the shape field
			if (baseFeatureClass != null)
			{
				AppendField(sb, QualifyFieldName(baseFeatureClass,
				                                 baseFeatureClass.ShapeFieldName));
			}

			return sb.ToString();
		}

		/// <summary>
		/// Adds the qualified non-geometry fields of a class. Port of the AO
		/// <c>TableJoinUtils.AddFields</c> (shape, shape.area and shape.length fields are
		/// excluded; the OID field can be excluded on request).
		/// </summary>
		private static void AddFields([NotNull] ICollection<string> fields,
		                              [NotNull] IClass objectClass,
		                              bool excludeOIDField)
		{
			var featureClass = objectClass as IFeatureClass;

			string areaFieldName = featureClass?.AreaField?.Name;
			string lengthFieldName = featureClass?.LengthField?.Name;

			foreach (IField field in objectClass.Fields)
			{
				esriFieldType fieldType = field.Type;

				if (excludeOIDField && fieldType == esriFieldType.esriFieldTypeOID)
				{
					// ignore oid field if specified
					continue;
				}

				if (fieldType == esriFieldType.esriFieldTypeGeometry)
				{
					// ignore shape field
					continue;
				}

				if (string.Equals(field.Name, areaFieldName,
				                  StringComparison.OrdinalIgnoreCase) ||
				    string.Equals(field.Name, lengthFieldName,
				                  StringComparison.OrdinalIgnoreCase))
				{
					// ignore shape.area / shape.length fields
					continue;
				}

				fields.Add(QualifyFieldName(objectClass, field.Name));
			}
		}

		private static void AppendField([NotNull] StringBuilder stringBuilder,
		                                [NotNull] string qualifiedFieldName)
		{
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append(",");
			}

			stringBuilder.Append(qualifiedFieldName);
		}

		#endregion

		#region Qualified key fields

		[NotNull]
		private static string GetOriginPK([NotNull] IRelationshipClass relationshipClass)
		{
			return QualifyFieldName(relationshipClass.OriginClass,
			                        relationshipClass.OriginPrimaryKey);
		}

		[NotNull]
		private static string GetDestinationPK([NotNull] IRelationshipClass relationshipClass)
		{
			return QualifyFieldName(relationshipClass.DestinationClass,
			                        relationshipClass.DestinationPrimaryKey);
		}

		/// <summary>
		/// The foreign key referencing the origin's primary key. On the destination table for
		/// simple (foreign key) relationship classes.
		/// </summary>
		[NotNull]
		private static string GetDestinationFK([NotNull] IRelationshipClass relationshipClass)
		{
			return QualifyFieldName(relationshipClass.DestinationClass,
			                        relationshipClass.OriginForeignKey);
		}

		/// <summary>
		/// The foreign key on the bridge table referencing the origin's primary key.
		/// </summary>
		[NotNull]
		private static string GetBridgeOriginFK([NotNull] IRelationshipClass relationshipClass,
		                                        [NotNull] ITable bridgeTable)
		{
			return QualifyFieldName(bridgeTable, relationshipClass.OriginForeignKey);
		}

		/// <summary>
		/// The foreign key on the bridge table referencing the destination's primary key.
		/// </summary>
		[NotNull]
		private static string GetBridgeDestinationFK(
			[NotNull] IRelationshipClass relationshipClass,
			[NotNull] ITable bridgeTable)
		{
			return QualifyFieldName(bridgeTable, relationshipClass.DestinationForeignKey);
		}

		#endregion

		#region Helpers

		/// <summary>
		/// Opens the bridge (relationship) table of an attributed or m:n relationship class.
		/// </summary>
		[NotNull]
		private static ITable OpenBridgeTable([NotNull] IRelationshipClass relationshipClass)
		{
			var featureWorkspace = (IFeatureWorkspace) relationshipClass.Workspace;

			return Assert.NotNull(featureWorkspace.OpenTable(relationshipClass.Name),
			                      "Cannot open the relationship table for {0}",
			                      relationshipClass.Name);
		}

		private static void ValidateTables([NotNull] IRelationshipClass relationshipClass,
		                                   [NotNull] IList<string> tables)
		{
			if (tables.Count < 2)
			{
				return;
			}

			bool valid =
				IsSameTable(tables[0], relationshipClass.OriginClass) &&
				IsSameTable(tables[1], relationshipClass.DestinationClass) ||
				IsSameTable(tables[0], relationshipClass.DestinationClass) &&
				IsSameTable(tables[1], relationshipClass.OriginClass);

			if (! valid)
			{
				throw new InvalidOperationException(
					$"The tables [{string.Join(", ", tables)}] must be the origin/destination " +
					$"('{relationshipClass.OriginClass.Name}'/" +
					$"'{relationshipClass.DestinationClass.Name}') of relationship class " +
					$"'{relationshipClass.Name}'.");
			}
		}

		private static bool IsSameTable([NotNull] string tableName,
		                                [NotNull] IObjectClass objectClass)
		{
			return tableName.Equals(objectClass.Name, StringComparison.OrdinalIgnoreCase);
		}

		[NotNull]
		private static string QualifyFieldName([NotNull] IClass table,
		                                       [NotNull] string fieldName)
		{
			return $"{table.Name}.{fieldName}";
		}

		[NotNull]
		private static string GetUnqualifiedName([NotNull] string datasetName)
		{
			int lastSeparatorIndex = datasetName.LastIndexOf('.');

			return lastSeparatorIndex < 0
				       ? datasetName
				       : datasetName.Substring(lastSeparatorIndex + 1);
		}

		#endregion
	}
}
