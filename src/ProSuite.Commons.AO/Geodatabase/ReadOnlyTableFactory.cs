using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class ReadOnlyTableFactory : ReadOnlyFeatureClass
	{
		protected static readonly Dictionary<ITable, ReadOnlyTable> Cache = new Dictionary<ITable, ReadOnlyTable>();

		public static ReadOnlyFeatureClass Create<T>([NotNull] T featureClass)
			where T : IFeatureClass
		{
			return (ReadOnlyFeatureClass) Create((ITable) featureClass);
		}

		public static ReadOnlyTable Create([NotNull] ITable table)
		{
			if (!Cache.TryGetValue(table, out ReadOnlyTable existing))
			{
				if (table is IFeatureClass fc)
				{ existing = CreateReadOnlyFeatureClass(fc); }
				else
				{ existing = CreateReadOnlyTable(table); }

				Cache.Add(table, existing);
			}
			return existing;
		}
		public static void ClearCache()
		{
			Cache.Clear();
		}

		private ReadOnlyTableFactory() : base(null)
		{ }
	}
}
