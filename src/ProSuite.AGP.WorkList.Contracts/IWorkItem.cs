using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface IWorkItem
	{
		int OID { get; }
		bool Visited { get; set; }
		GdbRowIdentity Proxy { get; }
		WorkItemStatus Status { get; set; }
		Envelope Extent { get; }
		string Description { get; }
		GeometryType? GeometryType { get; }
		bool HasGeometry { get; set; }

		void QueryPoints(out double xmin, out double ymin,
		                 out double xmax, out double ymax,
		                 out double zmax, double minimumSize);

		void QueryPoints(out double xmin, out double ymin,
		                 out double xmax, out double ymax,
		                 out double zmax);
	}
}
