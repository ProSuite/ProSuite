using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.AGP.WorkList;

public class WorkListFilterToSelection : IWorkListFilterDefinitionExpression
{
	[NotNull] private readonly ISourceClass _sourceClass;

	public WorkListFilterToSelection([NotNull] WorkListFilterDefinition filterDefinition,
	                                 [NotNull] ISourceClass sourceClass)
	{
		ArgumentNullException.ThrowIfNull(filterDefinition);

		FilterDefinition = filterDefinition;
		_sourceClass = sourceClass;
	}

	[NotNull]
	public WorkListFilterDefinition FilterDefinition { get; }

	[CanBeNull]
	public string Expression
	{
		get => GetBy(_sourceClass);
		set { }
	}

	public override string ToString()
	{
		return $"{FilterDefinition.Name}: {Expression ?? "<null>"}";
	}

	[CanBeNull]
	private static string GetBy(ISourceClass sourceClass)
	{
		// Don't get selection on background thread (geometry service)
		// nor on gui thread.
		if (! QueuedTask.OnWorker)
		{
			return null;
		}

		Map activeMap = MapUtils.GetActiveMap();

		Dictionary<MapMember, List<long>> selection =
			SelectionUtils.GetSelection<MapMember>(activeMap);

		Dictionary<Table, List<long>> selectionByTable =
			SelectionUtils.GetSelectionByTable(selection);

		string whereClause =
			StringUtils.Concatenate(GetWhereClauseBatch(selectionByTable, sourceClass).ToList(),
			                        " OR ");

		if (MapUtils.HasSelection(activeMap) && string.IsNullOrEmpty(whereClause))
		{
			// there is a selection but not of this sourceClass
			return "1=2";
		}

		return whereClause;
	}

	private static IEnumerable<string> GetWhereClauseBatch(
		Dictionary<Table, List<long>> selectionByTable, ISourceClass sourceClass)
	{
		foreach ((Table table, List<long> oids) in selectionByTable)
		{
			if (! sourceClass.TableIdentity.ReferencesTable(table))
			{
				continue;
			}

			using var tableDefinition = table.GetDefinition();
			string oidFieldName = tableDefinition.GetObjectIDField();

			foreach (string oidBatch in GetOidBatches(oids))
			{
				yield return $"{oidFieldName} IN ({oidBatch})";
			}
		}
	}

	private static IEnumerable<string> GetOidBatches(List<long> oids, int maxRowCount = 1000)
	{
		foreach (IList<long> oidBatch in CollectionUtils.Split(oids, maxRowCount))
		{
			if (oidBatch.Count == 0)
			{
				continue;
			}

			yield return StringUtils.Concatenate(oidBatch, ", ");
		}
	}
}
