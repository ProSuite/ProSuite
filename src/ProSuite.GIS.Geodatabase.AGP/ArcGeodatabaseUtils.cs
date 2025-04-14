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

			ArcWorkspace existingWorkspace = null;

			var gdb = databaseTable.GetDatastore() as ArcGIS.Core.Data.Geodatabase;

			if (gdb != null)
			{
				existingWorkspace = ArcWorkspace.GetByHandle(gdb.Handle);

				ArcTable found = existingWorkspace?.GetTableByName(databaseTable.GetName());

				if (found != null)
				{
					if (eagerPropertyCaching)
					{
						found.CacheProperties();
					}

					return found;
				}
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

			var arcWorkspace = workspace as ArcWorkspace;

			// Cached domain:
			ArcDomain existing = arcWorkspace?.GetDomainByName(domain.GetName());

			if (existing != null)
			{
				return existing;
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

			if (result == null)
			{
				throw new ArgumentOutOfRangeException(nameof(domain),
				                                      $"Domain {domain.GetName()} is neither range nor coded value domain");
			}

			arcWorkspace?.Cache(result);

			return result;
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
