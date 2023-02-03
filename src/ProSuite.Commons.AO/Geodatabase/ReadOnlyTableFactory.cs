using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	public static class ReadOnlyTableFactory
	{
		[ThreadStatic] private static Dictionary<ITable, ReadOnlyTable> _cache;

		static Dictionary<ITable, ReadOnlyTable> Cache =>
			_cache ?? (_cache = new Dictionary<ITable, ReadOnlyTable>());

		[NotNull]
		public static ReadOnlyFeatureClass Create<T>([NotNull] T featureClass)
			where T : IFeatureClass
		{
			return (ReadOnlyFeatureClass) Create((ITable) featureClass);
		}

		[NotNull]
		public static ReadOnlyTable Create([NotNull] ITable table,
		                                   [CanBeNull] string alternateOIDField = null,
		                                   bool createJoinedTable = false)
		{
			if (! Cache.TryGetValue(table, out ReadOnlyTable existing))
			{
				if (table is IFeatureClass fc)
				{
					existing = ! createJoinedTable
						           ? ReadOnlyFeatureClassHelper.Create(fc)
						           : ReadOnlyJoinedFeatureClassHelper.Create(fc);
				}
				else
				{
					existing = ! createJoinedTable
						           ? ReadOnlyTableHelper.Create(table)
						           : ReadOnlyJoinedTableHelper.Create(table);
				}

				existing.AlternateOidFieldName = alternateOIDField;

				Cache.Add(table, existing);
			}

			return existing;
		}

		[UsedImplicitly]
		private class ReadOnlyTableHelper : ReadOnlyTable
		{
			private ReadOnlyTableHelper() : base(null) { }
			public static ReadOnlyTable Create(ITable table)
				=> CreateReadOnlyTable(table);
		}


		[UsedImplicitly]
		private class ReadOnlyFeatureClassHelper : ReadOnlyFeatureClass
		{
			private ReadOnlyFeatureClassHelper() : base(null) { }
			public static ReadOnlyFeatureClass Create(IFeatureClass fc)
				=> CreateReadOnlyFeatureClass(fc);
		}

		[UsedImplicitly]
		private class ReadOnlyJoinedTableHelper : ReadOnlyJoinedTable
		{
			private ReadOnlyJoinedTableHelper() : base(null) { }
			public static ReadOnlyJoinedTable Create(ITable table)
				=> CreateReadOnlyJoinedTable(table);
		}

		[UsedImplicitly]
		private class ReadOnlyJoinedFeatureClassHelper : ReadOnlyJoinedFeatureClass
		{
			private ReadOnlyJoinedFeatureClassHelper() : base(null) { }
			public static ReadOnlyJoinedFeatureClass Create(IFeatureClass fc)
				=> CreateReadOnlyJoinedFeatureClass(fc);
		}

		public static void ClearCache()
		{
			Cache.Clear();
		}

		public static IEnumerable<IReadOnlyRow> EnumRows(IEnumerable<IRow> rows)
		{
			ITable current = null;
			ReadOnlyTable table = null;
			foreach (var row in rows)
			{
				ITable t = row.Table;
				if (t != current)
				{
					table = Create(row.Table);
					current = t;
				}

				yield return table.CreateRow(row);
			}
		}

	}
}
