using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

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

		IEnumerable<IReadOnlyRow> EnumRows([CanBeNull] ITableFilter filter, bool recycle);

		long RowCount([CanBeNull] ITableFilter filter);

		bool Equals([CanBeNull] IReadOnlyTable otherTable);
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

	public interface IReadOnlyRow : IDbRow
	{
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
