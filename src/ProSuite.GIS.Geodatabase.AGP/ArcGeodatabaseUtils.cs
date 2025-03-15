using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.GIS.Geodatabase.API;

namespace ProSuite.GIS.Geodatabase.AGP
{
	public static class ArcGeodatabaseUtils
	{
		internal static IEnumerable<IRow> GetArcRows(
			RowCursor cursor, ITable sourceTable = null)
		{
			while (cursor.MoveNext())
			{
				Row row = cursor.Current;

				yield return ToArcRow(row, sourceTable);
			}
		}

		public static ArcTable ToArcTable(
			[NotNull] Table proTable)
		{
			Table databaseTable =
				DatasetUtils.GetDatabaseTable(proTable);

			ArcTable result = databaseTable is FeatureClass featureClass
				                  ? new ArcFeatureClass(featureClass)
				                  : new ArcTable(proTable);

			return result;
		}

		public static ArcRow ToArcRow([NotNull] Row proRow,
		                              [CanBeNull] ITable parent = null)
		{
			if (parent == null)
			{
				parent = ToArcTable(proRow.GetTable());
			}

			return ArcRow.Create(proRow, parent);
		}

		[CanBeNull]
		public static ArcDomain ToArcDomain([CanBeNull] Domain domain)
		{
			if (domain == null)
			{
				return null;
			}

			if (domain is CodedValueDomain codedDomain)
			{
				return new ArcCodedValueDomain(codedDomain);
			}

			if (domain is RangeDomain rangeDomain)
			{
				return new ArcRangeDomain(rangeDomain);
			}

			throw new ArgumentOutOfRangeException("Unknown domain type");
		}

		public static QueryFilter ToProQueryFilter(IQueryFilter queryFilter)
		{
			QueryFilter result;
			if (queryFilter is ISpatialFilter spatialFilter)
			{
				result = new SpatialQueryFilter()
				         {
					         FilterGeometry =
						         (ArcGIS.Core.Geometry.Geometry) spatialFilter.Geometry
							         .NativeImplementation,
					         SpatialRelationship = (SpatialRelationship) spatialFilter.SpatialRel
				         };
			}
			else
			{
				result = new QueryFilter();
			}

			result.WhereClause = queryFilter.WhereClause;
			result.SubFields = queryFilter.SubFields;
			result.PostfixClause = queryFilter.PostfixClause;

			return result;
		}
	}
}
