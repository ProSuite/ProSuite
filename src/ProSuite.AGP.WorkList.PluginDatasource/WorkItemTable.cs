using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Core.Geometry;

namespace ProSuite.AGP.WorkList.PluginDatasource
{
	// TODO Temporarily moved to App/ProSuite.AGP.WorkListDatasource

	//public class WorkItemTable : PluginTableTemplate, IDisposable
	//{
	//	private readonly IWorkItemService<WorkItem> _service;
	//	private readonly GeometryType _geometryType;
	//	private readonly IReadOnlyList<PluginField> _fields;

	//	// todo daro: how many times invoked?
	//	public WorkItemTable(IWorkItemService<WorkItem> service, GeometryType geometryType)
	//	{
	//		_service = service;
	//		_geometryType = geometryType;

	//		_fields = new ReadOnlyCollection<PluginField>(_service.GetFields().ToList());

	//		//_fields = new ReadOnlyCollection<PluginField>(
	//		//	new[]
	//		//	{
	//		//		CreateField("OBJECTID", FieldType.OID),
	//		//		CreateField("ORIGINAL_OID", FieldType.Integer),
	//		//		CreateField("CURRENT", FieldType.Integer),
	//		//		CreateField("STATUS", FieldType.Integer),
	//		//		CreateField("VISITED", FieldType.Integer),
	//		//		CreateField("SHAPE", FieldType.Geometry)
	//		//	});
	//	}

	//	/// <summary>
	//	/// Get the collection of fields accessible on the plugin table
	//	/// </summary>
	//	/// <remarks>The order of returned columns in any rows must match the
	//	/// order of the fields specified from GetFields()</remarks>
	//	public override IReadOnlyList<PluginField> GetFields()
	//	{
	//		return _fields;
	//	}

	//	public override Envelope GetExtent()
	//	{
	//		return _service.GetExtent();
	//	}

	//	public override GeometryType GetShapeType()
	//	{
	//		return _geometryType;
	//	}

	//	/// <summary>
	//	/// The reason why concrete implementations of this abstraction should overwrite this
	//	/// method and IsNativeRowCountSupported (if the underlying data source supports native row count)
	//	/// is strictly for performance reasons. If IsNativeRowCountSupported returns false and a row count
	//	/// is needed for the plug-in table, the framework will call the Search method on the entire plug-in
	//	/// table and then manually iterate through the cursor to determine the number of rows.
	//	/// The framework will not call this method if IsNativeRowCountSupported return false.
	//	/// </summary>
	//	public override bool IsNativeRowCountSupported()
	//	{
	//		return false;
	//	}

	//	public override int GetNativeRowCount()
	//	{
	//		throw new NotSupportedException();
	//	}

	//	public override string GetName()
	//	{
	//		return $"{_service.Name}:${_geometryType}";
	//	}

	//	public override PluginCursorTemplate Search(QueryFilter queryFilter)
	//	{
	//		var enumerable = _service.GetValues(queryFilter, recycle : true, _geometryType);
	//		return new WorkItemCursor(enumerable);
	//	}

	//	public override PluginCursorTemplate Search(SpatialQueryFilter spatialQueryFilter)
	//	{
	//		return Search((QueryFilter) spatialQueryFilter);
	//	}

	//	public void Dispose()
	//	{
	//		_service?.Dispose();
	//	}
	//}
}
