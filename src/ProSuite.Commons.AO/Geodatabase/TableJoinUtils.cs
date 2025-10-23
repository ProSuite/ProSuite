using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AO.Geodatabase
{
	public static class TableJoinUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private const int _maxNameLength = 59;

		/// <summary>
		/// Creates a query table for a list of relationship classes and a join type.
		/// NOTE: The OIDs of these tables are NOT unique (TOP-5598). Use CreateReadOnlyQueryTable instead.
		/// </summary>
		/// <param name="relationshipClass">The relationship class.</param>
		/// <param name="joinType">Type of the join.</param>
		/// <param name="includeOnlyOIDFields">if set to <c>true</c> only key fields (plus the shape field if applicable) will be included
		/// in the query table.</param>
		/// <param name="excludeShapeField">if set to <c>true</c> no shape field will be included in the field list. 
		/// In this case the resulting table can never be used as a feature class.</param>
		/// <param name="whereClause">An optional where clause</param>
		/// <param name="queryTableName">An optional name for the query table. If not set, it's generated</param>
		/// <returns>
		/// A query table. This will be a feature class if
		/// <see cref="CanCreateQueryFeatureClass(System.Collections.Generic.IList{ESRI.ArcGIS.Geodatabase.IRelationshipClass},JoinType)"/>
		/// returns true on the relationship classes and join type./&gt;
		/// </returns>
		[NotNull]
		public static ITable CreateQueryTable(
			[NotNull] IRelationshipClass relationshipClass,
			JoinType joinType = JoinType.InnerJoin,
			bool includeOnlyOIDFields = false,
			bool excludeShapeField = false,
			string whereClause = null,
			string queryTableName = null)

		{
			return CreateQueryTable(new[] { relationshipClass }, joinType,
			                        includeOnlyOIDFields, excludeShapeField,
			                        whereClause, queryTableName);
		}

		/// <summary>
		/// Creates a query table for a list of relationship classes and a join type.
		/// NOTE: The OIDs of these tables are NOT unique (TOP-5598). Use CreateReadOnlyQueryTable instead.
		/// </summary>
		/// <param name="relationshipClasses">The relationship classes.</param>
		/// <param name="joinType">Type of the join.</param>
		/// <param name="includeOnlyOIDFields">if set to <c>true</c> only key fields (plus the shape field if applicable) will be included
		/// in the query table.</param>
		/// <param name="excludeShapeField">if set to <c>true</c> no shape field will be included in the field list. 
		/// In this case the resulting table can never be used as a feature class.</param>
		/// <param name="whereClause">An optional where clause</param>
		/// <param name="queryTableName">An optional name for the query table. If not set, it's generated</param>
		/// <returns>
		/// A query table. This will be a feature class if
		/// <see cref="CanCreateQueryFeatureClass(IList{IRelationshipClass},JoinType)"/>
		/// returns true on the relationship classes and join type./&gt;
		/// </returns>
		[NotNull]
		public static ITable CreateQueryTable(
			[NotNull] IList<IRelationshipClass> relationshipClasses,
			JoinType joinType = JoinType.InnerJoin,
			bool includeOnlyOIDFields = false,
			bool excludeShapeField = false,
			string whereClause = null,
			string queryTableName = null)
		{
			Assert.ArgumentNotNull(relationshipClasses, nameof(relationshipClasses));

			string name = ! string.IsNullOrWhiteSpace(queryTableName)
				              ? queryTableName
				              : GenerateQueryTableName(relationshipClasses);

			return CreateQueryTable(relationshipClasses, joinType, name,
			                        includeOnlyOIDFields, excludeShapeField,
			                        whereClause, out string _);
		}

		/// <summary>
		/// Creates a query table for a list of relationship classes and a join type wrapped in a
		/// read-only table.
		/// The OIDs of this class are unique or NULL, in case of a right join with no left row where the foreign key
		/// is in the left table or vice versa.
		/// </summary>
		/// <param name="relationshipClass">The relationship class.</param>
		/// <param name="joinType">Type of the join.</param>
		/// <param name="includeOnlyOIDFields">if set to <c>true</c> only key fields (plus the shape field if applicable) will be included
		/// in the query table.</param>
		/// <param name="excludeShapeField">if set to <c>true</c> no shape field will be included in the field list. 
		/// In this case the resulting table can never be used as a feature class.</param>
		/// <param name="whereClause">An optional where clause</param>
		/// <param name="queryTableName">An optional name for the query table. If not set, it's generated</param>
		/// <returns>
		/// A query table. This will be a feature class if
		/// <see cref="CanCreateQueryFeatureClass(IList{IRelationshipClass},JoinType)"/>
		/// returns true on the relationship classes and join type./&gt;
		/// </returns>
		public static IReadOnlyTable CreateReadOnlyQueryTable(
			[NotNull] IRelationshipClass relationshipClass,
			JoinType joinType = JoinType.InnerJoin,
			bool includeOnlyOIDFields = false,
			bool excludeShapeField = false,
			string whereClause = null,
			string queryTableName = null)
		{
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));

			var relationshipClassList = new List<IRelationshipClass> { relationshipClass };

			return CreateReadOnlyQueryTable(relationshipClassList, joinType,
			                                includeOnlyOIDFields, excludeShapeField,
			                                whereClause, queryTableName);
		}

		/// <summary>
		/// Creates a query table for a list of relationship classes and a join type wrapped in a
		/// read-only table.
		/// The OIDs of this class are unique or NULL, in case of a right join with no left row where the foreign key
		/// is in the left table or vice versa.
		/// </summary>
		/// <param name="relationshipClasses">The relationship classes.</param>
		/// <param name="joinType">Type of the join.</param>
		/// <param name="includeOnlyOIDFields">if set to <c>true</c> only key fields (plus the shape field if applicable) will be included
		/// in the query table.</param>
		/// <param name="excludeShapeField">if set to <c>true</c> no shape field will be included in the field list. 
		/// In this case the resulting table can never be used as a feature class.</param>
		/// <param name="whereClause">An optional where clause</param>
		/// <param name="queryTableName">An optional name for the query table. If not set, it's generated</param>
		/// <returns>
		/// A query table. This will be a feature class if
		/// <see cref="CanCreateQueryFeatureClass(IList{IRelationshipClass},JoinType)"/>
		/// returns true on the relationship classes and join type./&gt;
		/// </returns>
		public static IReadOnlyTable CreateReadOnlyQueryTable(
			[NotNull] IList<IRelationshipClass> relationshipClasses,
			JoinType joinType = JoinType.InnerJoin,
			bool includeOnlyOIDFields = false,
			bool excludeShapeField = false,
			string whereClause = null,
			string queryTableName = null)
		{
			Assert.ArgumentNotNull(relationshipClasses, nameof(relationshipClasses));

			string name = ! string.IsNullOrWhiteSpace(queryTableName)
				              ? queryTableName
				              : GenerateQueryTableName(relationshipClasses);

			ITable queryTable = CreateQueryTable(
				relationshipClasses, joinType, name, includeOnlyOIDFields, excludeShapeField,
				whereClause, out string primaryKeyField);

			IEnumerable<ITable> baseClasses =
				RelationshipClassUtils.GetObjectClasses(relationshipClasses).Cast<ITable>();

			var result = ReadOnlyTableFactory.CreateQueryTable(
				queryTable, primaryKeyField, baseClasses);

			return result;
		}

		[NotNull]
		public static IQueryDef CreateQueryDef([NotNull] IRelationshipClass relClass,
		                                       JoinType joinType = JoinType.InnerJoin,
		                                       bool includeOnlyOIDFields = false,
		                                       bool excludeShapeField = false)
		{
			Assert.ArgumentNotNull(relClass, nameof(relClass));

			return CreateQueryDef(new List<IRelationshipClass> { relClass },
			                      joinType, includeOnlyOIDFields, excludeShapeField,
			                      out string _, out esriGeometryType _, out string _);
		}

		[NotNull]
		public static ITable CreateQueryTable(
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] string datasetName,
			[NotNull] string primaryKey,
			[NotNull] string tables,
			[NotNull] string whereClause,
			[CanBeNull] string subfields = null)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNullOrEmpty(datasetName, nameof(datasetName));
			Assert.ArgumentNotNullOrEmpty(primaryKey, nameof(primaryKey));
			Assert.ArgumentNotNullOrEmpty(tables, nameof(tables));
			Assert.ArgumentNotNullOrEmpty(whereClause, nameof(whereClause));

			IQueryDef queryDef = workspace.CreateQueryDef();

			queryDef.Tables = tables;
			queryDef.SubFields = GetTableQuerySubfields(subfields, tables, workspace, primaryKey);
			queryDef.WhereClause = whereClause;

			IQueryName2 queryName = new FeatureQueryNameClass
			                        {
				                        PrimaryKey = primaryKey,
				                        CopyLocally = false,
				                        QueryDef = queryDef
			                        };

			var name = (IDatasetName) queryName;
			name.WorkspaceName = WorkspaceUtils.GetWorkspaceName(workspace);
			name.Name = datasetName;

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.Debug("Creating query-based feature class");

				using (_msg.IncrementIndentation())
				{
					_msg.DebugFormat("SELECT {0} FROM {1} WHERE {2}",
					                 queryDef.SubFields,
					                 queryDef.Tables,
					                 queryDef.WhereClause);
					_msg.DebugFormat("Primary key: {0}", queryName.PrimaryKey);
					_msg.DebugFormat("FClass name: {0}", datasetName);
				}
			}

			return (ITable) ((IName) queryName).Open();
		}

		/// <summary>
		/// Creates a query-based feature class.
		/// </summary>
		/// <param name="workspace">The workspace.</param>
		/// <param name="datasetName">Name of the resulting feature class.</param>
		/// <param name="primaryKey">The primary key field name.</param>
		/// <param name="shapeFieldName">Name of the shape field.</param>
		/// <param name="tables">The list of table names (string, comma-separated).</param>
		/// <param name="whereClause">The where clause.</param>
		/// <param name="subfields">The list of fields to include (string, comma-separated).</param>
		/// <returns></returns>
		[NotNull]
		public static IFeatureClass CreateQueryFeatureClass(
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] string datasetName,
			[NotNull] string primaryKey,
			[NotNull] string shapeFieldName,
			[NotNull] string tables,
			[NotNull] string whereClause,
			[CanBeNull] string subfields = null)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNullOrEmpty(datasetName, nameof(datasetName));
			Assert.ArgumentNotNullOrEmpty(primaryKey, nameof(primaryKey));
			Assert.ArgumentNotNullOrEmpty(shapeFieldName, nameof(shapeFieldName));
			Assert.ArgumentNotNullOrEmpty(tables, nameof(tables));
			Assert.ArgumentNotNullOrEmpty(whereClause, nameof(whereClause));

			IQueryDef queryDef = workspace.CreateQueryDef();

			queryDef.Tables = tables;
			queryDef.SubFields = GetFeatureClassQuerySubfields(subfields, tables, workspace,
			                                                   primaryKey, shapeFieldName);
			queryDef.WhereClause = whereClause;

			IQueryName2 queryName = new FeatureQueryNameClass
			                        {
				                        PrimaryKey = primaryKey,
				                        CopyLocally = false,
				                        QueryDef = queryDef
			                        };

			((IFeatureClassName) queryName).ShapeFieldName = shapeFieldName;

			var name = (IDatasetName) queryName;
			name.WorkspaceName = WorkspaceUtils.GetWorkspaceName(workspace);
			name.Name = datasetName;

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.Debug("Creating query-based feature class");

				using (_msg.IncrementIndentation())
				{
					_msg.DebugFormat("SELECT {0} FROM {1} WHERE {2}",
					                 queryDef.SubFields,
					                 queryDef.Tables,
					                 queryDef.WhereClause);
					_msg.DebugFormat("Primary key: {0}", queryName.PrimaryKey);
					_msg.DebugFormat("Shape field: {0}", shapeFieldName);
					_msg.DebugFormat("FClass name: {0}", datasetName);
				}
			}

			return (IFeatureClass) ((IName) queryName).Open();
		}

		public static bool CanCreateQueryFeatureClass(
			[NotNull] IRelationshipClass relationshipClass,
			JoinType joinType)
		{
			return CanCreateQueryFeatureClass(new[] { relationshipClass }, joinType);
		}

		public static bool CanCreateQueryFeatureClass(
			[NotNull] IList<IRelationshipClass> relationshipClasses,
			JoinType joinType)
		{
			return GetBaseFeatureClass(relationshipClasses, joinType) != null;
		}

		/// <summary>
		/// Creates a feature query name using an inner join between two feature classes or
		/// a feature class and a table.
		/// It uses the same workspace as the provided relationship class which relates the
		/// two tables to each other. All subfields will be included except the unused objectid
		/// fields and the unused shape field.
		/// </summary>
		/// <param name="relationshipClass">The relationship class that defines the join.</param>
		/// <param name="baseFeatureClass">The primary feature class, i.e. the one containing
		/// the shape field to be used.</param>
		/// <param name="resultDatasetName">The resulting dataset's name.</param>
		/// <param name="joinType">The join type.</param>
		/// <param name="primaryKeyFieldName">The fully qualified primary key field to use. For
		/// correct behaviour in a feature layer, it should be a unique field, such as the RID.</param>
		/// <returns></returns>
		[NotNull]
		public static IQueryName2 CreateFeatureQueryName(
			[NotNull] IRelationshipClass relationshipClass,
			[NotNull] IFeatureClass baseFeatureClass,
			[NotNull] string resultDatasetName,
			JoinType joinType,
			[CanBeNull] string primaryKeyFieldName)
		{
			// NOTE: if using baseFeatureClass.OIDFieldName in Make Query Table (and Add joined layer command) and
			//		 identify only finds 1 record instead of several (and selecting selects only 1) -> RID works
			//		 just using a cursor works fine if the (non-unique) baseFeatureClass.OIDFieldName is used
			// -> use TableJoinUtils.GetPrimaryKey() if no primary key field is provided, which uses RID. This is the safer option
			if (StringUtils.IsNullOrEmptyOrBlank(primaryKeyFieldName))
			{
				primaryKeyFieldName = GetPrimaryKey(relationshipClass, joinType, baseFeatureClass);
			}

			IQueryDef queryDef = CreateQueryDef(relationshipClass, joinType, baseFeatureClass);

			var workspace = (IFeatureWorkspace) ((IDataset) relationshipClass).Workspace;

			return CreateFeatureQueryName(baseFeatureClass, resultDatasetName,
			                              primaryKeyFieldName, workspace, queryDef);
		}

		public static GdbFeatureClass CreateJoinedGdbFeatureClass(
			[NotNull] IRelationshipClass relationshipClass,
			[NotNull] IFeatureClass geometryEndClass,
			[NotNull] string name,
			JoinType joinType = JoinType.InnerJoin,
			bool ensureUniqueIds = false)
		{
			return (GdbFeatureClass) CreateJoinedGdbTable(relationshipClass,
			                                              (ITable) geometryEndClass, name,
			                                              joinType, ensureUniqueIds);
		}

		public static GdbTable CreateJoinedGdbTable(
			[NotNull] IRelationshipClass relationshipClass,
			[NotNull] ITable leftTable,
			[NotNull] string name,
			JoinType joinType = JoinType.InnerJoin,
			bool ensureUniqueIds = false)
		{
			AssociationDescription associationDescription =
				AssociationDescriptionUtils.CreateAssociationDescription(relationshipClass);

			IReadOnlyTable roGeometryTable = ReadOnlyTableFactory.Create(leftTable);

			return CreateJoinedGdbTable(
				associationDescription, roGeometryTable, name, ensureUniqueIds, joinType);
		}

		/// <summary>
		/// Creates a GdbTable or GdbFeatureClass representing the join of the specified association.
		/// </summary>
		/// <param name="associationDescription">The definition of the join</param>
		/// <param name="geometryTable">The 'left' table which, if it has a geometry field, will
		/// be used to define the resulting FeatureClass. If this table has no geometry field, the
		/// result will be a GdbTable without geometry field.</param>
		/// <param name="name">The name of the result class.</param>
		/// <param name="ensureUniqueIds">Whether some extra performance penalty should be accepted
		/// in order to guarantee the uniqueness of the OBJECTID field. This is relevant for
		/// many-to-many joins.</param>
		/// <param name="joinType"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public static GdbTable CreateJoinedGdbTable(
			[NotNull] AssociationDescription associationDescription,
			[CanBeNull] IReadOnlyTable geometryTable,
			[NotNull] string name,
			bool ensureUniqueIds,
			JoinType joinType = JoinType.InnerJoin)
		{
			// TODO: Support both tables having no geometry -> get all records from the smaller?
			//       Or possibly always query the left table first (in inner joins) to allow for optimization by user.
			//       support left table having the geometry but using right join -> swap, leftJoin
			if (joinType == JoinType.RightJoin)
			{
				throw new NotImplementedException("RightJoin is not yet implemented.");
			}

			// If the geometry table is null, the 'left' table will be used (if it has a geometry).
			geometryTable = geometryTable ?? associationDescription.Table1;

			IReadOnlyTable otherTable = associationDescription.Table1.Equals(geometryTable)
				                            ? associationDescription.Table2
				                            : associationDescription.Table1;

			BackingDataset BackingDatasetFactoryFunc(GdbTable joinedSchema)
			{
				return new JoinedDataset(
					       associationDescription, geometryTable, otherTable, joinedSchema)
				       {
					       JoinType = joinType,
					       IncludeMtoNAssociationRows = ensureUniqueIds
				       };
			}

			var result =
				CreateJoinedGdbTable(name, BackingDatasetFactoryFunc, geometryTable, otherTable);

			// Make sure the ObjectID field is explicitly set (or unset) rather than relying on the
			// implicit logic in AddField which just makes the last added field of type objectId the
			// OID field.
			DefineOIDField(ensureUniqueIds, associationDescription, joinType, result,
			               geometryTable);

			return result;
		}

		/// <summary>
		/// Determines the table which will provide unique OIDs in the specified association.
		/// </summary>
		/// <param name="associationDescription"></param>
		/// <param name="joinType"></param>
		/// <param name="leftTable"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public static IReadOnlyTable DetermineOIDTable(
			AssociationDescription associationDescription,
			JoinType joinType, IReadOnlyTable leftTable)
		{
			IReadOnlyTable result;

			if (associationDescription is ForeignKeyAssociationDescription fkAssociation)
			{
				// The current assumption is that the left table contains the foreign key
				// and has therefore a unique OID. However, this logic is not enforced!
				// -> Consider using some IUniqueIdProvider implementation and keep
				// using the left table's OID by default.

				// The left/referencing table contains the foreign key and hence can only point to one
				// referenced row and hence exists at most once in the result -> OID
				result = fkAssociation.ReferencingTable;

				if (joinType == JoinType.LeftJoin)
				{
					bool isRightTableSuggested =
						! leftTable.Equals(fkAssociation.ReferencingTable);

					if (isRightTableSuggested)
					{
						// The right table provides the unique key, but it could have null-rows in LeftJoin
						if (fkAssociation.HasOneToOneCardinality)
						{
							// -> Only allow the left table if the cardinality is known 1:1:
							result = fkAssociation.ReferencedTable;
						}
						else
						{
							// No reasonable OID:
							result = null;
						}
					}
				}
				else if (joinType == JoinType.RightJoin)
				{
					throw new NotImplementedException("Right Join not yet implemented");
				}
			}
			else
			{
				ManyToManyAssociationDescription m2n =
					(ManyToManyAssociationDescription) associationDescription;

				result = m2n.AssociationTable;
			}

			return result;
		}

		public static void DefineOIDField(bool ensureUniqueIds,
		                                  [NotNull] AssociationDescription associationDescription,
		                                  JoinType joinType,
		                                  [NotNull] GdbTable result,
		                                  [NotNull] IReadOnlyTable geometryTable)
		{
			IReadOnlyTable oidSourceTable;

			// For backward compatibility (route layer) assuming the geometry table's OID field is always added
			if (! ensureUniqueIds)
			{
				oidSourceTable = geometryTable;
			}
			else
			{
				oidSourceTable =
					DetermineOIDTable(associationDescription, joinType, geometryTable);
			}

			if (oidSourceTable == null)
			{
				_msg.Debug($"{result.Name} will have no OBJECT ID field.");
				return;
			}

			string oidFieldName = oidSourceTable.HasOID
				                      ? DatasetUtils.QualifyFieldName(oidSourceTable,
					                      oidSourceTable.OIDFieldName)
				                      : null;

			// In case of m:n ensure the field is in the result schema:
			if (ensureUniqueIds &&
			    associationDescription is ManyToManyAssociationDescription m2n)
			{
				var associationTable = m2n.AssociationTable;

				AddFields(associationTable, result, true);
			}

			result.SetOIDFieldName(oidFieldName);

			const string noOid = "<None>";
			_msg.Debug(
				$"{result.Name} will use the following OBJECT ID field: {oidFieldName ?? noOid}.");
		}

		private static GdbTable CreateJoinedGdbTable(
			[NotNull] string name,
			[NotNull] Func<GdbTable, BackingDataset> datasetFactoryFunc,
			IReadOnlyTable geometryEndClass,
			IReadOnlyTable otherEndClass)
		{
			GdbTable result;

			IWorkspace workspace = geometryEndClass.Workspace;

			if (geometryEndClass is IReadOnlyFeatureClass featureClass)
			{
				result = new GdbFeatureClass(null, name, featureClass.ShapeType, null,
				                             datasetFactoryFunc, workspace);
			}
			else
			{
				result = new GdbTable(null, name, null, datasetFactoryFunc, workspace);
			}

			AddFields(geometryEndClass, result, false);
			AddFields(otherEndClass, result, true);

			return result;
		}

		private static void AddFields([NotNull] IReadOnlyTable fromClass,
		                              [NotNull] GdbTable toResult,
		                              bool exceptShapeFields)
		{
			IField lengthField = null;
			IField areaField = null;
			if (exceptShapeFields &&
			    fromClass is IReadOnlyFeatureClass otherFeatureClass)
			{
				lengthField = DatasetUtils.GetLengthField(otherFeatureClass);
				areaField = DatasetUtils.GetAreaField(otherFeatureClass);
			}

			for (int i = 0; i < fromClass.Fields.FieldCount; i++)
			{
				IField field = fromClass.Fields.Field[i];

				if (exceptShapeFields && field.Type == esriFieldType.esriFieldTypeGeometry)
				{
					continue;
				}

				if (field == lengthField || field == areaField)
				{
					// ignore, it would fail in search
					continue;
				}

				toResult.AddField(CreateQualifiedField(field, fromClass));
			}
		}

		private static IField CreateQualifiedField(IField prototype, IObjectClass table)
		{
			var result = (IField) ((IClone) prototype).Clone();

			((IFieldEdit) result).Name_2 = DatasetUtils.QualifyFieldName(table, prototype.Name);

			return result;
		}

		private static IField CreateQualifiedField(IField prototype, IReadOnlyTable table)
		{
			string qualifiedFieldName = DatasetUtils.QualifyFieldName(table, prototype.Name);

			var result = (IField) ((IClone) prototype).Clone();

			// NOTE: In .NET 6 / ArcObjects 10.8 this does nothing for OBJECTID fields!
			((IFieldEdit) result).Name_2 = qualifiedFieldName;

			return result;
		}

		[NotNull]
		private static IQueryDef CreateQueryDef([NotNull] IRelationshipClass relClass,
		                                        JoinType joinType,
		                                        [CanBeNull] IFeatureClass baseFeatureClass)
		{
			Assert.ArgumentNotNull(relClass, nameof(relClass));

			var joinDefinition = new RelationshipClassJoinDefinition(relClass, joinType,
				baseFeatureClass);

			IWorkspace workspace = ((IDataset) relClass).Workspace;
			IQueryDef result = ((IFeatureWorkspace) workspace).CreateQueryDef();

			// Problem found in 9.3.1, confirmed at 10.0 SP5:
			// if FC-FC inner join: FC comes first in table list (to avoid layer mismatch error) and the
			// OID of the non-primary key must be excluded, or it must appear in the subfield list BEFORE the
			// RID. Otherwise the feature's from a cursor can have the wrong shape. This happens both with
			// primary key = the base feature class' OID and also when primary key = RID (Tested with 10.0 SP5)

			// if FC-Table M:N inner join: The table without geometry field should come first in the table list
			// to support "*" as subfield list (which is not allowed for FC-FC case). Otherwise "*" results in
			// incorrect shapes too.
			result.Tables = joinDefinition.GetTableList();
			result.WhereClause = joinDefinition.GetJoinCondition();

			result.SubFields = GetSubFieldsString(
				new List<IRelationshipClass> { relClass },
				baseFeatureClass);

			return result;
		}

		/// <summary>
		/// Creates a feature query name.
		/// </summary>
		/// <param name="baseFeatureClass">The base feature class from which to use the shape field.</param>
		/// <param name="resultDatasetName">Name of the result dataset.</param>
		/// <param name="primaryKey">The primary key. If a join between 2 feature classes is used, take the primary feature class' object id (despite it not being unique)</param>
		/// <param name="workspace">The workspace in which to run the query (not necessarily the same as the baseFeatureClass, which is only supplied to provide
		/// the shape information).</param>
		/// <param name="queryDef">The query def. Note the restrictions on the table list and the included shape/oid fields in the sub fields</param>
		/// <param name="copyLocally">if set to <c>true</c> [copy locally].</param>
		/// <returns></returns>
		[NotNull]
		private static IQueryName2 CreateFeatureQueryName(
			[NotNull] IFeatureClass baseFeatureClass,
			[NotNull] string resultDatasetName,
			[NotNull] string primaryKey,
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] IQueryDef queryDef,
			bool copyLocally = false)
		{
			IQueryName2 result =
				new FeatureQueryNameClass
				{
					PrimaryKey = primaryKey,
					QueryDef = queryDef,
					CopyLocally = copyLocally
				};

			var featureClassName = (IFeatureClassName) result;
			featureClassName.ShapeFieldName =
				DatasetUtils.QualifyFieldName(baseFeatureClass, baseFeatureClass.ShapeFieldName);
			featureClassName.ShapeType = baseFeatureClass.ShapeType;

			var dataset = (IDataset) workspace;
			var workspaceName = (IWorkspaceName) dataset.FullName;

			var datasetName = (IDatasetName) result;
			datasetName.WorkspaceName = workspaceName;
			datasetName.Name = resultDatasetName;

			_msg.DebugFormat(
				"Created query name with primary key {0} and shape field name {1} and shape type {2}\n. Query Def: SELECT {3}\n FROM {4}\n WHERE {5}",
				result.PrimaryKey,
				featureClassName.ShapeFieldName, featureClassName.ShapeType,
				queryDef.SubFields, queryDef.Tables, queryDef.WhereClause);

			return result;
		}

		[NotNull]
		private static string GetTableQuerySubfields(
			[CanBeNull] string subfields,
			[NotNull] string tableNames,
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] string primaryKey)
		{
			return IsFullFieldList(subfields)
				       ? GenerateSafeFullFieldList(tableNames, workspace)
				       : EnsureSubFields(subfields, primaryKey);
		}

		[NotNull]
		private static string GetFeatureClassQuerySubfields(
			[CanBeNull] string subfields,
			[NotNull] string tableNames,
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] string primaryKey,
			[NotNull] string shapeFieldName)
		{
			return IsFullFieldList(subfields)
				       ? GenerateSafeFullFieldList(tableNames, workspace, shapeFieldName)
				       : EnsureSubFields(subfields, shapeFieldName, primaryKey);
		}

		private static bool IsFullFieldList([CanBeNull] string subfields)
		{
			const string allFields = "*";
			return string.IsNullOrEmpty(subfields) || subfields.Trim().Equals(allFields);
		}

		[NotNull]
		private static string GenerateSafeFullFieldList(
			[NotNull] string tableNames,
			[NotNull] IFeatureWorkspace workspace,
			string shapeFieldName = null)
		{
			Assert.ArgumentNotNullOrEmpty(tableNames, nameof(tableNames));
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			var fieldNames = new List<string>();

			foreach (string tableName in GetTokens(tableNames))
			{
				ITable table = DatasetUtils.OpenTable(workspace, tableName);
				var featureClass = table as IFeatureClass;

				foreach (IField field in DatasetUtils.GetFields(table))
				{
					if (featureClass != null)
					{
						if (field.Type == esriFieldType.esriFieldTypeGeometry)
						{
							// exclude shape field, will be added at end
							continue;
						}

						if (field == DatasetUtils.GetAreaField(featureClass) ||
						    field == DatasetUtils.GetLengthField(featureClass))
						{
							// exclude the shape area and length fields
							continue;
						}
					}

					string fieldName = DatasetUtils.QualifyFieldName(table, field.Name);

					fieldNames.Add(fieldName);
				}
			}

			// the shape field must be unqualified
			if (shapeFieldName != null)
			{
				fieldNames.Add(shapeFieldName);
			}

			return StringUtils.Concatenate(fieldNames, ",");
		}

		[NotNull]
		private static IEnumerable<string> GetTokens([NotNull] string concatenatedTokens)
		{
			Assert.ArgumentNotNullOrEmpty(concatenatedTokens, nameof(concatenatedTokens));

			var separators = new[] { ' ', ',', ';' };
			return concatenatedTokens.Split(separators, StringSplitOptions.RemoveEmptyEntries);
		}

		private static string EnsureSubFields([CanBeNull] string subFields,
		                                      params string[] fields)
		{
			if (subFields == "*")
			{
				return subFields;
			}

			if (fields.Length == 0)
			{
				return subFields;
			}

			string trimmedSubfields = subFields?.Trim() ?? string.Empty;

			var existingFields =
				new HashSet<string>(StringUtils.SplitAndTrim(trimmedSubfields, ','),
				                    StringComparer.OrdinalIgnoreCase);

			var sb = new StringBuilder(trimmedSubfields);

			foreach (string field in fields)
			{
				if (existingFields.Contains(field))
				{
					continue;
				}

				if (sb.Length == 0)
				{
					sb.Append(field);
				}
				else
				{
					sb.AppendFormat(",{0}", field);
				}
			}

			return sb.ToString();
		}

		[NotNull]
		private static ITable CreateQueryTable(
			[NotNull] IList<IRelationshipClass> relationshipClasses,
			JoinType joinType,
			[NotNull] string queryTableName,
			bool includeOnlyOIDFields, bool excludeShapeField,
			[CanBeNull] string whereClause,
			out string primaryKeyField)
		{
			Assert.ArgumentNotNull(relationshipClasses, nameof(relationshipClasses));
			Assert.ArgumentCondition(relationshipClasses.Count > 0, "0 relationship classes");
			Assert.ArgumentNotNull(queryTableName, nameof(queryTableName));
			Assert.ArgumentCondition(queryTableName.Length <= _maxNameLength,
			                         "Name '{0}' is too long. Maximum length is {1}",
			                         queryTableName, _maxNameLength);

			string shapeFieldName;
			esriGeometryType geometryType;
			IQueryDef queryDef = CreateQueryDef(relationshipClasses, joinType,
			                                    includeOnlyOIDFields, excludeShapeField,
			                                    out shapeFieldName,
			                                    out geometryType,
			                                    out primaryKeyField);

			if (StringUtils.IsNotEmpty(whereClause))
			{
				queryDef.WhereClause = StringUtils.IsNotEmpty(queryDef.WhereClause)
					                       ? $"({queryDef.WhereClause}) AND ({whereClause})"
					                       : whereClause;
			}

			IWorkspaceName workspaceName = GetWorkspaceName(relationshipClasses);

			IQueryName2 queryName = CreateQueryName(queryDef,
			                                        queryTableName,
			                                        primaryKeyField,
			                                        workspaceName,
			                                        shapeFieldName,
			                                        geometryType);

			ITable table = OpenTable(queryName);

			if (_msg.IsVerboseDebugEnabled)
			{
				bool isFeatureClass = table is IFeatureClass;

				_msg.Debug(isFeatureClass
					           ? "Creating query-based feature class"
					           : "Creating query-based table");

				using (_msg.IncrementIndentation())
				{
					LogQueryName(queryName);
				}
			}

			// NOTE: the resulting OID field may not be the one specified in IQueryName.PrimaryKey!
			// string oidFieldName = ((IObjectClass) table).OIDFieldName;
			//Assert.True(string.Equals(primaryKeyField, oidFieldName,
			//						  StringComparison.OrdinalIgnoreCase),
			//			$"Unexpected OID field name: {oidFieldName} (expected: {primaryKeyField})");

			return table;
		}

		private static void LogQueryName([NotNull] IQueryName2 queryName)
		{
			Assert.ArgumentNotNull(queryName, nameof(queryName));

			try
			{
				var datasetName = (IDatasetName) queryName;
				string queryTableName = datasetName.Name;
				IQueryDef queryDef = queryName.QueryDef;

				_msg.DebugFormat("SELECT {0} FROM {1} WHERE {2}",
				                 queryDef.SubFields,
				                 queryDef.Tables,
				                 queryDef.WhereClause);
				_msg.DebugFormat("Query table name: {0}", queryTableName);
				_msg.DebugFormat("Primary key: {0}", queryName.PrimaryKey);
				_msg.DebugFormat("Copy locally: {0}", queryName.CopyLocally);

				var queryDef2 = queryDef as IQueryDef2;
				if (queryDef2 != null)
				{
					_msg.DebugFormat("Prefix clause: {0}", queryDef2.PrefixClause);
					_msg.DebugFormat("Postfix clause: {0}", queryDef2.PostfixClause);
				}

				const bool replacePassword = true;
				_msg.DebugFormat("Workspace: {0}",
				                 datasetName.WorkspaceName == null
					                 ? "<not defined>"
					                 : WorkspaceUtils.GetConnectionString(datasetName.WorkspaceName,
						                 replacePassword));

				var featureClassName = queryName as IFeatureClassName;

				if (featureClassName != null)
				{
					_msg.DebugFormat("Shape field: {0}", featureClassName.ShapeFieldName);
					_msg.DebugFormat("Shape type: {0}", featureClassName.ShapeType);
				}
			}
			catch (Exception e)
			{
				_msg.WarnFormat("Error logging query name properties: {0}", e.Message);
			}
		}

		[NotNull]
		private static IQueryName2 CreateQueryName(
			[NotNull] IQueryDef queryDef,
			[NotNull] string queryTableName,
			[NotNull] string primaryKeyField,
			[NotNull] IWorkspaceName workspaceName,
			[CanBeNull] string shapeFieldName,
			esriGeometryType geometryType)
		{
			Assert.ArgumentNotNull(queryDef, nameof(queryDef));
			Assert.ArgumentNotNullOrEmpty(queryTableName, nameof(queryTableName));
			Assert.ArgumentNotNullOrEmpty(primaryKeyField, nameof(primaryKeyField));

			IQueryName2 result;
			if (StringUtils.IsNotEmpty(shapeFieldName))
			{
				result = new FeatureQueryNameClass();
				var featureClassName = (IFeatureClassName) result;

				featureClassName.ShapeFieldName = shapeFieldName;
				featureClassName.ShapeType = geometryType;
			}
			else
			{
				result = new TableQueryNameClass();
			}

			result.PrimaryKey = primaryKeyField;
			result.CopyLocally = false;
			result.QueryDef = queryDef;

			var datasetName = (IDatasetName) result;
			datasetName.Name = queryTableName;
			datasetName.WorkspaceName = workspaceName;

			return result;
		}

		[NotNull]
		private static ITable OpenTable([NotNull] IQueryName2 queryName)
		{
			Assert.ArgumentNotNull(queryName, nameof(queryName));

			try
			{
				return (ITable) ((IName) queryName).Open();
			}
			catch (Exception e)
			{
				_msg.DebugFormat("Error opening query-based table: {0}", e.Message);

				LogQueryName(queryName);

				throw;
			}
		}

		[NotNull]
		private static IQueryDef CreateQueryDef(
			[NotNull] IList<IRelationshipClass> relationshipClasses,
			JoinType joinType,
			bool includeOnlyOIDFields,
			bool excludeShapeField,
			[CanBeNull] out string shapeFieldName,
			out esriGeometryType geometryType,
			[NotNull] out string primaryKeyField)
		{
			IFeatureClass baseFeatureClass = excludeShapeField
				                                 ? null
				                                 : GetBaseFeatureClass(relationshipClasses,
					                                 joinType);

			if (baseFeatureClass != null)
			{
				shapeFieldName = DatasetUtils.QualifyFieldName(baseFeatureClass,
				                                               baseFeatureClass.ShapeFieldName);
				geometryType = baseFeatureClass.ShapeType;
			}
			else
			{
				shapeFieldName = null;
				geometryType = esriGeometryType.esriGeometryNull;
			}

			primaryKeyField = GetPrimaryKey(relationshipClasses, joinType, baseFeatureClass);

			return CreateQueryDef(relationshipClasses,
			                      joinType,
			                      primaryKeyField,
			                      baseFeatureClass,
			                      includeOnlyOIDFields);
		}

		[NotNull]
		private static string GetPrimaryKey(
			[NotNull] IList<IRelationshipClass> relationshipClasses,
			JoinType joinType,
			[CanBeNull] IFeatureClass baseFeatureClass)
		{
			Assert.ArgumentNotNull(relationshipClasses, nameof(relationshipClasses));
			Assert.ArgumentCondition(relationshipClasses.Count > 0,
			                         "at least one relationship class required");

			IRelationshipClass relationshipClass;
			switch (joinType)
			{
				case JoinType.InnerJoin:
					// TODO for > 1 relationship class: 
					// pick the smallest grain - or the one that involves the base feature class (if not null)
					relationshipClass = relationshipClasses[0];
					break;

				case JoinType.LeftJoin:
					relationshipClass = relationshipClasses[0];
					break;

				case JoinType.RightJoin:
					relationshipClass = relationshipClasses[relationshipClasses.Count - 1];
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(joinType));
			}

			return GetPrimaryKey(relationshipClass, joinType, baseFeatureClass);
		}

		[NotNull]
		private static string GetPrimaryKey(
			[NotNull] IRelationshipClass relationshipClass,
			JoinType joinType,
			[CanBeNull] IFeatureClass baseFeatureClass)
		{
			ITable table = GetPrimaryKeyTable(relationshipClass, joinType, baseFeatureClass);

			return DatasetUtils.QualifyFieldName(table, table.OIDFieldName);
		}

		[NotNull]
		private static ITable GetPrimaryKeyTable(
			[NotNull] IRelationshipClass relationshipClass,
			JoinType joinType,
			[CanBeNull] IFeatureClass baseFeatureClass)
		{
			bool usesTable = RelationshipClassUtils.UsesRelationshipTable(relationshipClass);

			// TODO consider relationship cardinality also - use side without duplication - at least where possible
			// BUT: always(?) avoid side which can have NULL values (due to outer join)
			// AND: coordinate with "exclusiveOIDFieldClass" in CreateQueryDef()

			switch (joinType)
			{
				case JoinType.InnerJoin:
					if (usesTable)
					{
						// use the RID from the relationship table
						// TODO this seems to make selection queries much slower
						// TODO however, if the destination OID field is used, only the first row with that OID can ever be selected
						return (ITable) relationshipClass;

						// TODO also use RID for outer joins? except if multipe feature classes are involved?
					}

					return baseFeatureClass != null &&
					       RelationshipClassUtils.InvolvesObjectClass(relationshipClass,
						       baseFeatureClass)
						       ? (ITable) baseFeatureClass
						       : (ITable) relationshipClass.DestinationClass;

				case JoinType.LeftJoin:
					// origin is left
					return (ITable) relationshipClass.OriginClass;

				case JoinType.RightJoin:
					// destination is right
					return (ITable) relationshipClass.DestinationClass;

				default:
					throw new ArgumentOutOfRangeException(nameof(joinType));
			}
		}

		//[NotNull]
		//private static string GetQueryTablePrimaryKey(
		//    [NotNull] IRelationshipClass relationshipClass)
		//{
		//    Assert.ArgumentNotNull(relationshipClass, "relationshipClass");

		//    if (relationshipClass.Cardinality !=
		//        esriRelCardinality.esriRelCardinalityManyToMany)
		//    {
		//        IObjectClass keyClass = relationshipClass.DestinationClass;

		//        return DatasetUtils.QualifyFieldName(keyClass, keyClass.OIDFieldName);
		//    }

		//    var bridgeTable = (ITable)relationshipClass;

		//    return DatasetUtils.QualifyFieldName(bridgeTable, bridgeTable.OIDFieldName);
		//}

		[NotNull]
		private static IWorkspaceName GetWorkspaceName(
			[NotNull] IList<IRelationshipClass> relationshipClasses)
		{
			IWorkspace workspace = GetWorkspace(relationshipClasses);
			var dataset = (IDataset) workspace;

			return (IWorkspaceName) dataset.FullName;
		}

		//private static bool CanCreateQueryFeatureClass(
		//    [NotNull] IList<IRelationshipClass> relationshipClasses,
		//    JoinType joinType,
		//    [NotNull] out string shapeFieldName,
		//    out esriGeometryType geometryType, 
		//    [CanBeNull] out IFeatureClass baseFeatureClass)
		//{
		//    Assert.ArgumentNotNull(relationshipClasses, "relationshipClasses");
		//    Assert.ArgumentCondition(relationshipClasses.Count > 0, "0 relationship classes");

		//    IFeatureClass featureClass = GetBaseFeatureClass(relationshipClasses, joinType);

		//    shapeFieldName = string.Empty;
		//    geometryType = esriGeometryType.esriGeometryNull;
		//    baseFeatureClass = null;

		//    if (featureClass == null)
		//    {
		//        return false;
		//    }

		//    switch (joinType)
		//    {
		//        case JoinType.LeftJoin:
		//            IRelationshipClass firstRelationshipClass = relationshipClasses[0];
		//            if (featureClass != firstRelationshipClass.OriginClass)
		//            {
		//                // can create query feature class for left join only if
		//                // the single base feature class is the origin class of the
		//                // first relationship class
		//                return false;
		//            }
		//            break;

		//        case JoinType.RightJoin:
		//            int lastIndex = relationshipClasses.Count - 1;
		//            IRelationshipClass lastRelationshipClass = relationshipClasses[lastIndex];

		//            if (featureClass != lastRelationshipClass.DestinationClass)
		//            {
		//                // can create query feature class for right join only if
		//                // the single base feature class is the destination class of the
		//                // last relationship class in the list
		//                return false;
		//            }
		//            break;
		//    }

		//    shapeFieldName = DatasetUtils.QualifyFieldName(featureClass,
		//                                                   featureClass.ShapeFieldName);
		//    geometryType = featureClass.ShapeType;
		//    baseFeatureClass = featureClass;

		//    return true;
		//}

		[CanBeNull]
		private static IFeatureClass GetBaseFeatureClass(
			[NotNull] IList<IRelationshipClass> relationshipClasses,
			JoinType joinType)
		{
			Assert.ArgumentNotNull(relationshipClasses, nameof(relationshipClasses));

			if (relationshipClasses.Count == 0)
			{
				return null;
			}

			switch (joinType)
			{
				case JoinType.InnerJoin:
					// prefer origin feature class

					// TODO: for n:m this is mostly arbitrary. How to override this choice?
					IFeatureClass destinationFeatureClass = null;
					foreach (IRelationshipClass relClass in relationshipClasses)
					{
						var originFeatureClass = relClass.OriginClass as IFeatureClass;
						if (originFeatureClass != null)
						{
							return originFeatureClass;
						}

						if (destinationFeatureClass == null)
						{
							destinationFeatureClass = relClass.DestinationClass as IFeatureClass;
						}
					}

					// no origin feature class found; use the first destination feature class
					return destinationFeatureClass;

				case JoinType.LeftJoin:
					// must be the origin class of the first relationship class
					// (origin is left, destination is right)
					IRelationshipClass firstRelationshipClass = relationshipClasses[0];
					return firstRelationshipClass.OriginClass as IFeatureClass;

				case JoinType.RightJoin:
					// must be the destination class of the last relationship class in the list
					// (origin is left, destination is right)
					int lastIndex = relationshipClasses.Count - 1;
					IRelationshipClass lastRelationshipClass = relationshipClasses[lastIndex];
					return lastRelationshipClass.DestinationClass as IFeatureClass;

				default:
					throw new ArgumentOutOfRangeException(nameof(joinType));
			}
		}

		[NotNull]
		private static IQueryDef CreateQueryDef(
			[NotNull] IList<IRelationshipClass> relationshipClasses,
			JoinType joinType,
			[NotNull] string primaryKeyField,
			[CanBeNull] IFeatureClass baseFeatureClass,
			bool includeOnlyOIDFields)
		{
			Assert.ArgumentNotNull(relationshipClasses, nameof(relationshipClasses));

			// TODO known issues
			// - Oracle:
			//   - for outer joins on n:m relationships between feature classes: 
			//     - Identify does not always return all features (note: picker also uses Identify internally)
			//     - When querying using ITable.Search/IFeatureClass.Search: no results are returned with some field lists
			//       (results are correct when only Shape field and OID field of base feature class is included)
			// - File geodatabase
			//   - query tables represent the last-saved state. They don't show the current editing state (last checked at 9.3.1)

			IWorkspace workspace = GetWorkspace(relationshipClasses);
			IQueryDef queryDef = ((IFeatureWorkspace) workspace).CreateQueryDef();

			IObjectClass exclusiveOIDFieldClass = null;
			if (baseFeatureClass != null &&
			    RelationshipClassUtils.GetFeatureClasses(relationshipClasses).Count() > 1)
			{
				// there is a valid base feature class, and more than one feature class
				// is involved in all relationship classes --> keep only the OID of the
				// base feature class in the subfields list
				exclusiveOIDFieldClass = baseFeatureClass;
			}

			const bool includeAllRelationshipTableFields = true;
			string subFieldsString = GetSubFieldsString(relationshipClasses,
			                                            baseFeatureClass,
			                                            primaryKeyField, includeOnlyOIDFields,
			                                            exclusiveOIDFieldClass,
			                                            includeAllRelationshipTableFields);

			string whereClause;
			string tablesExpression;

			if (UseJoinStatements(workspace, joinType))
			{
				tablesExpression = GetJoinedTablesExpression(relationshipClasses, joinType);
				whereClause = null;
			}
			else
			{
				string baseFeatureClassName = baseFeatureClass == null
					                              ? null
					                              : DatasetUtils.GetName(baseFeatureClass);

				tablesExpression = GetTablesExpression(relationshipClasses,
				                                       joinType,
				                                       baseFeatureClassName,
				                                       out whereClause);
			}

			queryDef.WhereClause = whereClause;
			queryDef.Tables = tablesExpression;
			queryDef.SubFields = subFieldsString;

			if (_msg.IsVerboseDebugEnabled)
			{
				if (StringUtils.IsNotEmpty(queryDef.WhereClause))
				{
					_msg.VerboseDebug(
						() =>
							$"Created Query Def: SELECT {queryDef.SubFields} FROM {queryDef.Tables} WHERE {queryDef.WhereClause}");
				}
				else
				{
					_msg.VerboseDebug(
						() =>
							$"Created Query Def: SELECT {queryDef.SubFields} FROM {queryDef.Tables}");
				}
			}

			return queryDef;
		}

		private static bool UseJoinStatements([NotNull] IWorkspace workspace, JoinType joinType)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			if (joinType == JoinType.InnerJoin)
			{
				// always use where clause-based inner join
				return false;
			}

			if (RuntimeUtils.Is10_0)
			{
				return false;
			}

			if (WorkspaceUtils.IsPersonalGeodatabase(workspace))
			{
				// TODO test now that TOP-4851 is fixed (add unit test, compare query shapes to base shapes)
				// return true;

				return false;
			}

			var connectionInfo = workspace as IDatabaseConnectionInfo2;
			if (connectionInfo == null)
			{
				// probably FGDB - use SQL join
				return true;
			}

			switch (connectionInfo.ConnectionDBMS)
			{
				case esriConnectionDBMS.esriDBMS_Oracle:
				case esriConnectionDBMS.esriDBMS_SQLServer:
					// where clause based outer join is supported, use it
					return false;

				default:
					// use SQL outer join statements, hope for the best 
					return true;
			}

			// TODO: test for 10.1 (fgdb, pgdb, arcsde)
		}

		[NotNull]
		private static string GetTablesExpression(
			[NotNull] IEnumerable<IRelationshipClass> relationshipClasses,
			JoinType joinType,
			[CanBeNull] string baseFeatureClassName,
			[CanBeNull] out string whereClause)
		{
			var tableNames = new List<string>();
			whereClause = null;

			foreach (IRelationshipClass relationshipClass in relationshipClasses)
			{
				var joinDef = new RelationshipClassJoinDefinition(relationshipClass, joinType);

				foreach (string tableName in joinDef.TableNames)
				{
					string tableNameUpper = tableName.ToUpper();

					if (! tableNames.Contains(tableNameUpper))
					{
						if (baseFeatureClassName != null &&
						    string.Equals(tableName, baseFeatureClassName,
						                  StringComparison.OrdinalIgnoreCase))
						{
							// make sure the base feature class is at first position in table list
							// TODO for ALL join types/cardinalities?
							tableNames.Insert(0, tableNameUpper);
						}
						else
						{
							tableNames.Add(tableNameUpper);
						}
					}
				}

				whereClause = whereClause == null
					              ? joinDef.GetJoinCondition()
					              : $"{whereClause} AND {joinDef.GetJoinCondition()}";
			}

			return StringUtils.Concatenate(tableNames, ",");
		}

		[NotNull]
		private static string GetJoinedTablesExpression(
			[NotNull] IList<IRelationshipClass> relationshipClasses,
			JoinType joinType)
		{
			var tablesExpression = new StringBuilder();

			var list = new List<IRelationshipClass>(relationshipClasses);
			if (joinType == JoinType.RightJoin)
			{
				list.Reverse();
			}

			foreach (IRelationshipClass relationshipClass in relationshipClasses)
			{
				var joinDef = new RelationshipClassJoinDefinition(relationshipClass, joinType);

				bool ignoreFirstTable = tablesExpression.Length > 0;
				tablesExpression.Append(joinDef.GetTableJoinStatement(ignoreFirstTable));
			}

			return tablesExpression.ToString();
		}

		[NotNull]
		private static string GetSubFieldsString(
			[NotNull] ICollection<IRelationshipClass> relationshipClasses,
			[CanBeNull] IFeatureClass baseFeatureClass,
			bool includeOnlyOIDFields = false,
			[CanBeNull] IObjectClass exclusiveOIDFieldClass = null)
		{
			const bool includeAllRelationshipTableFields = true;
			const string primaryKeyField = null;

			return GetSubFieldsString(relationshipClasses,
			                          baseFeatureClass,
			                          primaryKeyField, includeOnlyOIDFields,
			                          exclusiveOIDFieldClass, includeAllRelationshipTableFields);
		}

		[NotNull]
		private static string GetSubFieldsString(
			[NotNull] ICollection<IRelationshipClass> relationshipClasses,
			[CanBeNull] IFeatureClass baseFeatureClass,
			[CanBeNull] string primaryKeyField,
			bool includeOnlyOIDFields,
			[CanBeNull] IObjectClass exclusiveOIDFieldClass,
			bool includeAllRelationshipTableFields)
		{
			var subfields = new List<string>();

			// add non-geometry fields
			foreach (IObjectClass objectClass in
			         RelationshipClassUtils.GetObjectClasses(relationshipClasses))
			{
				bool excludeOIDField = exclusiveOIDFieldClass != null &&
				                       objectClass != exclusiveOIDFieldClass;

				AddFields(subfields, objectClass, includeOnlyOIDFields, excludeOIDField);
			}

			// In inner joins it's important that the RID comes after the OBJECTID of the
			// non-baseFeatureClass!
			foreach (IRelationshipClass relationshipClass in relationshipClasses)
			{
				if (RelationshipClassUtils.UsesRelationshipTable(relationshipClass))
				{
					if (includeAllRelationshipTableFields)
					{
						// append all fields
						AddFields(subfields, (IObjectClass) relationshipClass, false, false);
					}
					else
					{
						// append RID field
						var relTable = (ITable) relationshipClass;
						string qualifiedFieldName = DatasetUtils.QualifyFieldName(relTable,
							relTable
								.OIDFieldName);

						subfields.Add(qualifiedFieldName);
					}
				}
			}

			string lastFieldBeforeShape;
			if (baseFeatureClass != null)
			{
				lastFieldBeforeShape = DatasetUtils.QualifyFieldName(baseFeatureClass,
					baseFeatureClass.OIDFieldName);
			}
			else
			{
				lastFieldBeforeShape = primaryKeyField;
			}

			var sb = new StringBuilder();
			// for ArcGIS >= 10.8 ? : Primary key field must come before any other OID-Field
			if (lastFieldBeforeShape != null &&
			    lastFieldBeforeShape.Equals(primaryKeyField,
			                                StringComparison.CurrentCultureIgnoreCase))
			{
				AppendField(sb, lastFieldBeforeShape);
			}

			foreach (string subfield in subfields)
			{
				if (lastFieldBeforeShape != null &&
				    lastFieldBeforeShape.Equals(subfield,
				                                StringComparison.InvariantCultureIgnoreCase))
				{
					continue;
				}

				AppendField(sb, subfield);
			}

			// add primary key field *after* other non-shape fields and *before* shape field (is this still relevand in ArcGIS >= 10.8
			if (! StringUtils.IsNullOrEmptyOrBlank(lastFieldBeforeShape) &&
			    ! lastFieldBeforeShape.Equals(primaryKeyField,
			                                  StringComparison.CurrentCultureIgnoreCase))
			{
				AppendField(sb, lastFieldBeforeShape);
			}

			// add shape field
			if (baseFeatureClass != null)
			{
				AppendField(sb,
				            DatasetUtils.QualifyFieldName(baseFeatureClass,
				                                          baseFeatureClass.ShapeFieldName));
			}
			//if (shapeFieldName != null && StringUtils.IsNotEmpty(shapeFieldName))
			//{
			//	AppendField(sb, shapeFieldName);
			//}

			return sb.ToString();
		}

		[NotNull]
		private static IWorkspace GetWorkspace(
			[NotNull] IList<IRelationshipClass> relationshipClasses)
		{
			// TODO verify that all are from same workspace
			return ((IDataset) relationshipClasses[0]).Workspace;
		}

		[NotNull]
		private static string GenerateQueryTableName(
			[NotNull] IEnumerable<IRelationshipClass> relClasses)
		{
			Assert.ArgumentNotNull(relClasses, nameof(relClasses));

			var sb = new StringBuilder();

			foreach (IRelationshipClass relClass in relClasses)
			{
				if (sb.Length > 0)
				{
					sb.Append("_");
				}

				sb.Append(DatasetUtils.GetUnqualifiedName(relClass));
			}

			sb.Append("_JOIN");

			if (sb.Length > _maxNameLength)
			{
				sb.Remove(_maxNameLength, sb.Length - _maxNameLength);
			}

			return sb.ToString();
		}

		private static void AddFields([NotNull] ICollection<JoinedSubfield> fields,
		                              [NotNull] IObjectClass objectClass,
		                              bool includeOnlyOIDFields,
		                              bool excludeOIDField)
		{
			var featureClass = objectClass as IFeatureClass;

			foreach (IField field in DatasetUtils.GetFields(objectClass))
			{
				esriFieldType fieldType = field.Type;

				if (excludeOIDField && fieldType == esriFieldType.esriFieldTypeOID)
				{
					// ignore oid field if specified
					continue;
				}

				if (includeOnlyOIDFields && fieldType != esriFieldType.esriFieldTypeOID)
				{
					// ignore non-oid fields if specified
					continue;
				}

				if (fieldType == esriFieldType.esriFieldTypeGeometry)
				{
					// ignore shape field
					continue;
				}

				if (featureClass != null)
				{
					if (DatasetUtils.GetAreaField(featureClass) == field)
					{
						// ignore shape.area field
						continue;
					}

					if (DatasetUtils.GetLengthField(featureClass) == field)
					{
						// ignore shape.length field
						continue;
					}
				}

				string qualifiedFieldName = DatasetUtils.QualifyFieldName(objectClass, field.Name);

				fields.Add(new JoinedSubfield(qualifiedFieldName, fieldType));
			}
		}

		private static void AddFields([NotNull] List<string> fields,
		                              [NotNull] IObjectClass objectClass,
		                              bool includeOnlyOIDFields,
		                              bool excludeOIDField)
		{
			var featureClass = objectClass as IFeatureClass;

			foreach (IField field in DatasetUtils.GetFields(objectClass))
			{
				esriFieldType fieldType = field.Type;

				if (excludeOIDField && fieldType == esriFieldType.esriFieldTypeOID)
				{
					// ignore oid field if specified
					continue;
				}

				if (includeOnlyOIDFields && fieldType != esriFieldType.esriFieldTypeOID)
				{
					// ignore non-oid fields if specified
					continue;
				}

				if (fieldType == esriFieldType.esriFieldTypeGeometry)
				{
					// ignore shape field
					continue;
				}

				if (featureClass != null)
				{
					if (DatasetUtils.GetAreaField(featureClass) == field)
					{
						// ignore shape.area field
						continue;
					}

					if (DatasetUtils.GetLengthField(featureClass) == field)
					{
						// ignore shape.length field
						continue;
					}
				}

				string qualifiedFieldName = DatasetUtils.QualifyFieldName(objectClass, field.Name);

				fields.Add(qualifiedFieldName);
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

		private class JoinedSubfieldComparer : IComparer<JoinedSubfield>
		{
			[CanBeNull] private readonly string _primaryKeyField;
			[CanBeNull] private readonly string _baseFeatureClassOIDField;

			public JoinedSubfieldComparer([CanBeNull] IFeatureClass baseFeatureClass,
			                              [CanBeNull] string primaryKeyField)
			{
				_primaryKeyField = primaryKeyField;
				if (baseFeatureClass != null)
				{
					_baseFeatureClassOIDField = DatasetUtils.QualifyFieldName(
						baseFeatureClass,
						baseFeatureClass.OIDFieldName);
				}
			}

			public int Compare(JoinedSubfield x, JoinedSubfield y)
			{
				if (x == null && y == null)
				{
					return 0;
				}

				if (x == null)
				{
					return -1;
				}

				if (y == null)
				{
					return 1;
				}

				if (! x.IsOIDField && ! y.IsOIDField)
				{
					// non-OID fields: order alphabetically
					return string.CompareOrdinal(x.FieldName, y.FieldName);
				}

				if (! y.IsOIDField)
				{
					// x is an OID field, y is non-oid --> put x after y
					return 1;
				}

				if (! x.IsOIDField)
				{
					// x is non-oid, y is an OID field --> put x before y
					return -1;
				}

				// both are OID fields

				// primary key first
				if (string.Equals(x.FieldName, _primaryKeyField))
				{
					return -1;
				}

				if (string.Equals(y.FieldName, _primaryKeyField))
				{
					return 1;
				}

				// then the base feature class OID field
				if (string.Equals(x.FieldName, _baseFeatureClassOIDField))
				{
					return -1;
				}

				if (string.Equals(y.FieldName, _baseFeatureClassOIDField))
				{
					return 1;
				}

				return string.CompareOrdinal(x.FieldName, y.FieldName);
			}
		}

		private class JoinedSubfield
		{
			public JoinedSubfield([NotNull] string fieldName, esriFieldType fieldType)
			{
				FieldName = fieldName;
				FieldType = fieldType;
			}

			[NotNull]
			public string FieldName { get; }

			private esriFieldType FieldType { get; }

			public bool IsOIDField => FieldType == esriFieldType.esriFieldTypeOID;

			public override string ToString()
			{
				return $"{nameof(FieldName)}: {FieldName}, {nameof(FieldType)}: {FieldType}";
			}
		}
	}
}
