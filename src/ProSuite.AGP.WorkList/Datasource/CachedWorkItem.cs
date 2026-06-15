using System;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList.Datasource;

/// <summary>
/// A work item that has been read from the (XML) persisted work list state. It represents a
/// snapshot of the work item at the time the work list was persisted. It could potentially
/// be stale if the underlying data has changed since then.
/// This lightweight implementation does not support setting or caching the buffered geometry
/// and is used for display purposes only based on the <see cref="WorkItemTable"/>.
/// Once the work list is opened in the navigator (by the very latest), the DB-based work items
/// will need to used in the form of another <see cref="IWorkItem"/> implementation.
/// </summary>
public class CachedWorkItem : IWorkItem
{
	private readonly GdbRowIdentity _rowIdentity;

	public CachedWorkItem(long oid, GdbRowIdentity rowIdentity)
	{
		_rowIdentity = rowIdentity;
		OID = oid;
	}

	public long OID { get; set; }

	public long ObjectID => _rowIdentity.ObjectId;

	public long UniqueTableId => _rowIdentity.Table.Id;

	public GdbRowIdentity GdbRowProxy => _rowIdentity;

	public WorkItemStatus Status { get; set; }

	public bool Visited { get; set; }

	public bool HasExtent => Extent != null;

	public bool HasBufferedGeometry => false;

	public void SetBufferedGeometry(Geometry geometry) { }

	public void SetExtent(Envelope extent)
	{
		Extent = extent;
	}

	public string GetDescription()
	{
		return null;
	}

	public Envelope Extent { get; private set; }

	public Geometry BufferedGeometry => null;

	public GeometryType? SourceGeometryType { get; set; }

	#region Equality members

	protected bool Equals(WorkItem other)
	{
		return UniqueTableId == other.UniqueTableId && ObjectID == other.ObjectID;
	}

	public override bool Equals(object obj)
	{
		if (obj is null)
		{
			return false;
		}

		if (ReferenceEquals(this, obj))
		{
			return true;
		}

		if (obj.GetType() != GetType())
		{
			return false;
		}

		return Equals((WorkItem) obj);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(UniqueTableId, GdbRowProxy);
	}

	#endregion

	public override string ToString()
	{
		return
			$"item id={OID}, row oid={ObjectID}, {GdbRowProxy.Table.Name}, {Status}, {Visited}";
	}
}
