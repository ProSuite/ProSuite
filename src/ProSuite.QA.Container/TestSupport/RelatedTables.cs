using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
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
		public static RelatedTables Create([NotNull] IList<IReadOnlyTable> relatedTables,
		                                   [NotNull] IReadOnlyTable joinedTable)
		{
			Assert.ArgumentNotNull(relatedTables, nameof(relatedTables));
			Assert.ArgumentNotNull(joinedTable, nameof(joinedTable));

			var list = new List<RelatedTable>();

			foreach (IReadOnlyTable table in relatedTables)
			{
				if (! table.HasOID)
				{
					continue;
				}

				string tableName = table.Name;
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
	}
}
