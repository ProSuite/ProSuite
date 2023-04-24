using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;

namespace ProSuite.QA.Container.PolygonGrower
{
	public class NetPoint : NetElement
	{
		private readonly NetPoint_ _point;

		public NetPoint(TableIndexRow row) : base(row)
		{
			_point = new NetPoint_((IPoint) ((IReadOnlyFeature) row.Row).Shape);
		}

		public override string ToString()
		{
			return $"OID:{Row.Row.OID}; Pt:[{_point.Point.X:N0},{_point.Point.Y:N0}]";
		}

		protected override NetPoint_ NetPoint__
		{
			get { return _point; }
		}

		protected override NetPoint_ QueryNetPoint(NetPoint_ queryPoint)
		{
			return _point;
		}
	}
}
