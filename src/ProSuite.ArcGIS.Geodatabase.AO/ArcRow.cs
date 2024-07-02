extern alias EsriGeodatabase;
extern alias EsriGeometry;
using ESRI.ArcGIS.Geodatabase.AO;
using EsriGeodatabase::ESRI.ArcGIS.Geodatabase;

using EsriGeometry::ESRI.ArcGIS.Geometry;
using ProSuite.ArcGIS.Geometry.AO;
using ArcEnvelope = ESRI.ArcGIS.Geometry.ArcEnvelope;
using ArcGeometry = ESRI.ArcGIS.Geometry.ArcGeometry;

namespace ESRI.ArcGIS.Geodatabase
{
	extern alias EsriGeometry;

	public class ArcRow : IObject
	{
		private readonly EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IObject _aoObject;

		public ArcRow(EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IRow aoObject)
		: this((EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IObject)aoObject)
		{ }

		public ArcRow(EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IObject aoObject)
		{
			_aoObject = aoObject;
		}

		public EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IObject AoObject => _aoObject;

		#region Implementation of IRowBuffer

		public object get_Value(int Index)
		{
			return _aoObject.get_Value(Index);
		}

		public void set_Value(int Index, object Value)
		{
			_aoObject.set_Value(Index, Value);
		}

		public IFields Fields => new ArcFields(_aoObject.Fields);

		#endregion

		#region Implementation of IRow

		public bool HasOID => _aoObject.HasOID;

		public long OID => _aoObject.OID;

		public ITable Table => new ArcTable(_aoObject.Table);

		public void Store()
		{
			_aoObject.Store();
		}

		public void Delete()
		{
			_aoObject.Delete();
		}

		#endregion

		#region Implementation of IObject

		public IObjectClass Class => new ArcTable(_aoObject.Table);

		#endregion
	}

	public class ArcFeature : ArcRow, IFeature
	{
		private readonly EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IFeature _aoFeature;

		public ArcFeature(EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IFeature aoFeature)
			: base(aoFeature)
		{
			_aoFeature = aoFeature;
		}

		#region Implementation of IFeature

		public ESRI.ArcGIS.Geometry.IGeometry ShapeCopy => new ArcGeometry(_aoFeature.ShapeCopy);

		public ESRI.ArcGIS.Geometry.IGeometry Shape
		{
			get => new ArcGeometry(_aoFeature.Shape);
			set => _aoFeature.Shape = ((ArcGeometry)value).AoGeometry;
		}

		public ESRI.ArcGIS.Geometry.IEnvelope Extent => new ArcEnvelope(_aoFeature.Extent);

		#endregion
	}
}
