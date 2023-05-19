using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Db;

namespace ProSuite.Commons.AO.Geodatabase
{
	public interface IReadOnlyDataset
	{
		string Name { get; }
		IName FullName { get; }
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

		IReadOnlyRow GetRow(long oid);

		IEnumerable<IReadOnlyRow> EnumRows(ITableFilter filter, bool recycle);

		long RowCount(ITableFilter filter);

		bool Equals(IReadOnlyTable otherTable);
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

		long OID { get; }

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
}
