using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Text;

namespace ProSuite.AGP.WorkList;

// todo daro: rename to WorkItemClass?
public abstract class SourceClass : ISourceClass
{
	private readonly GdbTableIdentity _tableIdentity;

	protected SourceClass(GdbTableIdentity tableIdentity,
	                      IAttributeReader attributeReader = null)
	{
		_tableIdentity = tableIdentity;
		AttributeReader = attributeReader;
	}

	public GdbTableIdentity TableIdentity => _tableIdentity;

	public bool HasGeometry => _tableIdentity.HasGeometry;

	protected long ArcGISTableId => _tableIdentity.Id;

	public string Name => _tableIdentity.Name;

	public IAttributeReader AttributeReader { get; set; }

	public string DefaultDefinitionQuery { get; protected init; }

	/// <summary>
	/// Ensures the filter is valid with the correct subfields. This method is called by Pro and we cannot
	/// control whether it's called with a SpatialQueryFilter or a QueryFilter. Querying a table with a
	/// spatial filter throws an exception, so we clone the filter to the correct type.
	/// </summary>
	/// <param name="filter"></param>
	/// <param name="ignoreDefinitionQuery"></param>
	public void EnsureValidFilter(ref QueryFilter filter, bool ignoreDefinitionQuery = false)
	{
		QueryFilter result;

		string relevantSubFields = GetRelevantSubFields();

		List<string> subfields =
			StringUtils.SplitAndTrim(relevantSubFields, ",");

		if (HasGeometry && filter is SpatialQueryFilter)
		{
			result = GdbQueryUtils.CloneFilter<SpatialQueryFilter>(filter);
		}
		else
		{
			result = GdbQueryUtils.CloneFilter<QueryFilter>(filter);
		}

		if (GdbQueryUtils.EnsureSubFields(subfields, result.SubFields, out string newSubFields))
		{
			result.SubFields = newSubFields;
		}
		else
		{
			// filter.Subfields.Equals("*")
			result.SubFields = relevantSubFields;
		}

		EnsureValidFilterCore(ref result, ignoreDefinitionQuery);

		filter = result;
	}

	public bool Uses(ITableReference tableReference)
	{
		return tableReference.ReferencesTable(_tableIdentity.Id, _tableIdentity.Name);
	}

	public abstract bool Contains(Row row);

	public virtual T CreateWorkItem<T>(Row row) where T : IWorkItem
	{
		IWorkItem item = new WorkItem(GetUniqueTableId(),
		                              new GdbRowIdentity(row.GetObjectID(), TableIdentity));
		return (T) item;
	}

	public T OpenDataset<T>() where T : Table
	{
		Datastore datastore = TableIdentity.Workspace.OpenDatastore();

		if (datastore is Geodatabase geodatabase)
		{
			return geodatabase.OpenDataset<T>(_tableIdentity.Name);
		}

		if (datastore is FileSystemDatastore fsDatastore)
		{
			return fsDatastore.OpenDataset<T>(_tableIdentity.Name);
		}

		if (datastore is PluginDatastore plugin)
		{
			return (T) plugin.OpenTable(_tableIdentity.Name);
		}

		throw new NotSupportedException(
			$"Datastore {datastore} type is not supported ");
	}

	public abstract long GetUniqueTableId();

	public override string ToString()
	{
		return string.IsNullOrEmpty(DefaultDefinitionQuery)
			       ? Name
			       : $"{Name}, {DefaultDefinitionQuery}";
	}

	private string GetRelevantSubFields()
	{
		return GetRelevantSubFieldsCore();
	}

	protected virtual string GetRelevantSubFieldsCore()
	{
		return string.Empty;
	}

	protected virtual void EnsureValidFilterCore(ref QueryFilter filter, bool ignoreDefinitionQuery)
	{
		if (ignoreDefinitionQuery)
		{
			return;
		}

		GdbQueryUtils.AppendWhereClause(ref filter, DefaultDefinitionQuery);
	}
}
