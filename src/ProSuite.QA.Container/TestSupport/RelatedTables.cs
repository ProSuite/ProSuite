using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public class RelatedTables
	{
		// list of tables and OID-field index in joined table
		private readonly List<RelatedTable> _relTables;

		private RelatedTables([NotNull] List<RelatedTable> relTables)
		{
			Assert.ArgumentNotNull(relTables, nameof(relTables));

			_relTables = relTables;
		}

		[NotNull]
		public static RelatedTables Create([NotNull] IList<ITable> relatedTables,
		                                   [NotNull] ITable joinedTable)
		{
			Assert.ArgumentNotNull(relatedTables, nameof(relatedTables));
			Assert.ArgumentNotNull(joinedTable, nameof(joinedTable));

			var list = new List<RelatedTable>();

			foreach (ITable table in relatedTables)
			{
				if (! table.HasOID)
				{
					continue;
				}

				string tableName = ((IDataset) table).Name;
				string oidFieldName = tableName + "." + table.OIDFieldName;
				int oidFieldIndex = joinedTable.FindField(oidFieldName);

				if (oidFieldIndex >= 0)
				{
					list.Add(new RelatedTable(table, tableName, oidFieldName, oidFieldIndex));
				}
			}

			return new RelatedTables(list);
		}

		[NotNull]
		public IList<RelatedTable> Related => _relTables.AsReadOnly();

		[NotNull]
		public InvolvedRows GetInvolvedRows([NotNull] IRow row)
		{
			InvolvedRows involved = new InvolvedRows();
			involved.TestedRows.Add(row);

			foreach (RelatedTable relatedTable in _relTables)
			{
				object oid = row.Value[relatedTable.OidFieldIndex];

				if (oid is int)
				{
					involved.Add(new InvolvedRow(relatedTable.TableName, (int) oid));
				}
			}

			return involved;
		}

		[CanBeNull]
		public IGeometry GetGeometry([NotNull] IRow row)
		{
			IGeometry geometry = TestUtils.GetShapeCopy(row);
			if (geometry != null)
			{
				return geometry;
			}

			foreach (RelatedTable relatedTable in _relTables)
			{
				if (relatedTable.IsFeatureClass == false)
				{
					continue;
				}

				object oid = row.Value[relatedTable.OidFieldIndex];
				if (! (oid is int))
				{
					continue;
				}

				geometry = TestUtils.GetShapeCopy(relatedTable.Table.GetRow((int) oid));
				if (geometry != null)
				{
					return geometry;
				}
			}

			return null;
		}
	}
}
