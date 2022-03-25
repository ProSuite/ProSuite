
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using System.Collections.Generic;

namespace ProSuite.Commons.AO.Geodatabase
{
	public interface IReadOnlyDataset
	{
		string Name { get; }
		ESRI.ArcGIS.esriSystem.IName FullName { get; }
		IWorkspace Workspace { get; }
	}

	public interface IRowCreator<T>
		where T : IRow, IReadOnlyRow
	{
		T CreateRow();
	}
	public interface IReadOnlyTable : IReadOnlyDataset
	{
		IFields Fields { get; }
		int FindField(string fieldName);
		bool HasOID { get; }
		string OIDFieldName { get; }
		IReadOnlyRow GetRow(int oid);
		IEnumerable<IReadOnlyRow> EnumRows(IQueryFilter filter, bool recycle);
		int RowCount(IQueryFilter filter);
	}
	public interface IReadOnlyGeoDataset
	{
		IEnvelope Extent { get; }
		ISpatialReference SpatialReference { get; }
	}

	public interface IReadOnlyFeatureClass : IReadOnlyTable, IReadOnlyGeoDataset
	{
		string ShapeFieldName { get; }
		esriGeometryType ShapeType { get; }
		IField AreaField { get; }
		IField LengthField { get; }
	}

	public interface IReadOnlyRow
	{
		bool HasOID { get; }
		int OID { get; }
		object get_Value(int Index);
		IReadOnlyTable Table { get; }
	}
	public interface IReadOnlyFeature : IReadOnlyRow
	{
		IGeometry Shape { get; }
		IGeometry ShapeCopy { get; }
		IEnvelope Extent { get; }
		esriFeatureType FeatureType { get; }
		IReadOnlyFeatureClass FeatureClass { get; }
	}

	public class ReadOnlyTableFactory : ReadOnlyFeatureClass
	{
		protected static readonly Dictionary<ESRI.ArcGIS.Geodatabase.ITable, ReadOnlyTable> Cache = new Dictionary<ESRI.ArcGIS.Geodatabase.ITable, ReadOnlyTable>();

		public static ReadOnlyFeatureClass Create<T>([NotNull] T featureClass)
			where T : IFeatureClass
		{
			return (ReadOnlyFeatureClass) Create((ESRI.ArcGIS.Geodatabase.ITable) featureClass);
		}

