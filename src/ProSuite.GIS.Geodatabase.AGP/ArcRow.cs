using System;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ESRI.ArcGIS.Geodatabase.AO;
using ESRI.ArcGIS.Geometry;
using ProSuite.ArcGIS.Geodatabase.AO;

namespace ESRI.ArcGIS.Geodatabase
{

	public class ArcRow : IObject
	{
		private readonly Row _proRow;
		private readonly ITable _parentTable;

		public ArcRow(Row proRow, ITable parentTable)
		{
			_proRow = proRow;
			_parentTable = parentTable;
		}

		public Row ProRow => _proRow;

		#region Implementation of IRowBuffer

		public object get_Value(int index)
		{
			return _proRow[index];
		}

		public void set_Value(int index, object value)
		{
			_proRow[index] = value;
		}

		public IFields Fields =>
			new ArcFields(_proRow.GetFields(), new ArcGeometryDef(GetShapeDescription()));

		private ShapeDescription GetShapeDescription()
		{
			FeatureClass featureClass = _proRow.GetTable() as FeatureClass;

			if (featureClass == null)
			{
				return null;
			}

			FeatureClassDefinition classDefinition = featureClass.GetDefinition();

			return new ShapeDescription(classDefinition);
		}

		#endregion

		#region Implementation of IRow

		// TODO: Discuss this
		//public bool HasOID => _proRow.HasOID;
		public bool HasOID => throw new NotImplementedException();

		public long OID => _proRow.GetObjectID();

		public ITable Table => new ArcTable(_proRow.GetTable());

		public void Store()
		{
			_proRow.Store();
		}

		public void Delete()
		{
			_proRow.Delete();
		}

		#endregion

		#region Implementation of IObject

		public IObjectClass Class => new ArcTable(_proRow.GetTable());

		#endregion
	}

	public class ArcFeature : ArcRow, IFeature
	{
		private readonly Feature _proFeature;

		public ArcFeature(Feature proFeature, IFeatureClass parentClass)
			: base(proFeature, parentClass as ITable)
		{
			_proFeature = proFeature;
		}

		#region Implementation of IFeature

		public IGeometry ShapeCopy
		{
			get
			{
				global::ArcGIS.Core.Geometry.Geometry clone = _proFeature.GetShape().Clone();
				return new ArcGeometry(clone);
			}
		}

		public IGeometry Shape
		{
			get => new ArcGeometry(_proFeature.GetShape());
			set => _proFeature.SetShape(((ArcGeometry) value).ProGeometry);
		}

		public IEnvelope Extent
		{
			get
			{
				global::ArcGIS.Core.Geometry.Geometry geometry = _proFeature.GetShape();

				return new ArcEnvelope(geometry.Extent);
			}
		}

		#endregion
	}
}
