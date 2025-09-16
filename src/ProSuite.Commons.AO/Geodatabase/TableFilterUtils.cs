using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

namespace ProSuite.Commons.AO.Geodatabase
{
	public static class TableFilterUtils
	{
		public static void SetSubFields([NotNull] ITableFilter queryFilter,
		                                params string[] fieldNames)
		{
			SetSubFields(queryFilter, (IEnumerable<string>) fieldNames);
		}

		public static void SetSubFields([NotNull] ITableFilter queryFilter,
		                                [NotNull] IEnumerable<string> fieldNames)
		{
			Assert.ArgumentNotNull(queryFilter, nameof(queryFilter));
			Assert.ArgumentNotNull(fieldNames, nameof(fieldNames));

			// The SubFields can't be set to null/empty and then added with AddField() 
			// -> SubFields reverts to "*" when setting to null/empty
			// instead, the first field must be assigned to the SubFields property. The other fields can 
			// then be added using AddField() (which makes sure that fields are included only once)

			HashSet<string> uniqueFieldNames =
				new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
			foreach (var fieldName in fieldNames)
			{
				uniqueFieldNames.Add(fieldName);
			}

			queryFilter.SubFields =
				string.Concat(uniqueFieldNames.Select(x => $"{x},")).TrimEnd(',');
		}

		[NotNull]
		public static IQueryFilter GetQueryFilter([CanBeNull] ITableFilter filter,
		                                          IFeatureClass featureClass = null)
		{
			IQueryFilter result;
			if (filter is IFeatureClassFilter fcFilter)
			{
				ISpatialFilter sf =
					GdbQueryUtils.CreateSpatialFilter(
						featureClass, fcFilter.FilterGeometry, fcFilter.SpatialRelationship,
						filterOwnsGeometry: true, outputSpatialReference: null);
				sf.SpatialRelDescription = fcFilter.SpatialRelDescription;
				result = sf;
			}
			else
			{
				result = GdbQueryUtils.CreateQueryFilter();
			}

			if (filter != null)
			{
				result.SubFields = filter.SubFields;
				result.WhereClause = filter.WhereClause;

				if (! string.IsNullOrWhiteSpace(filter.PostfixClause))
				{
					var filterDefinition = (IQueryFilterDefinition) result;
					filterDefinition.PostfixClause = filter.PostfixClause;
				}
			}

			return result;
		}

		public static IEnumerable<IReadOnlyRow> GetRows(IReadOnlyTable table,
		                                                IEnumerable<long> oids, bool recycle)
		{
			return GetRowsInList(table, table.OIDFieldName, oids, recycle);
		}

		[NotNull]
		public static IEnumerable<IReadOnlyRow> GetRowsInList(
			[NotNull] IReadOnlyTable table,
			[NotNull] string fieldName,
			[NotNull] IEnumerable valueList,
			bool recycle,
			[CanBeNull] ITableFilter queryFilter = null)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));
			Assert.ArgumentNotNull(valueList, nameof(valueList));

			foreach (var row in GetRowsInList(table,
			                                  DatasetUtils.GetField(table, fieldName), valueList,
			                                  (q) => GetRows(table, q, recycle), queryFilter))
			{
				yield return row;
			}
		}

		[NotNull]
		public static IEnumerable<IReadOnlyRow> GetRows(
			[NotNull] IReadOnlyTable table,
			[NotNull] ITableFilter filter,
			bool recycle)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(filter, nameof(filter));

			foreach (var row in table.EnumRows(filter, recycle))
			{
				yield return row;
			}
		}

		private static IEnumerable<T> GetRowsInList<T>(
			[NotNull] IReadOnlyTable table,
			[NotNull] IField field,
			[NotNull] IEnumerable valueList,
			[NotNull] Func<ITableFilter, IEnumerable<T>> getRows,
			[CanBeNull] ITableFilter queryFilter = null)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(valueList, nameof(valueList));
			Assert.ArgumentNotNull(getRows, nameof(getRows));

			// TODO: assert that the values match the field type

			esriFieldType fieldType = field.Type;

			queryFilter = queryFilter ?? new AoTableFilter();

			GdbQueryUtils.GetWhereClauseLimits(
				table.Workspace, out int maxWhereClauseLength, out int maxValueCount);

			string origWhereClause = queryFilter.WhereClause;
			try
			{
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
							sb.Append(")");
							queryFilter.WhereClause = sb.ToString();

							foreach (T row in getRows(queryFilter))
							{
								yield return row;
							}
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

					sb.Append(GdbSqlUtils.GetLiteral(value, fieldType, table.Workspace));
					valueCount++;
				}

				if (sb != null)
				{
					sb.Append(")");
					queryFilter.WhereClause = sb.ToString();

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
	}
}
