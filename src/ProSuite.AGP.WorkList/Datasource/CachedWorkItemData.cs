using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.SpatialIndex;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList.Datasource;

/// <summary>
/// Represents a lightweight, in-memory cached work item data source based on a (XML) persisted
/// work list definition. This implementation is used for display purposes of work list layers
/// in the map, before the work list is actually opened in the navigator. It does not require
/// a connection to the underlying geodatabase or other metadata.
/// Its purpose is to provide a self-sufficient representation of the work items only based on
/// the definition file referenced in the <see cref="WorkListDatasourceBase"/>. It shall be
/// replaced with the live DB-based once the work list is opened in the navigator, or possibly
/// earlier if eager caching is started once the additional application metadata comes online.
/// </summary>
public class CachedWorkItemData : IWorkItemData
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private readonly Dictionary<long, GdbTableIdentity> _tablesById;
	private readonly SpatialHashSearcher<XmlWorkItemState> _spatialSearcher;

	public CachedWorkItemData(XmlWorkListDefinition workListDefinition)
	{
		Assert.ArgumentNotNull(workListDefinition, nameof(workListDefinition));
		Assert.ArgumentCondition(! string.IsNullOrEmpty(workListDefinition.Name),
		                         "Work list has no name");

		Name = workListDefinition.Name;

		DisplayName = workListDefinition.DisplayName;

		// Extent is null in work lists saved in previous versions
		if (workListDefinition.Extent != null)
		{
			Extent = EnvelopeBuilderEx.FromXml(workListDefinition.Extent);
		}

		Stopwatch watch = Stopwatch.StartNew();

		if (workListDefinition.Items?.Count > 0)
		{
			try
			{
				_spatialSearcher = SpatialHashSearcher<XmlWorkItemState>.CreateSpatialSearcher(
					workListDefinition.Items,
					xmlItem =>
						new EnvelopeXY(xmlItem.XMin, xmlItem.YMin, xmlItem.XMax, xmlItem.YMax));
			}
			catch (Exception e)
			{
				// TODO: Investigate case when all items have 0 extent at coordinate 0/0.
				_msg.Warn(
					$"Work list {workListDefinition.DisplayName} ({workListDefinition.Name}):" +
					$" Cannot create cached work items. Open Work List Navigator to see work items",
					e);
			}
		}

		_tablesById = new Dictionary<long, GdbTableIdentity>();

		PopulateTableIdentities(workListDefinition);

		_msg.DebugStopTiming(watch, "Created CachedWorkItemData for '{0}' with {1} items.", Name,
		                     workListDefinition.Items?.Count);
	}

	private void PopulateTableIdentities(XmlWorkListDefinition workListDefinition)
	{
		foreach (XmlWorkListWorkspace xmlWorkListWorkspace in workListDefinition.Workspaces)
		{
			if (! Enum.TryParse(xmlWorkListWorkspace.WorkspaceFactory,
			                    out WorkspaceFactory result))
			{
				throw new InvalidOperationException(
					$"Cannot parse {xmlWorkListWorkspace.WorkspaceFactory}");
			}

			Connector connector = WorkspaceUtils.CreateConnector(
				result, xmlWorkListWorkspace.ConnectionString);

			GdbWorkspaceIdentity workspace =
				new GdbWorkspaceIdentity(connector, xmlWorkListWorkspace.ConnectionString);

			foreach (XmlTableReference xmlTable in xmlWorkListWorkspace.Tables)
			{
				var tableIdentity = new GdbTableIdentity(xmlTable.Name, xmlTable.Id, workspace);

				_tablesById[tableIdentity.Id] = tableIdentity;
			}
		}
	}

	#region Implementation of IWorkItemData

	public string Name { get; }

	public string DisplayName { get; set; }

	public Envelope Extent { get; private set; }

	public IEnumerable<IWorkItem> Search(QueryFilter filter)
	{
		if (filter is SpatialQueryFilter spatialFilter)
		{
			return Search(spatialFilter);
		}

		if (_spatialSearcher == null)
		{
			return Enumerable.Empty<IWorkItem>();
		}

		SpatialReference spatialReference = Extent.SpatialReference;

		if (filter.ObjectIDs.Count == 0)
		{
			return _spatialSearcher.Select(xmlItem => ToWorkItem(xmlItem, spatialReference));
		}

		List<long> oids = filter.ObjectIDs.OrderBy(oid => oid).ToList();
		return _spatialSearcher.Where(item => oids.BinarySearch(item.OID) >= 0)
		                       .Select(xmlItem => ToWorkItem(xmlItem, spatialReference));
	}

	public IEnumerable<IWorkItem> Search([CanBeNull] SpatialQueryFilter filter)
	{
		if (_spatialSearcher == null)
		{
			return Enumerable.Empty<IWorkItem>();
		}

		WorkItemStatus? currentVisibility = WorkItemStatus.Todo;

		Predicate<IWorkItemState> predicate = item => item.Status == currentVisibility;

		Geometry filterGeometry = filter?.FilterGeometry;
		if (filterGeometry == null || filterGeometry.IsEmpty)
		{
			return _spatialSearcher.Where(item => predicate(item))
			                       .Select(xmlItem => ToWorkItem(xmlItem, Extent.SpatialReference));
		}

		Envelope extent = filterGeometry.Extent;

		double tolerance = filterGeometry.SpatialReference?.XYTolerance ?? 0;
		return _spatialSearcher.Search(extent.XMin, extent.YMin,
		                               extent.XMax, extent.YMax,
		                               tolerance, predicate)
		                       .Select(xmlItem => ToWorkItem(xmlItem, Extent.SpatialReference));
	}

	public Geometry GetItemDisplayGeometry(IWorkItem item)
	{
		return ! item.HasExtent
			       ? null
			       : PolygonBuilderEx.CreatePolygon(item.Extent, item.Extent!.SpatialReference);
	}

	public IWorkItem CurrentItem => null;

	#endregion

	private CachedWorkItem ToWorkItem(XmlWorkItemState xmlItem,
	                                  SpatialReference spatialReference)
	{
		if (! _tablesById.TryGetValue(xmlItem.Row.TableId, out GdbTableIdentity tableIdentity))
		{
			throw new InvalidOperationException(
				$"Table with id {xmlItem.Row.TableId} not found in work list definition");
		}

		var gdbRowIdentity = new GdbRowIdentity(xmlItem.Row.OID, tableIdentity);

		var result = new CachedWorkItem(xmlItem.OID, gdbRowIdentity);

		result.SetExtent(
			EnvelopeBuilderEx.CreateEnvelope(xmlItem.XMin, xmlItem.YMin, xmlItem.XMax, xmlItem.YMax,
			                                 spatialReference));

		result.Status = xmlItem.Status;
		result.Visited = xmlItem.Visited;

		return result;
	}
}
