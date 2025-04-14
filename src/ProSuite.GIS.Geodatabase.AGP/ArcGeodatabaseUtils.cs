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
			[NotNull] Table proTable,
			bool eagerPropertyCaching = false)
		{
			Table databaseTable = DatasetUtils.GetDatabaseTable(proTable);

			var gdb = (ArcGIS.Core.Data.Geodatabase) databaseTable.GetDatastore();

			ArcWorkspace existingWorkspace = ArcWorkspace.GetByHandle(gdb.Handle);

			ArcTable found = existingWorkspace?.GetTableByName(databaseTable.GetName());

			if (found != null)
			{
				if (eagerPropertyCaching)
				{
					found.CacheProperties();
				}

				return found;
			}

			ArcTable result = databaseTable is FeatureClass featureClass
				                  ? new ArcFeatureClass(featureClass, eagerPropertyCaching)
				                  : new ArcTable(proTable, eagerPropertyCaching);

			existingWorkspace?.Cache(result);

			return result;
		}

		public static ArcFeatureClass ToArcFeatureClass(
			[NotNull] FeatureClass proFeatureClass,
			bool eagerPropertyCaching = false)
		{
			return (ArcFeatureClass) ToArcTable((Table) proFeatureClass, eagerPropertyCaching);
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
		public static ArcDomain ToArcDomain([CanBeNull] Domain domain,
		                                    [CanBeNull] IFeatureWorkspace workspace)
		{
			if (domain == null)
			{
				return null;
			}

			ArcDomain cachedDomain = workspace?.get_DomainByName(domain.GetName()) as ArcDomain;

			if (cachedDomain != null)
			{
				return cachedDomain;
			}

			ArcDomain result = null;

			// Use the ProDomain
			if (domain is CodedValueDomain codedDomain)
			{
				result = new ArcCodedValueDomain(codedDomain);
			}

			if (domain is RangeDomain rangeDomain)
			{
				result = new ArcRangeDomain(rangeDomain);
			}

			if (result != null && workspace is ArcWorkspace arcWorkspace)
			{
				arcWorkspace.Cache(result);
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
