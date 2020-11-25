using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.QA.Container.PolygonGrower
{
	public class NetPoint : NetElement
	{
		private readonly NetPoint_ _point;

		public NetPoint(TableIndexRow row) : base(row)
		{
			_point = new NetPoint_((IPoint) ((IFeature) row.Row).Shape);
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
