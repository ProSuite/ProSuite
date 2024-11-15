using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AO.Geodatabase
{
	public static class GdbQueryUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// enumerate rows of objectClass that satisfy a given where clause and for which
		/// at least one related feature intersects a given geometry.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <param name="intersectedGeometry">The intersected geometry.</param>
		/// <param name="whereClause">The where clause.</param>
		/// <param name="relClass">The relationship class, for which origin or destination class must be a feature class
		///  and the other must be equal to objectClass</param>
		/// <param name="postfixClause">The postfix clause (group by ..., order by ...).</param>
		/// <returns>enumeration of RowProxy</returns>
		[NotNull]
		public static IEnumerable<FieldMappingRowProxy> GetRowProxys(
			[NotNull] IObjectClass objectClass,
			[CanBeNull] IGeometry intersectedGeometry,
			[CanBeNull] string whereClause,
			[NotNull] IRelationshipClass relClass,
			[CanBeNull] string postfixClause)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));
			Assert.ArgumentNotNull(relClass, nameof(relClass));

			var relClasses = new List<IRelationshipClass>(1) { relClass };

			return GetRowProxys(objectClass,
			                    intersectedGeometry, whereClause,
			                    relClasses, postfixClause);
		}

		/// <summary>
		/// enumerate rows of objectClass that satisfy a given where clause and for which
		/// at leaste one related feature intersects a given geometry.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <param name="intersectedGeometry">The intersected geometry.</param>
		/// <param name="relClass">The relationship class, for which origin or destination class must be a feature class
		///  and the other must be equal to objectClass</param>
		/// <returns>enumeration of RowProxy</returns>
		[NotNull]
		public static IEnumerable<FieldMappingRowProxy> GetRowProxys(
			[NotNull] IObjectClass objectClass,
			[CanBeNull] IGeometry intersectedGeometry,
			[NotNull] IRelationshipClass relClass)
		{
			return GetRowProxys(objectClass, intersectedGeometry, string.Empty, relClass);
		}

		/// <summary>
		/// enumerate rows of objectClass that satisfy a given where clause and for which
		/// at leaste one related feature intersects a given geometry.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <param name="intersectedGeometry">The intersected geometry.</param>
		/// <param name="whereClause">The where clause.</param>
		/// <param name="relClass">The relationship class, for which origin or destination class must be a feature class
		///  and the other must be equal to objectClass</param>
		/// <returns>enumeration of RowProxy</returns>
		[NotNull]
		public static IEnumerable<FieldMappingRowProxy> GetRowProxys(
			[NotNull] IObjectClass objectClass,
			[CanBeNull] IGeometry intersectedGeometry,
			[CanBeNull] string whereClause,
			[NotNull] IRelationshipClass relClass)
		{
			return GetRowProxys(objectClass, intersectedGeometry,
			                    whereClause, relClass,
			                    string.Empty);
		}

		/// <summary>
		/// enumerate rows of objectClass that satisfy a given where clause and for which 
		/// at leaste one related feature intersects a given geometry.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <param name="intersectedGeometry">The intersected geometry.</param>
		/// <param name="whereClause">The where clause.</param>
		/// <param name="relClasses">list of connected relationship classes, where origin or destination class must be a feature class
		///  and the other must be equal to objectClass</param>
		/// <returns>enumeration of RowProxy</returns>
		[NotNull]
		public static IEnumerable<FieldMappingRowProxy> GetRowProxys(
			[NotNull] IObjectClass objectClass,
			[CanBeNull] IGeometry intersectedGeometry,
			[CanBeNull] string whereClause,
			[NotNull] IList<IRelationshipClass> relClasses)
		{
			return GetRowProxys(objectClass, intersectedGeometry,
			                    whereClause, relClasses,
			                    string.Empty);
		}

		/// <summary>
		/// enumerate rows of objectClass that satisfy a given where clause and for which 
		/// at leaste one related feature intersects a given geometry.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <param name="intersectedGeometry">The intersected geometry.</param>
		/// <param name="whereClause">The where clause.</param>
		/// <param name="relClasses">list of connected relationship classes, where origin or destination class must be a feature class
		///  and the other must be equal to objectClass</param>
		/// <param name="postfixClause">The postfix clause (group by ..., order by ...).</param>
		/// <returns>enumeration of RowProxy</returns>
		[NotNull]
		public static IEnumerable<FieldMappingRowProxy> GetRowProxys(
			[NotNull] IObjectClass objectClass,
			[CanBeNull] IGeometry intersectedGeometry,
			[CanBeNull] string whereClause,
			[NotNull] IList<IRelationshipClass> relClasses,
			[CanBeNull] string postfixClause)
		{
			return GetRowProxys(objectClass, intersectedGeometry,
			                    whereClause, relClasses, postfixClause, string.Empty);
		}

		/// <summary>
		/// enumerate rows of objectClass that satisfy a given where clause and for which
		/// at leaste one related feature intersects a given geometry.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <param name="intersectedGeometry">The intersected geometry.</param>
		/// <param name="whereClause">The where clause.</param>
		/// <param name="relClasses">list of connected relationship classes, where origin or destination class must be a feature class
		/// and the other must be equal to objectClass</param>
		/// <param name="postfixClause">The postfix clause (group by ..., order by ...).</param>
		/// <param name="subfields">The subfields.</param>
		/// <returns>
		/// enumeration of RowProxy
		/// </returns>
		[NotNull]
		public static IEnumerable<FieldMappingRowProxy> GetRowProxys(
			[NotNull] IObjectClass objectClass,
			[CanBeNull] IGeometry intersectedGeometry,
			[CanBeNull] string whereClause,
			[NotNull] IList<IRelationshipClass> relClasses,
			[CanBeNull] string postfixClause,
			[CanBeNull] string subfields)
		{
			const bool includeOnlyOIDFields = false;
			return GetRowProxys(objectClass, intersectedGeometry,
			                    whereClause, relClasses, postfixClause, subfields,
			                    includeOnlyOIDFields);
		}

		/// <summary>
		/// enumerate rows of objectClass that satisfy a given where clause and for which
		/// at leaste one related feature intersects a given geometry.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <param name="intersectedGeometry">The intersected geometry.</param>
		/// <param name="whereClause">The where clause.</param>
		/// <param name="relClasses">list of connected relationship classes, where origin or destination class must be a feature class
		/// and the other must be equal to objectClass</param>
		/// <param name="postfixClause">The postfix clause (group by ..., order by ...).</param>
		/// <param name="subfields">The subfields.</param>
		/// <param name="includeOnlyOIDFields">if set to <c>true</c> the underlying query is constructed such that only 
		/// the OID fields of the involved tables are included (in addition to the shape field if applicable).</param>
		/// <returns>
		/// enumeration of RowProxy
		/// </returns>
		[NotNull]
		public static IEnumerable<FieldMappingRowProxy> GetRowProxys(
			[NotNull] IObjectClass objectClass,
			[CanBeNull] IGeometry intersectedGeometry,
			[CanBeNull] string whereClause,
			[NotNull] IList<IRelationshipClass> relClasses,
			[CanBeNull] string postfixClause,
			[CanBeNull] string subfields,
			bool includeOnlyOIDFields)
		{
			const bool recycle = false;
			return GetRowProxys(objectClass, intersectedGeometry,
			                    whereClause, relClasses,
			                    postfixClause, subfields,
			                    includeOnlyOIDFields, recycle);
		}

		public static HashSet<long> GetRelatedOids(IObjectClass objectClass,
		                                           IGeometry intersectedGeometry,
		                                           IList<IRelationshipClass> relClasses)
		{
			IRelationshipClass geomRelClass = relClasses[relClasses.Count - 1];
			IObjectClass startClass;
			if (geomRelClass.OriginClass is IFeatureClass fc)
			{
				startClass = geomRelClass.DestinationClass;
			}
			else
			{
				fc = (IFeatureClass) geomRelClass.DestinationClass;
				startClass = geomRelClass.OriginClass;
			}

			GdbFeatureClass gdbFc =
				TableJoinUtils.CreateJoinedGdbFeatureClass(geomRelClass, fc, "geometryRel");

			HashSet<long> oids =
				GetRelatedOids(gdbFc, new AoFeatureClassFilter(intersectedGeometry));

			for (int iRel = relClasses.Count - 2; iRel >= 0; iRel--)
			{
				ITable startTbl = (ITable) startClass;
				IRelationshipClass relClass = relClasses[iRel];
				GdbTable gdbTbl =
					TableJoinUtils.CreateJoinedGdbTable(relClass, startTbl, $"rel_{iRel}");

				HashSet<long> allOids = new HashSet<long>();
				IWorkspace ws = ((IDataset) relClass).Workspace;
				int oidFieldIndex = startTbl.FindField(startTbl.OIDFieldName);
				foreach (var whereClause in EnumWhereClauses(
					         oids, null, startTbl.Fields.get_Field(oidFieldIndex), ws))
				{
					foreach (long oid in GetRelatedOids(
						         gdbTbl, new AoTableFilter { WhereClause = whereClause }))
					{
						allOids.Add(oid);
					}
				}

				startClass = relClass.OriginClass == startClass
					             ? relClass.DestinationClass
					             : relClass.OriginClass;
				oids = allOids;
			}

			if (startClass != objectClass)
			{
				throw new InvalidOperationException("relClasses do not start with objectClass");
			}

			return oids;
		}

		private static HashSet<long> GetRelatedOids(GdbTable gdbFc, ITableFilter filter)
		{
			JoinedDataset joined = Assert.NotNull((JoinedDataset) gdbFc.BackingDataset);
			IDictionary<string, IList<IReadOnlyRow>> otherRowsByFeatureKey =
				joined.GetOtherRowsByFeatureKey(filter);

			HashSet<long> oids = new HashSet<long>();
			foreach (IList<IReadOnlyRow> otherRows in otherRowsByFeatureKey.Values)
			{
				foreach (IReadOnlyRow readOnlyRow in otherRows)
				{
					oids.Add(readOnlyRow.OID);
				}
			}

			return oids;
		}

		/// <summary>
		/// enumerate rows of objectClass that satisfy a given where clause and for which
		/// at least one related feature intersects a given geometry.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <param name="intersectedGeometry">The intersected geometry.</param>
		/// <param name="whereClause">The where clause.</param>
		/// <param name="relClasses">list of connected relationship classes, where origin or destination class must be a feature class
		/// and the other must be equal to objectClass</param>
		/// <param name="postfixClause">The postfix clause (group by ..., order by ...).</param>
		/// <param name="subfields">The subfields.</param>
		/// <param name="includeOnlyOIDFields">if set to <c>true</c> the underlying query is constructed such that only 
		/// the OID fields of the involved tables are included (in addition to the shape field if applicable).</param>
		/// <param name="recycle">if set to <c>true</c> a recycling cursor is used. In this case the returned 
		/// row proxy can't be stored in a collection since the underlying feature is always the same instance.</param>
		/// <returns>
		/// enumeration of RowProxy
		/// </returns>
		[NotNull]
		public static IEnumerable<FieldMappingRowProxy> GetRowProxys(
			[NotNull] IObjectClass objectClass,
			[CanBeNull] IGeometry intersectedGeometry,
			[CanBeNull] string whereClause,
			[NotNull] IList<IRelationshipClass> relClasses,
			[CanBeNull] string postfixClause,
			[CanBeNull] string subfields, bool includeOnlyOIDFields,
			bool recycle)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));
			Assert.ArgumentNotNull(relClasses, nameof(relClasses));

			ITable queryTable = TableJoinUtils.CreateQueryTable(relClasses,
			                                                    JoinType.InnerJoin,
			                                                    includeOnlyOIDFields);

			const bool includeReadOnlyFields = true;
			const bool searchJoinedFields = true;
			IDictionary<int, int> fieldMapping =
				GdbObjectUtils.CreateMatchingIndexMatrix(queryTable, objectClass,
				                                         includeReadOnlyFields,
				                                         searchJoinedFields);

			int targetOidField = objectClass.Fields.FindField(objectClass.OIDFieldName);
			int sourceOidField = fieldMapping[targetOidField];

			IQueryFilter filter;
			if (intersectedGeometry != null)
			{
				var joinedFeatureClass = (IFeatureClass) queryTable;

				filter = new SpatialFilterClass
				         {
					         Geometry = intersectedGeometry,
					         GeometryField = joinedFeatureClass.ShapeFieldName,
					         SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects
				         };
			}
			else
			{
				filter = new QueryFilterClass();
			}

			if (! string.IsNullOrEmpty(subfields))
			{
				filter.SubFields = subfields;
			}

			if (! string.IsNullOrEmpty(whereClause))
			{
				filter.WhereClause = whereClause;
			}

			if (! string.IsNullOrEmpty(postfixClause))
			{
				((IQueryFilterDefinition) filter).PostfixClause = postfixClause;
			}

			foreach (IRow queryRow in GetRows(queryTable, filter, recycle))
			{
				long oid;
				try
				{
					oid = Assert.NotNull(GdbObjectUtils.ReadRowOidValue(queryRow, sourceOidField))
					            .Value;
				}
				catch
				{
					try
					{
						_msg.DebugFormat("row: {0}", GdbObjectUtils.ToString(queryRow));
						_msg.DebugFormat("source oid field index: {0}", sourceOidField);
						_msg.DebugFormat("target oid field index: {0}", targetOidField);
						_msg.DebugFormat("subfields: {0}", subfields);

						_msg.Debug("relationship classes:");
						foreach (IRelationshipClass relClass in relClasses)
						{
							_msg.DebugFormat("- {0}", relClass == null
								                          ? "<null>"
								                          : DatasetUtils.GetName(relClass));
						}

						_msg.Debug("fields:");
						foreach (IField field in DatasetUtils.GetFields(queryTable))
						{
							_msg.DebugFormat("- {0}", field.Name);
						}
					}
					catch (Exception e)
					{
						_msg.Debug("logging error", e);
					}

					throw;
				}

				yield return new FieldMappingRowProxy(queryRow, fieldMapping,
				                                      (ITable) objectClass, oid);
			}
		}

		/// <summary>
		/// enumerate rows of objectClass, where related features intersect a given geometry
		/// </summary>
		/// <param name="objectClass"></param>
		/// <param name="intersectedGeometry"></param>
		/// <param name="relClasses">list of connected relationship classes, where origin or destination class must be a feature class
		///  and the other must be equal to objectClass</param>
		/// <returns>enumeration of RowProxy</returns>
		[NotNull]
		public static IEnumerable<FieldMappingRowProxy> GetRowProxys(
			[NotNull] IObjectClass objectClass,
			[CanBeNull] IGeometry intersectedGeometry,
			[NotNull] IList<IRelationshipClass> relClasses)
		{
			return GetRowProxys(objectClass, intersectedGeometry, string.Empty, relClasses);
		}

		/// <summary>
		/// Creates query filter with a list of subfields
		/// </summary>
		/// <param name="subFields">The field names to be used as subfields.</param>
		/// <returns></returns>
		[NotNull]
		public static IQueryFilter CreateQueryFilter(params string[] subFields)
		{
			var result = new QueryFilterClass();

			SetSubFields(result, subFields);

			return result;
		}

		/// <summary>
		/// Sets the subfields for a query filter
		/// </summary>
		/// <param name="queryFilter">The query filter.</param>
		/// <param name="fieldNames">The field names to be used as subfields.</param>
		public static void SetSubFields([NotNull] IQueryFilter queryFilter,
		                                params string[] fieldNames)
		{
			SetSubFields(queryFilter, (IEnumerable<string>) fieldNames);
		}

		/// <summary>
		/// Sets the subfields for a query filter
		/// </summary>
		/// <param name="queryFilter">The query filter.</param>
		/// <param name="fieldNames">The field names to be used as subfields.</param>
		public static void SetSubFields([NotNull] IQueryFilter queryFilter,
		                                [NotNull] IEnumerable<string> fieldNames)
		{
			Assert.ArgumentNotNull(queryFilter, nameof(queryFilter));
			Assert.ArgumentNotNull(fieldNames, nameof(fieldNames));

			// The SubFields can't be set to null/empty and then added with AddField() 
			// -> SubFields reverts to "*" when setting to null/empty
			// instead, the first field must be assigned to the SubFields property. The other fields can 
			// then be added using AddField() (which makes sure that fields are included only once)

			bool first = true;
			foreach (string fieldName in fieldNames)
			{
				if (first)
				{
					queryFilter.SubFields = fieldName;
					first = false;
				}
				else
				{
					queryFilter.AddField(fieldName);
				}
			}
		}

		/// <summary>
		/// Appends the specified field name to <paramref name="currentSubFields"/> if there are other fields
		/// already. In case it is null, empty or "*" the specified field name is returned (as single
		/// sub-field). The <paramref name="fieldName"/> can also be a comma-separated list of fields.
		/// </summary>
		public static string AppendToFieldList([CanBeNull] string currentSubFields,
		                                       [NotNull] string fieldName)
		{
			if (string.IsNullOrEmpty(currentSubFields) || currentSubFields == "*")
			{
				return fieldName;
			}

			List<string> currentFieldNames = StringUtils.SplitAndTrim(currentSubFields, ',');

			if (currentFieldNames.Any(f => f.Equals(fieldName, StringComparison.OrdinalIgnoreCase)))
			{
				return currentSubFields;
			}

			return $"{currentSubFields}, {fieldName}";
		}

		[NotNull]
		public static IFeatureClassFilter CreateFeatureClassFilter(
			[NotNull] IGeometry searchGeometry,
			esriSpatialRelEnum spatialRel = esriSpatialRelEnum.esriSpatialRelIntersects)
		{
			return new AoFeatureClassFilter(searchGeometry, spatialRel);
		}

		[CanBeNull]
		public static ITableFilter ToTableFilter([CanBeNull] IQueryFilter queryFilter)
		{
			if (queryFilter == null)
			{
				return null;
			}

			ITableFilter tableFilter;

			if (queryFilter is ISpatialFilter spatialFilter)
			{
				tableFilter = CreateFeatureClassFilter(
					spatialFilter.Geometry, spatialFilter.SpatialRel);
			}
			else
			{
				tableFilter = new AoTableFilter();
			}

			tableFilter.SubFields = queryFilter.SubFields;
			tableFilter.WhereClause = queryFilter.WhereClause;
			tableFilter.PostfixClause = ((IQueryFilterDefinition) queryFilter).PostfixClause;
			return tableFilter;
		}

		[NotNull]
		public static IQueryFilter CreateSpatialFilter(
			[NotNull] IFeatureClass featureClass,
			[NotNull] IGeometry searchGeometry,
			esriSpatialRelEnum spatialRel = esriSpatialRelEnum.esriSpatialRelIntersects)
		{
			const bool filterOwnsGeometry = false;
			return CreateSpatialFilter(featureClass, searchGeometry,
			                           spatialRel, filterOwnsGeometry);
		}

		[NotNull]
		public static IQueryFilter CreateSpatialFilter([NotNull] IFeatureClass featureClass,
		                                               [NotNull] IGeometry searchGeometry,
		                                               esriSpatialRelEnum spatialRel,
		                                               bool filterOwnsGeometry)
		{
			const ISpatialReference outputSpatialReference = null;
			return CreateSpatialFilter(featureClass, searchGeometry,
			                           spatialRel, filterOwnsGeometry,
			                           outputSpatialReference);
		}

		/// <summary>
		/// Creates a spatial filter from the provided parameters. The search geometry can be high- or low-level
		/// and it will be ensured that its extent is not below the resolution.
		/// </summary>
		/// <param name="featureClass">The feature class used for extra validation. It is required
		/// if a non-null output spatial reference is specified.</param>
		/// <param name="searchGeometry"></param>
		/// <param name="spatialRel"></param>
		/// <param name="filterOwnsGeometry"></param>
		/// <param name="outputSpatialReference"></param>
		/// <param name="searchOrder"></param>
		/// <returns></returns>
		[NotNull]
		public static ISpatialFilter CreateSpatialFilter(
			[CanBeNull] IFeatureClass featureClass,
			[NotNull] IGeometry searchGeometry,
			esriSpatialRelEnum spatialRel,
			bool filterOwnsGeometry,
			[CanBeNull] ISpatialReference outputSpatialReference,
			esriSearchOrder searchOrder = esriSearchOrder.esriSearchOrderSpatial)
		{
			Assert.ArgumentNotNull(searchGeometry, nameof(searchGeometry));

			if (outputSpatialReference != null)
			{
				Assert.ArgumentCondition(featureClass != null,
				                         "If the output spatial reference is specified, the feature class is required.");
			}

			ISpatialFilter spatialFilter = new SpatialFilterClass();

			// Extra validation if feature class was provided:
			IGeometry validGeometry = searchGeometry;

			if (featureClass != null)
			{
				// make sure the geometry remains non-empty even after simplify:
				double xyResolution = GeometryUtils.GetXyResolution(featureClass);

				if (! IsValidFilterGeometry(searchGeometry, xyResolution, out validGeometry,
				                            out string message))
				{
					_msg.DebugFormat("Filter geometry is not valid: {0}. Geometry: {1}",
					                 message, GeometryUtils.ToString(searchGeometry));

					if (validGeometry != null)
					{
						_msg.DebugFormat(
							"A valid filter geometry could be derived. Using the valid geometry: {0}",
							GeometryUtils.ToString(validGeometry));
					}
					else
					{
						_msg.DebugFormat("Invalid spatial filter geometry provided: {0}",
						                 GeometryUtils.ToString(searchGeometry));
						throw new InvalidOperationException(
							$"Invalid geometry for spatial filter: {message}");
					}
				}

				if (validGeometry != searchGeometry)
				{
					filterOwnsGeometry = true;
				}
				else
				{
					validGeometry = searchGeometry;
				}
			}

			spatialFilter.set_GeometryEx(validGeometry, filterOwnsGeometry);
			spatialFilter.SpatialRel = spatialRel;

			if (featureClass != null)
			{
				spatialFilter.GeometryField = featureClass.ShapeFieldName;
				spatialFilter.set_OutputSpatialReference(featureClass.ShapeFieldName,
				                                         outputSpatialReference);
			}

			// be on the safe side and specify spatial explicitly (from 9.3.1 spatial seems to be the default)
			spatialFilter.SearchOrder = searchOrder;

			return spatialFilter;
		}

		/// <summary>
		/// Determines whether the provided filter geometry is valid or not.
		/// The validGeometry is the inputGeometry or an adapted copy if the inputGeometry is not
		/// a valid filter geometry because 
		/// - it is a low-level geometry
		/// - it is a multipatch geometry (valid at 10.x)
		/// - it is so small that its envelope collapses when snapped to the resolution (valid at 10.?)
		/// Not all invalid conditions can be corrected. For
		/// example input geometries that are already invalid in their current spatial 
		/// reference (such as polygons with only one point) will not be corrected.
		/// Non-simple geometries are valid.
		/// Empty geometries are valid at 10.0 but not any more at 10.2
		/// </summary>
		/// <param name="inputGeometry"></param>
		/// <param name="searchClassXyResolution"></param>
		/// <param name="validGeometry"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		public static bool IsValidFilterGeometry([NotNull] IGeometry inputGeometry,
		                                         double searchClassXyResolution,
		                                         [CanBeNull] out IGeometry validGeometry,
		                                         out string message)
		{
			Assert.ArgumentNotNull(inputGeometry, nameof(inputGeometry));

			message = string.Empty;

			// geometry must implement IRelationalOperator for query filter
			var relationalOperator = inputGeometry as IRelationalOperator;

			IGeometry result;
			if (relationalOperator == null)
			{
				message =
					$"Search geometry with type {inputGeometry.GeometryType} not implement IRelationalOperator";

				// NOTE: Multipatch implements IRelationalOperator since 10.0
				if (inputGeometry is IMultiPatch multiPatch)
				{
					IPolygon searchPoly = GeometryFactory.CreatePolygon(multiPatch);

					result = searchPoly;
				}
				else
				{
					result = GeometryUtils.GetHighLevelGeometry(inputGeometry);
				}
			}
			else
			{
				result = inputGeometry;
			}

			// empty geometries are valid up to 10.0 (but possibly not intended)
			if (result.IsEmpty)
			{
				message = "Search geometry is empty";
				validGeometry = null;
				return false;
			}

			if (result.SpatialReference == null)
			{
				// Generally ok, but probably good to know in case of an error:
				_msg.DebugFormat("The spatial reference of the provided filter geometry is null!");
			}

			// handle the most typical case of 'The number of points is less than required' (e.g. large zoom in custom layers):
			// enlarge sub-resolution polygons/polylines that would result in errors
			// this is fast and should be safe as IRelationalOperator even uses the tolerance
			if (IsBelowMinimumDimension(result, searchClassXyResolution))
			{
				// IFeatureClass.Search would otherwise result in exception 'The number of points is less than required for feature'
				message = "Search geometry is below minmum dimensions and had to be enlarged.";
				validGeometry = GetEnlargedGeometry(result, searchClassXyResolution);
				return false;
			}

			if (GeometryUtils.IsZAware(result) && ! ((IZAware) result).ZSimple)
			{
				message = "Search geometry is Z-aware but has NaN Z-values.";
				// TOP-5234: Non-Z-simple geometries result in no features being found in SDE!
				// TODO: Test non-z-aware feature class
				validGeometry = GeometryFactory.Clone(result);
				GeometryUtils.MakeNonZAware(validGeometry);
				return false;
			}
			// NOTE: there are other reasons why the filter geometry might be invalid: 
			//		 - not enough points (but large enough) -> might use GeometryUtils.IsGeometryValid after snap to spatial ref
			//		 ...
			// -> Not all conditions can be handled here or it would get inefficient. Better log the filter properties (including geometry) on exception (GetRows)

			validGeometry = result;
			return true;
		}

		private static bool IsBelowMinimumDimension([NotNull] IGeometry geometry,
		                                            double minimumDimension)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentCondition(! geometry.IsEmpty, "geometry is empty.");

			IEnvelope envelope = geometry.Envelope;

			minimumDimension -=
				MathUtils.GetDoubleSignificanceEpsilon(envelope.XMax, envelope.YMax);

			if ((geometry is IPolygon || geometry is IEnvelope) &&
			    (envelope.Height < minimumDimension ||
			     envelope.Width < minimumDimension))
			{
				return true;
			}

			if (geometry is IPolyline &&
			    envelope.Height < minimumDimension &&
			    envelope.Width < minimumDimension)
			{
				return true;
			}

			return false;
		}

		[NotNull]
		private static IGeometry GetEnlargedGeometry([NotNull] IGeometry smallGeometry,
		                                             double minimumSize)
		{
			Assert.ArgumentNotNull(smallGeometry, nameof(smallGeometry));
			Assert.ArgumentCondition(! smallGeometry.IsEmpty, "smallGeometry is empty.");

			if (smallGeometry is IPolygon || smallGeometry is IEnvelope)
			{
				IEnvelope replacementEnv = GeometryFactory.Clone(smallGeometry.Envelope);
				EnforceMinimumSize(replacementEnv, minimumSize);

				return replacementEnv;
			}

			if (smallGeometry is IPolyline)
			{
				var polyline = (IPolyline) GeometryFactory.Clone(smallGeometry);
				if (polyline.Length > 0)
				{
					GeometryUtils.ExtendCurve(polyline, esriSegmentExtension.esriExtendTangents,
					                          minimumSize - polyline.Length);
				}

				return polyline;
			}

			throw new ArgumentOutOfRangeException(
				string.Format("Unsupported geometry type to enlarge: {0}",
				              smallGeometry.GeometryType));
		}

		// duplicate from conflictRow
		private static void EnforceMinimumSize([NotNull] IEnvelope envelope,
		                                       double minimumSize)
		{
			double dx = 0;
			double dy = 0;
			if (envelope.Width < minimumSize)
			{
				dx = (minimumSize - envelope.Width) / 2;
			}

			if (envelope.Height < minimumSize)
			{
				dy = (minimumSize - envelope.Height) / 2;
			}

			if (dx > 0 || dy > 0)
			{
				envelope.Expand(dx, dy, false);
			}
		}

		/// <summary>
		/// enumerate rows of objectClass, where related features intersect a given geometry
		/// </summary>
		/// <param name="objectClass"></param>
		/// <param name="intersectedGeometry"></param>
		/// <param name="relClasses">list of connected relationship classes, where origin or destination class must be a feature class
		///  and the other must be equal to objectClass</param>
		/// <param name="postfixClause">postfix clause (group by ..., order by...)</param>
		/// <returns>enumeration of RowProxy</returns>
		[NotNull]
		public static IEnumerable<FieldMappingRowProxy> GetRowProxys(
			[NotNull] IObjectClass objectClass,
			[CanBeNull] IGeometry intersectedGeometry,
			[NotNull] IList<IRelationshipClass> relClasses,
			[CanBeNull] string postfixClause)
		{
			return GetRowProxys(objectClass, intersectedGeometry,
			                    string.Empty, relClasses,
			                    postfixClause);
		}

		/// <summary>
		/// Get all objects related to a given object
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="relationshipClasses">relationship classes used to find objects</param>
		/// <returns></returns>
		[NotNull]
		public static IList<IObject> GetRelatedObjectList(
			[NotNull] IObject obj,
			[NotNull] IEnumerable<IRelationshipClass> relationshipClasses)
		{
			return new List<IObject>(GetRelatedObjects(obj, relationshipClasses));
		}

		[NotNull]
		public static IEnumerable<IObject> GetRelatedObjects(
			[NotNull] IObject obj,
			[NotNull] IEnumerable<IRelationshipClass> relationshipClasses)
		{
			Assert.ArgumentNotNull(obj, nameof(obj));
			Assert.ArgumentNotNull(relationshipClasses, nameof(relationshipClasses));

			foreach (IRelationshipClass relationshipClass in relationshipClasses)
			{
				ISet pairs = relationshipClass.GetObjectsRelatedToObject(obj);

				for (var related = (IObject) pairs.Next();
				     related != null;
				     related = (IObject) pairs.Next())
				{
					yield return related;
				}
			}
		}

		[CanBeNull]
		public static IObject GetObject([NotNull] IObjectClass objectClass,
		                                long objectId,
		                                [CanBeNull] string oidFieldName = null)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			// TODO delegate to GetObjects() with id list --> call ITable.GetRows()
			// NOTE: the GetFeature() based implementations cause massive memory growth
			// with large geometries since features remain in cache until the end of 
			// the edit operation

			//IFeatureClass featureClass = objectClass as IFeatureClass;
			//return featureClass != null
			//        ? GetFeature(featureClass, objectId)
			//        : (IObject) GetRow((ITable) objectClass, objectId);

			if (oidFieldName == null)
			{
				oidFieldName = objectClass.OIDFieldName;
			}

			string whereClause = string.Format("{0}={1}", oidFieldName, objectId);

			IList<IObject> list = FindList<IObject>(objectClass, whereClause);

			switch (list.Count)
			{
				case 0:
					return null;

				case 1:
					return list[0];

				default:
					throw new InvalidDataException(
						string.Format("More than one row with ID {0} found in table {1}",
						              objectId, DatasetUtils.GetName(objectClass)));
			}
		}

		[CanBeNull]
		public static IRow GetRow([NotNull] ITable table, long rowId)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			var objectClass = (IObjectClass) table;
			return GetObject(objectClass, rowId);
		}

		[CanBeNull]
		public static IFeature GetFeature([NotNull] IFeatureClass featureClass,
		                                  long featureId)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			return (IFeature) GetObject(featureClass, featureId);

			// NOTE: the GetFeature() based implementations cause massive memory growth
			// with large geometries since features remain in cache until the end of 
			// the edit operation

			//try
			//{
			//    return featureClass.GetFeature(featureId);
			//}
			//catch (COMException e)
			//{
			//    if (e.ErrorCode == (int) fdoError.FDO_E_FEATURE_NOT_FOUND)
			//    {
			//        return null;
			//    }
			//    throw;
			//}
		}

		[CanBeNull]
		public static IReadOnlyFeature GetFeature([NotNull] IReadOnlyFeatureClass featureClass,
		                                          long featureId)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			return (IReadOnlyFeature) featureClass.GetRow(featureId);

			// NOTE: the GetFeature() based implementations cause massive memory growth
			// with large geometries since features remain in cache until the end of 
			// the edit operation

			//try
			//{
			//    return featureClass.GetFeature(featureId);
			//}
			//catch (COMException e)
			//{
			//    if (e.ErrorCode == (int) fdoError.FDO_E_FEATURE_NOT_FOUND)
			//    {
			//        return null;
			//    }
			//    throw;
			//}
		}

		/// <summary>
		/// Gets the features for a given collection of object ids
		/// </summary>
		/// <param name="featureClass">The feature class.</param>C
		/// <param name="objectIds">The object ids.</param>
		/// <param name="recycling">if set to <c>true</c> the same feature instance is recycled in the result.</param>
		/// <returns></returns>
		public static IEnumerable<IFeature> GetFeatures(
			[NotNull] IFeatureClass featureClass,
			[NotNull] IEnumerable<long> objectIds,
			bool recycling)
		{
			// Redirect to more generic method. For legacy implementation see GdbQueryUtils10
			return GetFeaturesInList(featureClass, featureClass.OIDFieldName, objectIds, recycling);
		}

		[NotNull]
		public static IEnumerable<IObject> GetObjectsByIds(
			[NotNull] IObjectClass objectClass,
			[NotNull] IEnumerable<long> objectIDs,
			bool recycle)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));
			Assert.ArgumentNotNull(objectIDs, nameof(objectIDs));

			var table = (ITable) objectClass;

			foreach (IRow row in GetRowsByObjectIds(table, objectIDs, recycle))
			{
				yield return (IObject) row;
			}
		}

		/// <summary>
		/// Gets the features for a given collection of object ids
		/// </summary>
		/// <param name="table">The table.</param>C
		/// <param name="objectIds">The object ids.</param>
		/// <param name="recycling">if set to <c>true</c> the same feature instance is recycled in the result.</param>
		/// <returns></returns>
		/// <remarks>This apparently may return <i>deleted</i> features, if called in the same 
		/// edit operation that deleted the features.</remarks>
		public static IEnumerable<IRow> GetRowsByObjectIds(
			[NotNull] ITable table,
			[NotNull] IEnumerable<long> objectIds,
			bool recycling)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(objectIds, nameof(objectIds));

			// Redirect to more generic method. For legacy implementation see GdbQueryUtils10
			return GetRowsInList<IRow>(table, table.OIDFieldName, objectIds, recycling);
		}

		/// <summary>
		/// Enumerates all rows of a table 
		/// </summary>
		/// <param name="table"></param>
		/// <param name="recycle">use recycling for row instance</param>
		/// <returns></returns>
		public static IEnumerable<IRow> GetRows([NotNull] ITable table,
		                                        bool recycle)
		{
			return GetRows(table, null, recycle);
		}

		/// <summary>
		/// Enumerates all rows of a table corresponding to a query filter  
		/// </summary>
		/// <param name="table"></param>
		/// <param name="queryFilter"></param>
		/// <param name="recycle">use recycling for row instance</param>
		/// <returns></returns>
		public static IEnumerable<IRow> GetRows([NotNull] ITable table,
		                                        [CanBeNull] IQueryFilter queryFilter,
		                                        bool recycle)
		{
			return GetRows<IRow>(table, queryFilter, recycle);
		}

		/// <summary>
		/// Enumerates all objects of an object class corresponding to a query filter  
		/// </summary>
		/// <param name="objectClass"></param>
		/// <param name="recycle">use recycling for row instance</param>
		/// <returns></returns>
		public static IEnumerable<IObject> GetObjects([NotNull] IObjectClass objectClass,
		                                              bool recycle)
		{
			return GetObjects(objectClass, null, recycle);
		}

		/// <summary>
		/// Enumerates all objects of an object class corresponding to a query filter  
		/// </summary>
		/// <param name="objectClass"></param>
		/// <param name="queryFilter"></param>
		/// <param name="recycle">use recycling for row instance</param>
		/// <returns></returns>
		public static IEnumerable<IObject> GetObjects([NotNull] IObjectClass objectClass,
		                                              [CanBeNull] IQueryFilter queryFilter,
		                                              bool recycle)
		{
			return GetRows<IObject>((ITable) objectClass, queryFilter, recycle);
		}

		/// <summary>
		/// Enumerates all features of a feature class corresponding to a query filter  
		/// </summary>
		/// <param name="featureClass"></param>
		/// <param name="recycle">use recycling for row instance</param>
		/// <returns></returns>
		public static IEnumerable<IFeature> GetFeatures(
			[NotNull] IFeatureClass featureClass,
			bool recycle)
		{
			return GetFeatures(featureClass, (IQueryFilter) null, recycle);
		}

		/// <summary>
		/// Enumerates all features of a feature class corresponding to a query filter  
		/// </summary>
		/// <param name="featureClass"></param>
		/// <param name="queryFilter"></param>
		/// <param name="recycle">use recycling for row instance</param>
		/// <returns></returns>
		public static IEnumerable<IFeature> GetFeatures(
			[NotNull] IFeatureClass featureClass,
			[CanBeNull] IQueryFilter queryFilter,
			bool recycle)
		{
			return GetRows<IFeature>((ITable) featureClass, queryFilter, recycle);
		}

		[NotNull]
		public static IEnumerable<IFeature> GetFeaturesInList(
			[NotNull] IFeatureClass featureClass,
			[NotNull] string fieldName,
			[NotNull] IEnumerable valueList,
			bool recycle,
			[CanBeNull] IQueryFilter filter = null)
		{
			return GetRowsInList<IFeature>((ITable) featureClass, fieldName, valueList,
			                               recycle, filter);
		}

		[NotNull]
		public static IEnumerable<IRow> GetRowsInList(
			[NotNull] ITable table,
			[NotNull] string fieldName,
			[NotNull] IEnumerable valueList,
			bool recycle,
			[CanBeNull] IQueryFilter queryFilter = null)
		{
			return GetRowsInList<IRow>(table, fieldName, valueList, recycle, queryFilter);
		}

		[NotNull]
		public static IEnumerable<T> GetRowsInList<T>(
			[NotNull] ITable table,
			[NotNull] string fieldName,
			[NotNull] IEnumerable valueList,
			bool recycle,
			[CanBeNull] IQueryFilter queryFilter = null)
			where T : IRow
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));
			Assert.ArgumentNotNull(valueList, nameof(valueList));

			foreach (IRow row in GetRowsInList(DatasetUtils.GetWorkspace(table),
			                                   DatasetUtils.GetField(table, fieldName), valueList,
			                                   (q) => GetRows(table, q, recycle), queryFilter))
			{
				yield return (T) row;
			}
		}

		private static IEnumerable<T> GetRowsInList<T>(
			[NotNull] IWorkspace workspace,
			[NotNull] IField field,
			[NotNull] IEnumerable valueList,
			[NotNull] Func<IQueryFilter, IEnumerable<T>> getRows,
			[CanBeNull] IQueryFilter queryFilter = null)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNull(valueList, nameof(valueList));
			Assert.ArgumentNotNull(getRows, nameof(getRows));

			if (queryFilter == null)
			{
				queryFilter = new QueryFilterClass();
			}

			string origWhereClause = queryFilter.WhereClause;
			try
			{
				foreach (string whereClause in EnumWhereClauses(
					         valueList, origWhereClause, field, workspace))
				{
					queryFilter.WhereClause = whereClause;

					foreach (T row in getRows(queryFilter))
					{
						yield return row;
					}
				}
			}
			finally
			{
				queryFilter.WhereClause = origWhereClause;
			}
		}

		private static IEnumerable<string> EnumWhereClauses(
			IEnumerable valueList, string origWhereClause, IField field,
			IWorkspace workspace)
		{
			// TODO: assert that the values match the field type

			esriFieldType fieldType = field.Type;

			GetWhereClauseLimits(workspace, out int maxWhereClauseLength, out int maxValueCount);

			StringBuilder sb = null;
			var valueCount = 0;
			foreach (object value in valueList)
			{
				if (sb == null ||
				    sb.Length >= maxWhereClauseLength ||
				    valueCount >= maxValueCount)
				{
					if (sb != null)
					{
						// NOTE: the last value plus the closing bracket may exceed the maximum length
						yield return $"{sb})";
					}

					sb = new StringBuilder();
					if (! string.IsNullOrEmpty(origWhereClause))
					{
						sb.AppendFormat("({0}) AND ", origWhereClause);
					}

					sb.AppendFormat("{0} ", field.Name);
					sb.Append("IN (");
					valueCount = 0;
				}
				else
				{
					sb.Append(",");
				}

				sb.Append(GdbSqlUtils.GetLiteral(value, fieldType, workspace));
				valueCount++;
			}

			if (sb != null)
			{
				yield return $"{sb})";
			}
		}

		private static void GetWhereClauseLimits([NotNull] IWorkspace workspace,
		                                         out int maximumWhereClauseLength,
		                                         out int maximumInValueCount)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			// safe defaults:
			maximumWhereClauseLength = 2000;
			maximumInValueCount = 1000;

			switch (workspace.Type)
			{
				case esriWorkspaceType.esriLocalDatabaseWorkspace:
					if (WorkspaceUtils.IsFileGeodatabase(workspace))
					{
						if (RuntimeUtils.Is10_0)
						{
							// sql behavior is different, not yet verified 
							// -> use safe defaults
							return;
						}

						// these values have been found to work
						maximumWhereClauseLength = 200000;
						maximumInValueCount = 10000;
						return;
					}

					if (WorkspaceUtils.IsPersonalGeodatabase(workspace))
					{
						maximumWhereClauseLength = 60000; // limit is approximately 64000 
						maximumInValueCount = 1000; // not sure
						return;
					}

					// Unknown; use safe defaults
					return;

				case esriWorkspaceType.esriRemoteDatabaseWorkspace:
					var connectionInfo = (IDatabaseConnectionInfo2) workspace;
					switch (connectionInfo.ConnectionDBMS)
					{
						case esriConnectionDBMS.esriDBMS_Oracle:
							maximumWhereClauseLength = 60000; // not sure where the limit is
							maximumInValueCount = 1000; // this limit is documented
							return;

						case esriConnectionDBMS.esriDBMS_SQLServer:
							maximumWhereClauseLength = 60000; // actual limit is higher
							maximumInValueCount = 1000; // limit may be higher
							return;

						case esriConnectionDBMS.esriDBMS_PostgreSQL:
							// apparently unlimited
							maximumWhereClauseLength = 200000;
							maximumInValueCount = 10000;
							return;

						case esriConnectionDBMS.esriDBMS_Unknown:
						case esriConnectionDBMS.esriDBMS_Informix:
						case esriConnectionDBMS.esriDBMS_DB2:
							// Unknown; use safe defaults
							return;

						default:
							// this includes values added after 10.0:
							// esriConnectionDBMS.esriDBMS_Netezza
							// esriConnectionDBMS.esriDBMS_Teradata
							// esriConnectionDBMS.esriDBMS_SQLite

							// Unknown; use safe defaults
							return;
					}

				case esriWorkspaceType.esriFileSystemWorkspace:
					// Unknown; use safe defaults
					return;

				default:
					// Unknown; use safe defaults
					return;
			}
		}

		[NotNull]
		public static IEnumerable<IRow> GetRowsNotInList([NotNull] ITable table,
		                                                 [NotNull] IQueryFilter filter,
		                                                 bool recycle,
		                                                 [NotNull] string fieldName,
		                                                 [NotNull] IEnumerable valueList)
		{
			var list = new List<object>();
			foreach (object value in valueList)
			{
				if (value == null || value is DBNull)
				{
					// ignore null values in the list
					// TODO: revise, maybe rows with null values should only be excluded if null is in the exclusion list
					// NOTE: sql does not return rows with NULL if 'not in (list)' is used
				}
				else
				{
					// only add non-null values (otherwise comparer throws exception)
					list.Add(value);
				}
			}

			list.Sort();

			string origFields = filter.SubFields;
			try
			{
				int fieldIndex = table.FindField(fieldName);
				filter.AddField(fieldName);

				_msg.DebugFormat("GetRowsInList WhereClause: {0}", filter.WhereClause);

				foreach (IRow row in GetRows(table, filter, recycle))
				{
					object value = row.Value[fieldIndex];
					bool valueIsNull = value == null || value is DBNull;

					if (valueIsNull)
					{
						// always exclude rows with null values for the exclusion field
					}
					else if (list.BinarySearch(value) < 0)
					{
						yield return row;
					}
				}
			}
			finally
			{
				filter.SubFields = origFields;
			}
		}

		[NotNull]
		public static IList<IRow> FindList([NotNull] IObjectClass objectClass,
		                                   [NotNull] string whereClause)
		{
			const string defaultSubfields = "*";
			return FindList(objectClass, whereClause, defaultSubfields);
		}

		[NotNull]
		public static IList<IRow> FindList([NotNull] IObjectClass objectClass,
		                                   [NotNull] string whereClause,
		                                   [NotNull] string subfields)
		{
			return new List<IRow>(Find(objectClass, whereClause, false,
			                           delegate(IRow row, out IRow result)
			                           {
				                           result = row;
				                           return true;
			                           }, subfields));
		}

		[NotNull]
		public static IList<IFeature> FindList([NotNull] IFeatureClass objectClass,
		                                       [NotNull] string whereClause)
		{
			const string defaultSubfields = "*";
			return FindList<IFeature>(objectClass, whereClause, defaultSubfields);
		}

		public static IList<T> FindList<T>([NotNull] IObjectClass objectClass,
		                                   [NotNull] string whereClause)
			where T : class
		{
			const string defaultSubfields = "*";
			return FindList<T>(objectClass, whereClause, defaultSubfields);
		}

		[NotNull]
		public static IList<T> FindList<T>([NotNull] IObjectClass objectClass,
		                                   [NotNull] string whereClause,
		                                   [NotNull] string subFields) where T : class
		{
			const bool recycling = false;
			return new List<T>(Find(
				                   objectClass, whereClause, recycling,
				                   delegate(IRow row, out T result)
				                   {
					                   result = row as T;
					                   return true;
				                   }, subFields));
		}

		[NotNull]
		public static IList<T> FindList<T>([NotNull] IObjectClass objectClass,
		                                   [NotNull] string whereClause,
		                                   [NotNull] ReadRow<T> readRow) where T : class
		{
			return new List<T>(Find(objectClass, whereClause, false, readRow));
		}

		[NotNull]
		public static IEnumerable<T> Find<T>([NotNull] IObjectClass objectClass,
		                                     [NotNull] string whereClause,
		                                     bool recycling,
		                                     [NotNull] ReadRow<T> readRow) where T : class
		{
			const string defaultSubfields = "*";
			return Find(objectClass, whereClause, recycling, readRow, defaultSubfields);
		}

		/// <summary>
		/// Returns a list of objects of a given type, based on a search in a given
		/// object class.
		/// </summary>
		/// <param name="objectClass">The object class to search in.</param>
		/// <param name="whereClause">The where clause.</param>
		/// <param name="recycling">if set to <c>true</c> a recycling cursor is used.</param>
		/// <param name="readRow">A delegate of type <see cref="ReadRow&lt;T&gt;"/> to 
		/// a method that processes each row and 
		/// 1. returns a result object (may be the row itself, a field value of it, or some
		/// transformation of it, and
		/// 2. indicates if the search should continue </param>
		/// <param name="subfields">The subfields to consider in the query.</param>
		/// <returns></returns>
		[NotNull]
		public static IEnumerable<T> Find<T>([NotNull] IObjectClass objectClass,
		                                     [NotNull] string whereClause,
		                                     bool recycling,
		                                     [NotNull] ReadRow<T> readRow,
		                                     [NotNull] string subfields)
			where T : class
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			IQueryFilter filter = new QueryFilterClass
			                      {
				                      WhereClause = whereClause,
				                      SubFields = subfields
			                      };

			if (objectClass is IFeatureClass featureClass)
			{
				IFeatureCursor cursor = OpenCursor(featureClass, recycling, filter);

				try
				{
					IFeature feature;
					while ((feature = cursor.NextFeature()) != null)
					{
						T result;
						bool cancel = ! readRow(feature, out result);

						if (result != null)
						{
							yield return result;
						}

						if (cancel)
						{
							yield break;
						}
					}
				}
				finally
				{
					ComUtils.ReleaseComObject(cursor);
				}
			}
			else
			{
				ICursor cursor = OpenCursor(objectClass, recycling, filter);

				try
				{
					IRow row;
					while ((row = cursor.NextRow()) != null)
					{
						T result;
						bool cancel = ! readRow(row, out result);

						if (result != null)
						{
							yield return result;
						}

						if (cancel)
						{
							yield break;
						}
					}
				}
				finally
				{
					ComUtils.ReleaseComObject(cursor);
				}
			}
		}

		public static long Count([NotNull] IReadOnlyTable table,
		                         [CanBeNull] string whereClause = null)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			ITableFilter filter = new AoTableFilter
			                      {
				                      WhereClause = whereClause,
				                      SubFields = table.OIDFieldName
			                      };

			return table.RowCount(filter);
		}

		public static long Count([NotNull] IObjectClass objectClass,
		                         [CanBeNull] string whereClause = null)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			IQueryFilter filter = new QueryFilterClass
			                      {
				                      WhereClause = whereClause,
				                      SubFields = objectClass.OIDFieldName
			                      };

			return Count(objectClass, filter);
		}

		public static long Count([NotNull] ITable table,
		                         [NotNull] IQueryFilter filter)
		{
			if (table is IFeatureClass featureClass)
			{
				return featureClass.FeatureCount(filter);
			}

			return table.RowCount(filter);
		}

		public static long Count([NotNull] IObjectClass objectClass,
		                         [NotNull] IQueryFilter filter)
		{
			if (objectClass is IFeatureClass featureClass)
			{
				return featureClass.FeatureCount(filter);
			}

			var table = (ITable) objectClass;

			return table.RowCount(filter);
		}

		public static long Count([NotNull] IFeatureClass featureClass,
		                         [CanBeNull] IGeometry searchGeometry)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			if (searchGeometry == null)
			{
				return Count(featureClass);
			}

			IQueryFilter filter = CreateSpatialFilter(featureClass, searchGeometry);
			filter.SubFields = featureClass.OIDFieldName;

			return featureClass.FeatureCount(filter);
		}

		/// <summary>
		/// Enumerates all rows of table corresponding to filter  
		/// </summary>
		/// <param name="table"></param>
		/// <param name="filter"></param>
		/// <param name="recycle">use recycling for row instance</param>
		/// <returns></returns>
		private static IEnumerable<T> GetRows<T>([NotNull] ITable table,
		                                         [CanBeNull] IQueryFilter filter,
		                                         bool recycle) where T : IRow
		{
			Assert.ArgumentNotNull(table, nameof(table));

			Stopwatch watch = null;
			if (_msg.IsVerboseDebugEnabled)
			{
				watch = _msg.DebugStartTiming();
			}

			long rowCount = 0;

			if (table is IFeatureClass featureClass)
			{
				IFeatureCursor cursor = OpenCursor(featureClass, recycle, filter);
				try
				{
					for (IFeature feature = cursor.NextFeature();
					     feature != null;
					     feature = cursor.NextFeature())
					{
						rowCount++;
						yield return (T) feature;
					}
				}
				finally
				{
					ComUtils.ReleaseComObject(cursor);
				}
			}
			else
			{
				ICursor cursor = OpenCursor(table, recycle, filter);
				try
				{
					for (IRow row = cursor.NextRow();
					     row != null;
					     row = cursor.NextRow())
					{
						rowCount++;
						yield return (T) row;
					}
				}
				finally
				{
					// In case TOP-2231 ever happens again, re-introduce this:
					// releasing only when there have been rows seems to reduce the 
					// frequency of https://issuetracker02.eggits.net/browse/TOP-2231
					// (COM object that has been separated from its underlying RCW cannot be used)
					// in subsequent edit operations.
					//if (rowCount > 0 && Marshal.IsComObject(cursor))
					//{
					//	ComUtils.ReleaseComObject(cursor);
					//}

					ComUtils.ReleaseComObject(cursor);
				}
			}

			if (watch != null)
			{
				_msg.DebugStopTiming(watch, "GetRows() SELECT {0} FROM {1} {2}",
				                     filter == null
					                     ? "*"
					                     : filter.SubFields,
				                     DatasetUtils.GetName(table),
				                     filter == null
					                     ? string.Empty
					                     : "WHERE " + filter.WhereClause);

				var spatialFilter = filter as ISpatialFilter;

				if (spatialFilter?.Geometry != null)
				{
					using (_msg.IncrementIndentation())
					{
						_msg.DebugFormat("Geometry: {0}", spatialFilter.Geometry.GeometryType);
						_msg.DebugFormat("SpatialRel: {0}", spatialFilter.SpatialRel);
						_msg.DebugFormat("SpatialRelDescription: {0}",
						                 spatialFilter.SpatialRelDescription);
						_msg.DebugFormat("SearchOrder: {0}", spatialFilter.SearchOrder);
					}
				}

				_msg.DebugFormat("Result row count: {0}", rowCount);
			}
		}

		[NotNull]
		public static IFeatureCursor OpenCursor([NotNull] IFeatureClass featureClass,
		                                        bool recycling,
		                                        [CanBeNull] IQueryFilter filter = null)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			// In some situations for (unregistered) PostGIS tables the Search() method changes the SubFields
			// to the full list of attributes in escaped quotations marks, which makes subsequent queries
			// using the same filter queries fail.
			string subFieldsBefore = filter?.SubFields;

			try
			{
				return featureClass.Search(filter, recycling);
			}
			catch (Exception e)
			{
				LogQueryParameters((ITable) featureClass, recycling, filter);
				throw new InvalidOperationException(
					string.Format(
						"Error opening feature cursor for {0}: {1} (see log for detailed query parameters)",
						DatasetUtils.GetName(featureClass), e.Message), e);
			}
			finally
			{
				if (subFieldsBefore != filter?.SubFields)
				{
					filter.SubFields = subFieldsBefore;
				}
			}
		}

		[NotNull]
		public static ICursor OpenCursor([NotNull] IObjectClass objectClass,
		                                 bool recycling,
		                                 [CanBeNull] IQueryFilter filter = null)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			return OpenCursor((ITable) objectClass, recycling, filter);
		}

		[NotNull]
		public static ICursor OpenCursor([NotNull] ITable table,
		                                 bool recycling,
		                                 [CanBeNull] IQueryFilter filter = null)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			// In some situations for (unregistered) PostGIS tables the Search() method changes the SubFields
			// to the full list of attributes in escaped quotations marks, which makes subsequent queries
			// using the same filter queries fail.
			string subFieldsBefore = filter?.SubFields;
			try
			{
				if (_msg.IsVerboseDebugEnabled)
				{
					LogQueryParameters(table, recycling, filter);
				}

				return table.Search(filter, recycling);
			}
			catch (Exception e)
			{
				LogQueryParameters(table, recycling, filter);
				throw new InvalidOperationException(
					string.Format(
						"Error opening cursor for {0}: {1} (see log for detailed query parameters)",
						DatasetUtils.GetName(table), e.Message), e);
			}
			finally
			{
				if (subFieldsBefore != filter?.SubFields)
				{
					filter.SubFields = subFieldsBefore;
				}
			}
		}

		private static void LogQueryParameters([NotNull] ITable table,
		                                       bool recycling,
		                                       [CanBeNull] IQueryFilter filter)
		{
			try
			{
				_msg.DebugFormat("Dataset: {0}", DatasetUtils.GetName(table));
				const bool replacePassword = true;
				_msg.DebugFormat("Workspace: {0}",
				                 WorkspaceUtils.GetConnectionString(
					                 DatasetUtils.GetWorkspace(table), replacePassword));
				_msg.DebugFormat("Recycling cursor requested: {0}", recycling);

				if (filter == null)
				{
					_msg.Debug("No query filter specified");
				}
				else
				{
					_msg.Debug("Query filter properties:");
					using (_msg.IncrementIndentation())
					{
						LogFilterProperties(filter);
					}
				}
			}
			catch (Exception e)
			{
				_msg.Debug("Error logging query parameters", e);
			}
		}

		public static void LogFilterProperties([NotNull] IQueryFilter filter)
		{
			_msg.DebugFormat("Subfields: {0}", filter.SubFields);
			_msg.DebugFormat("WhereClause: {0}", filter.WhereClause);

			var spatialFilter = filter as ISpatialFilter;

			if (filter is IQueryFilter2 filter2)
			{
				_msg.DebugFormat("SpatialResolution: {0}", filter2.SpatialResolution);
			}

			if (filter is IQueryFilterDefinition filterDefinition)
			{
				_msg.DebugFormat("PostfixClause: {0}", filterDefinition.PostfixClause);

				IFilterDefs filterDefs = filterDefinition.FilterDefs;
				if (filterDefs != null)
				{
					int count = filterDefs.Count;
					_msg.DebugFormat("FilterDefs: {0}", count);
					for (var i = 0; i < count; i++)
					{
						IFilterDef filterDef = filterDefs.Element[i];

						// just ToString(); would have to downcast to get properties
						_msg.DebugFormat("FilterDef {0}: {1}", i, filterDef);
					}
				}
			}

			if (filter is IQueryFilterDefinition2 filterDefinition2)
			{
				_msg.DebugFormat("PrefixClause: {0}", filterDefinition2.PrefixClause);
			}

			if (spatialFilter != null)
			{
				_msg.DebugFormat("GeometryField: {0}", spatialFilter.GeometryField);
				_msg.DebugFormat("SpatialRel: {0}", spatialFilter.SpatialRel);
				_msg.DebugFormat("SpatialRelDescription: {0}",
				                 spatialFilter.SpatialRelDescription);
				_msg.DebugFormat("SearchOrder: {0}", spatialFilter.SearchOrder);

				if (spatialFilter.Geometry != null)
				{
					_msg.Debug("Filter geometry:");
					_msg.Debug(GeometryUtils.ToString(spatialFilter.Geometry));
				}
				else
				{
					_msg.Debug("Filter geometry: not defined");
				}

				if (! string.IsNullOrEmpty(spatialFilter.GeometryField))
				{
					ISpatialReference spatialReference =
						spatialFilter.OutputSpatialReference[spatialFilter.GeometryField];

					if (spatialReference == null)
					{
						_msg.Debug("Output spatial reference: not defined");
					}
					else
					{
						_msg.Debug("Output spatial reference:");
						_msg.Debug(SpatialReferenceUtils.ToString(spatialReference));
					}
				}
			}
		}

		[NotNull]
		private static IGeoDatabaseBridge GetGeodatabaseBridge()
		{
			return ComUtils.CreateObject<IGeoDatabaseBridge>(
				"esriGeoDatabase.GeoDatabaseHelper");
		}
	}
}