		public static ReadOnlyTable Create([NotNull] ESRI.ArcGIS.Geodatabase.ITable table)
		{
			if (!Cache.TryGetValue(table, out ReadOnlyTable existing))
			{
				if (table is ESRI.ArcGIS.Geodatabase.IFeatureClass fc)
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
	public class ReadOnlyFeatureClass : ReadOnlyTable, IReadOnlyFeatureClass
	{
		protected static ReadOnlyFeatureClass CreateReadOnlyFeatureClass(ESRI.ArcGIS.Geodatabase.IFeatureClass fc)
		{ return new ReadOnlyFeatureClass(fc); }
		protected ReadOnlyFeatureClass(ESRI.ArcGIS.Geodatabase.IFeatureClass featureClass)
			: base((ESRI.ArcGIS.Geodatabase.ITable)featureClass)
		{ }

		public string ShapeFieldName => FeatureClass.ShapeFieldName;
		public IField AreaField => Commons.AO.Geodatabase.DatasetUtils.GetAreaField(FeatureClass);
		public IField LengthField => Commons.AO.Geodatabase.DatasetUtils.GetLengthField(FeatureClass);
		public IEnvelope Extent => ((ESRI.ArcGIS.Geodatabase.IGeoDataset)FeatureClass).Extent;
		public ISpatialReference SpatialReference => ((ESRI.ArcGIS.Geodatabase.IGeoDataset)FeatureClass).SpatialReference;
		public esriGeometryType ShapeType => FeatureClass.ShapeType;
		protected ESRI.ArcGIS.Geodatabase.IFeatureClass FeatureClass => (ESRI.ArcGIS.Geodatabase.IFeatureClass)Table;
		public override ReadOnlyRow CreateRow(ESRI.ArcGIS.Geodatabase.IRow row)
		{
			return new ReadOnlyFeature(this, (ESRI.ArcGIS.Geodatabase.IFeature)row);
		}
	}
	public class ReadOnlyTable : IReadOnlyTable
	{
		public static IEnumerable<IReadOnlyRow> EnumRows(IEnumerable<ESRI.ArcGIS.Geodatabase.IRow> rows)
		{
			ESRI.ArcGIS.Geodatabase.ITable current = null;
			ReadOnlyTable table = null;
			foreach (var row in rows)
			{
				ESRI.ArcGIS.Geodatabase.ITable t = row.Table;
				if (t != current)
				{
					table = CreateReadOnlyTable(row.Table);
					current = t;
				}
				yield return table.CreateRow(row);
			}
		}

		protected static ReadOnlyTable CreateReadOnlyTable(ESRI.ArcGIS.Geodatabase.ITable table)
		{ return new ReadOnlyTable(table); }

		private readonly ESRI.ArcGIS.Geodatabase.ITable _table;
		protected ReadOnlyTable(ESRI.ArcGIS.Geodatabase.ITable table)
		{
			_table = table;
		}

		public ESRI.ArcGIS.Geodatabase.ITable BaseTable => _table;
		protected ESRI.ArcGIS.Geodatabase.ITable Table => _table;
		ESRI.ArcGIS.esriSystem.IName IReadOnlyDataset.FullName => ((ESRI.ArcGIS.Geodatabase.IDataset)_table).FullName;
		IWorkspace IReadOnlyDataset.Workspace => ((ESRI.ArcGIS.Geodatabase.IDataset)_table).Workspace;
		public string Name => Commons.AO.Geodatabase.DatasetUtils.GetName(_table);
		public IFields Fields => _table.Fields;
		public int FindField(string name) => _table.FindField(name);
		public bool HasOID => _table.HasOID;
		public string OIDFieldName => _table.OIDFieldName;
		public IReadOnlyRow GetRow(int oid) => CreateRow(_table.GetRow(oid));
		public int RowCount(IQueryFilter filter) => _table.RowCount(filter);

		public virtual ReadOnlyRow CreateRow(ESRI.ArcGIS.Geodatabase.IRow row)
		{
			return new ReadOnlyRow(this, row);
		}

		public IEnumerable<IReadOnlyRow> EnumRows(IQueryFilter filter, bool recycle)
		{
			foreach (var row in new Commons.AO.EnumCursor(_table, filter, recycle))
			{
				yield return CreateRow(row);
			}
		}
	}
	public class ReadOnlyRow : IReadOnlyRow
	{
		public static ReadOnlyFeature Create(IFeature row)
		{
			ReadOnlyTable tbl = ReadOnlyTableFactory.Create(row.Table);
			ReadOnlyFeatureClass fc = (ReadOnlyFeatureClass) tbl;
			return new ReadOnlyFeature(fc, row);
		}

		public static ReadOnlyRow Create(IRow row)
		{
			ReadOnlyTable tbl = ReadOnlyTableFactory.Create(row.Table);
			if (tbl is ReadOnlyFeatureClass fc)
			{
				return new ReadOnlyFeature(fc, (IFeature) row);
			}

			return new ReadOnlyRow(tbl, row);
		}
		public ReadOnlyRow(ReadOnlyTable table, ESRI.ArcGIS.Geodatabase.IRow row)
		{
			Table = table;
			Row = row;
		}

		public ESRI.ArcGIS.Geodatabase.IRow BaseRow => Row;
		protected ESRI.ArcGIS.Geodatabase.IRow Row { get; }
		public bool HasOID => Row.HasOID;
		public int OID => Row.OID;
		public object get_Value(int field) => Row.Value[field];
		IReadOnlyTable IReadOnlyRow.Table => Table;
		public ReadOnlyTable Table { get; }
	}

	public class ReadOnlyFeature : ReadOnlyRow, IReadOnlyFeature
	{
		public static ReadOnlyFeature Create(IFeature feature)
		{
			return new ReadOnlyFeature(
				ReadOnlyTableFactory.Create((IFeatureClass) feature.Table), feature);
		}
		public ReadOnlyFeature(ReadOnlyFeatureClass featureClass, ESRI.ArcGIS.Geodatabase.IFeature feature)
			: base(featureClass, feature)
		{ }
		protected ESRI.ArcGIS.Geodatabase.IFeature Feature => (ESRI.ArcGIS.Geodatabase.IFeature)Row;
		public IEnvelope Extent => Feature.Extent;
		public IGeometry Shape => Feature.Shape;
		public IGeometry ShapeCopy => Feature.ShapeCopy;
		IReadOnlyFeatureClass IReadOnlyFeature.FeatureClass => FeatureClass;
		public ReadOnlyFeatureClass FeatureClass => (ReadOnlyFeatureClass)Table;
		public esriFeatureType FeatureType => Feature.FeatureType;
	}

}
