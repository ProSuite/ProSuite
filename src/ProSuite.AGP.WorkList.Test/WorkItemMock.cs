using System;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList.Test
{
	public class WorkItemMock : IWorkItem
	{
		public WorkItemMock(int oid)
		{
			OID = oid;
			Status = WorkItemStatus.Todo;
		}

		public int OID { get; }
		public bool Visited { get; set; }
		public GdbRowIdentity Proxy { get; }
		public WorkItemStatus Status { get; set; }
		public Envelope Extent { get; }
		public string Description { get; }
		public GeometryType? GeometryType { get; set; }

		public void QueryPoints(out double xmin, out double ymin, out double xmax, out double ymax,
		                        out double zmax,
		                        double minimumSize)
		{
			throw new NotImplementedException();
		}

		public void QueryPoints(out double xmin, out double ymin, out double xmax, out double ymax,
		                        out double zmax)
		{
			throw new NotImplementedException();
		}

		public void SetDone(bool done = true)
		{
			throw new NotImplementedException();
		}

		[Obsolete("use Visited property")]
		public void SetVisited(bool visited = true)
		{
			throw new NotImplementedException();
		}

		public override string ToString()
		{
			return $"OID {OID}";
		}
	}
}
