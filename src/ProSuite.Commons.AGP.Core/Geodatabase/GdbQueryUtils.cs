using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AGP.Core.Geodatabase
{
	public static class GdbQueryUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static QueryFilter CreateFilter([NotNull] IReadOnlyList<long> oids)
		{
			Assert.ArgumentNotNull(oids, nameof(oids));

			return new QueryFilter { ObjectIDs = oids };
		}

		[NotNull]
		public static SpatialQueryFilter CreateSpatialFilter(
			[NotNull] Geometry filterGeometry,
			SpatialRelationship spatialRelationship = SpatialRelationship.Intersects,
			SearchOrder searchOrder = SearchOrder.Spatial)
		{
			Assert.ArgumentNotNull(filterGeometry, nameof(filterGeometry));

			return new SpatialQueryFilter
			       {
				       FilterGeometry = filterGeometry,
				       SpatialRelationship = spatialRelationship,
				       SearchOrder = searchOrder
			       };
		}

		public static Feature GetFeature([NotNull] FeatureClass featureClass, long oid)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			return GetRow<Feature>(featureClass, oid);
		}

		public static IEnumerable<Feature> GetFeatures(
			[NotNull] Table featureClass,
			[NotNull] IEnumerable<long> objectIds,
			[CanBeNull] SpatialReference outputSpatialReference,
			bool recycle,
			CancellationToken cancellationToken = default)
		{
			IReadOnlyList<long> oidList = objectIds.ToList();

			try
			{
				QueryFilter filter = CreateFilter(oidList);

				filter.OutputSpatialReference = outputSpatialReference;

				return GetFeatures(featureClass, filter, recycle, cancellationToken);
			}
			catch (Exception e)
			{
				// TODO: Specifically catch FDO_E_SE_LOG_NOEXIST
				_msg.Debug("Error getting rows by OID-list", e);

				const int maxRowCount = 1000;
				return GetRowsByObjectIdsBatched<Feature>(
					featureClass, oidList, outputSpatialReference, recycle, maxRowCount,
					cancellationToken);
			}
		}

		public static IEnumerable<Feature> GetFeatures(
			[NotNull] Table featureClass,
			[CanBeNull] QueryFilter filter,
			bool recycling,
			CancellationToken cancellationToken = default)
		{
			Stopwatch watch = null;
			if (_msg.IsVerboseDebugEnabled)
			{
				watch = Stopwatch.StartNew();
			}

			using RowCursor cursor = OpenCursor(featureClass, filter, recycling);

			long rowCount = 0;

			while (cursor.MoveNext())
			{
				if (cancellationToken.IsCancellationRequested)
				{
					yield break;
				}

				var feature = (Feature) cursor.Current;

				rowCount++;
				yield return feature;
			}

			if (watch != null)
			{
				_msg.DebugStopTiming(watch, "GetRows() SELECT {0} FROM {1} {2}",
				                     filter == null ? "*" : filter.SubFields,
				                     featureClass.GetName(),
				                     filter == null ? string.Empty : "WHERE " + filter.WhereClause);

				var spatialFilter = filter as SpatialQueryFilter;

				if (spatialFilter?.FilterGeometry != null)
				{
					using (_msg.IncrementIndentation())
					{
						_msg.DebugFormat("Geometry: {0}",
						                 spatialFilter.FilterGeometry.GeometryType);
						_msg.DebugFormat("SpatialRel: {0}", spatialFilter.SpatialRelationship);

						// TODO: Remove once 3.0 is de-supported
						// TODO: Define compilation symbol / variable in project for Pro versions
#if DEBUG
						_msg.DebugFormat("SpatialRelDescription: {0}",
						                 spatialFilter.SpatialRelationshipDescription);
#endif

						_msg.DebugFormat("SearchOrder: {0}", spatialFilter.SearchOrder);
					}
				}

				_msg.DebugFormat("Result row count: {0}", rowCount);
			}
		}

		public static Row GetRow([NotNull] Table table, long oid)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			return GetRow<Row>(table, oid);
		}

		public static T GetRow<T>([NotNull] Table table, long oid)
			where T : Row
		{
			Assert.ArgumentNotNull(table, nameof(table));

			QueryFilter filter = CreateFilter(new[] { oid });

			using (RowCursor cursor = OpenCursor(table, filter, false))
			{
				if (! cursor.MoveNext())
				{
					return null;
				}

				var result = (T) cursor.Current;

				// todo daro: remove later when GetRow is used intensively throughout the solution
				Assert.False(cursor.MoveNext(), "more than one row found");

				return result;
			}
		}

		public static IEnumerable<T> GetRows<T>(
			[NotNull] Table table,
			[CanBeNull] QueryFilter filter = null,
			bool recycle = true,
			CancellationToken cancellationToken = default)
			where T : Row
		{
			Assert.ArgumentNotNull(table, nameof(table));

			using (RowCursor cursor = OpenCursor(table, filter, recycle))
			{
				while (cursor.MoveNext())
				{
					if (cancellationToken.IsCancellationRequested)
					{
						yield break;
					}

					yield return (T) cursor.Current;
				}
			}
		}

		/// <summary>
		/// Returns the rows from ITable.GetRows method called in batches with the specified maximum number of rows.
		/// </summary>
		/// <param name="table"></param>
		/// <param name="objectIds"></param>
		/// <param name="outputSpatialReference"></param>
		/// <param name="recycle"></param>
		/// <param name="maxRowCount"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public static IEnumerable<T> GetRowsByObjectIdsBatched<T>(
			[NotNull] Table table,
			[NotNull] IEnumerable<long> objectIds,
			SpatialReference outputSpatialReference,
			bool recycle,
			int maxRowCount,
			CancellationToken cancellationToken = default) where T : Row
		{
			foreach (IList<long> oidBatch in CollectionUtils.Split(objectIds, maxRowCount))
			{
				if (oidBatch.Count == 0)
				{
					continue;
				}

				QueryFilter filter = GetOidListFilter(table, oidBatch, outputSpatialReference);

				foreach (var row in GetRows<T>(table, filter, recycle, cancellationToken))
				{
					yield return row;
				}
			}
		}

		public static RowCursor OpenCursor([NotNull] Table table,
		                                   [CanBeNull] QueryFilter filter,
		                                   bool recycle)
		{
			// NOTE: An invalid filter (e.g. subfields "*,OBJECTID") can crash the application.
			_msg.VerboseDebug(() => $"Querying table {table.GetName()} using filter: " +
			                        $"{FilterPropertiesToString(filter)}");

			RowCursor cursor = null;

			try
			{
				cursor = table.Search(filter, recycle);
			}
			catch (Exception e)
			{
				cursor?.Dispose();

				_msg.Debug($"Error querying table {table.GetName()} using filter " +
				           $"{FilterPropertiesToString(filter)}", e);
				throw;
			}

			return cursor;
		}

		/// <summary>
		/// Ensures that with the specified sub-fields string the required field will be fetched in a
		/// SQL query. Otherwise it will be added to the result string and the method will return true.
		/// </summary>
		/// <param name="currentSubFields"></param>
		/// <param name="fieldNameToEnsure"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		public static bool EnsureSubField(string currentSubFields, string fieldNameToEnsure,
		                                  out string result)
		{
			result = null;

			if (string.IsNullOrEmpty(currentSubFields))
			{
				result = fieldNameToEnsure;
				return true;
			}

			if (currentSubFields.Trim().Equals("*"))
			{
				return false;
			}

			// ensure OID field is in SubFields:
			var existingFields =
				new HashSet<string>(StringUtils.SplitAndTrim(currentSubFields, ','),
				                    StringComparer.OrdinalIgnoreCase);

			if (existingFields.Contains(fieldNameToEnsure))
			{
				return false;
			}

			result = string.Concat(currentSubFields, ",", fieldNameToEnsure);

			return true;
		}

		public static string FilterPropertiesToString([CanBeNull] QueryFilter filter)
		{
			if (filter == null)
			{
				return "Filter is null";
			}

			StringBuilder sb = new StringBuilder();

			sb.AppendLine($"SubFields: {filter.SubFields}");
			sb.AppendLine($"WhereClause: {filter.WhereClause}");

			sb.AppendLine($"PrefixClause: {filter.PrefixClause}");
			sb.AppendLine($"PostfixClause: {filter.PostfixClause}");

			var spatialFilter = filter as SpatialQueryFilter;

			if (spatialFilter != null)
			{
				sb.AppendLine($"SpatialRel: {spatialFilter.SpatialRelationship}");

				// TODO: Remove once 3.0 is de-supported
				// TODO: Define compilation symbol / variable in project for Pro versions
#if DEBUG
				sb.AppendLine(
					$"SpatialRelDescription: {spatialFilter.SpatialRelationshipDescription}");
#endif

				sb.AppendLine($"SearchOrder: {spatialFilter.SearchOrder}");

				if (spatialFilter.FilterGeometry != null)
				{
					sb.AppendFormat("Filter geometry (envelope):");
					sb.AppendFormat(GeometryUtils.Format(spatialFilter.FilterGeometry.Extent));
				}
				else
				{
					sb.AppendFormat("Filter geometry: not defined");
				}

				if (spatialFilter.OutputSpatialReference != null)
				{
					sb.Append(
						$"Output spatial reference: {spatialFilter.OutputSpatialReference.Name}");
				}
			}

			return sb.ToString();
		}

		private static QueryFilter GetOidListFilter(
			[NotNull] Table table,
			IEnumerable<long> objectIds,
			SpatialReference outputSpatialReference)
		{
			var filter = new QueryFilter
			             {
				             WhereClause =
					             $"{table.GetDefinition().GetObjectIDField()} IN ({StringUtils.Concatenate(objectIds, ", ")})"
			             };

			filter.OutputSpatialReference = outputSpatialReference;

			return filter;
		}
	}
}
