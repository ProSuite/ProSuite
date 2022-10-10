using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class ReadOnlyTableFactory : ReadOnlyFeatureClass
	{
		[ThreadStatic] private static Dictionary<ITable, ReadOnlyTable> _cache;

		protected static Dictionary<ITable, ReadOnlyTable> Cache =>
			_cache ?? (_cache = new Dictionary<ITable, ReadOnlyTable>());

		public static ReadOnlyFeatureClass Create<T>([NotNull] T featureClass)
			where T : IFeatureClass
		{
			return (ReadOnlyFeatureClass) Create((ITable) featureClass);
		}

		public static ReadOnlyTable Create([NotNull] ITable table,
		                                   [CanBeNull] string alternateOIDField = null)
		{
			if (! Cache.TryGetValue(table, out ReadOnlyTable existing))
			{
				if (table is IFeatureClass fc)
				{
					existing = CreateReadOnlyFeatureClass(fc);
				}
				else
				{
					existing = CreateReadOnlyTable(table);
				}

				existing.AlternateOidFieldName = alternateOIDField;

				Cache.Add(table, existing);
			}

			return existing;
		}

		public static void ClearCache()
		{
			Cache.Clear();
		}

		private ReadOnlyTableFactory() : base(null) { }
	}
}
