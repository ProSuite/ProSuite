using System;
using System.Collections.Generic;
using System.Linq;
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
		                                   [CanBeNull] string alternateOIDField = null)
		{
			Func<ReadOnlyTable> createFunc =
				() => table is IFeatureClass fc
					      ? ReadOnlyFeatureClassHelper.Create(fc)
					      : ReadOnlyTableHelper.Create(table);

			ReadOnlyTable result = EnsureCached(table, createFunc, alternateOIDField);

			return result;
		}

		public static ReadOnlyTable CreateQueryTable(
			[NotNull] ITable aoQueryTable,
			[NotNull] string alternateOIDField,
			[NotNull] IEnumerable<ITable> baseTables)
		{
			Func<ReadOnlyTable> createFunc =
				() =>
				{
					IEnumerable<ReadOnlyTable> roTables =
						baseTables.Select(t => Create(t));

					return aoQueryTable is IFeatureClass fc
						       ? (ReadOnlyTable) ReadOnlyJoinedFeatureClass.Create(fc, roTables)
						       : ReadOnlyJoinedTable.Create(aoQueryTable, roTables);
				};

			ReadOnlyTable result = EnsureCached(aoQueryTable, createFunc, alternateOIDField);

			return result;
		}

		private static ReadOnlyTable EnsureCached([NotNull] ITable aoTable,
		                                          [NotNull] Func<ReadOnlyTable> createAction,
		                                          [CanBeNull] string alternateOIDField)
		{
			if (! Cache.TryGetValue(aoTable, out ReadOnlyTable existing))
			{
				existing = createAction();

				existing.AlternateOidFieldName = alternateOIDField;

				Cache.Add(aoTable, existing);
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
