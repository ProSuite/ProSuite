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
