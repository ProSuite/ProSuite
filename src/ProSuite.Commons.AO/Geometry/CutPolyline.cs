using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.AO.Geometry
{
	public class CutPolyline
	{
		public IPolyline Polyline { get; set; }

		public int? ObjectId { get; set; }

		public bool? SuccessfulCut { get; set; }

		public RingPlaneTopology? RingPlaneTopology { get; }

		public CutPolyline(IPolyline polyline, RingPlaneTopology? ringPlaneTopology = null)
		{
			Polyline = polyline;
			RingPlaneTopology = ringPlaneTopology;
		}

		public CutPolyline(int objectId)
		{
			ObjectId = objectId;
		}

		public override string ToString()
		{
			return
				$"Id: {ObjectId}, RingPlaneTopology: {RingPlaneTopology}, cutline (length {Polyline?.Length})";
		}
	}
}
