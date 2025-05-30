using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AGP.QA;

namespace ProSuite.AGP.QA.ProPlugins
{
	public abstract class SelectInvolvedObjectsCmdBase : ButtonCommandBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected override async Task<bool> OnClickAsyncCore()
		{
			Map map = Assert.NotNull(MapView.Active?.Map, "No active map");

			if (! MapUtils.HasSelection(map))
			{
				_msg.Debug("No features or rows selected");
				return false;
			}

			long selectedCount = await QueuedTaskUtils.Run(() => SelectInvolvedRows(map));

			return selectedCount > 0;
		}

		private long SelectInvolvedRows([NotNull] Map map)
		{
			IEnumerable<IDisplayTable> displayTables = MapUtils.GetDisplayTables<IDisplayTable>(
				map.GetLayersAsFlattenedList(), null);

			long selectedCount = 0;
			foreach (IDisplayTable displayTable in displayTables)
			{
				Table issueTable = displayTable.GetTable();

				if (! IsIssueTable(issueTable, out bool fromProductionModel))
				{
					continue;
				}

				//get selected issue objects from issue layers
				List<Row> issueObjects = new List<Row>();

				if (displayTable is FeatureLayer featureLayer)
				{
					issueObjects.AddRange(SelectionUtils.GetSelectedFeatures(featureLayer));
				}
				else if (displayTable is StandaloneTable standaloneTable)
				{
					issueObjects.AddRange(StandaloneTableUtils.GetSelectedRows(standaloneTable));
				}

				IAttributeReader attributeReader =
					GetInvolvedObjectsAttributeReader(issueTable, fromProductionModel);

				_msg.DebugFormat("{0} issue objects selected from {1}", issueObjects.Count,
				                 issueTable.GetName());

				if (issueObjects.Count == 0)
				{
					continue;
				}

				// Getting involved rows (OIDs grouped by table) from issue objects:
				Dictionary<string, List<long>> involvedRows =
					GetInvolvedRows(issueObjects, attributeReader);

				_msg.DebugFormat("Involved rows found from {0} object classes.",
				                 involvedRows.Count);

				//select features or rows based on involved rows
				selectedCount += SelectRows(involvedRows, map);
			}

			return selectedCount;
		}

		private static long SelectRows([NotNull] Dictionary<string, List<long>> involvedRows,
		                               [NotNull] Map map)
		{
			long selectedCount = 0;

			foreach (KeyValuePair<string, List<long>> keyValuePair in involvedRows)
			{
				string tableName = keyValuePair.Key;
				List<long> objectIds = keyValuePair.Value;

				// TODO: More robust table comparison than just the name
				Predicate<IDisplayTable> layerPredicate = l => IsBasedOnTable(l, tableName);

				long selectedRowsForTable =
					SelectionUtils.SelectRows(map, layerPredicate, objectIds);

				if (selectedRowsForTable == 0)
				{
					_msg.Warn(
						$"No object selected for {tableName}. No visible and selectable layer " +
						$"contains the objects <oid> {StringUtils.Concatenate(objectIds, ", ")}");
				}

				selectedCount += selectedRowsForTable;
			}

			return selectedCount;
		}

		private static bool IsBasedOnTable([NotNull] IDisplayTable displayTable,
		                                   [NotNull] string tableName)
		{
			// TODO: More robust table comparison than just the name!

			string candidateName = displayTable.GetTable()?.GetName();

			if (string.IsNullOrEmpty(candidateName))
			{
				return false;
			}

			// Typically the datasets written to the involved rows are harvested with unqualified names.
			// But the layer typically references qualified dataset from the production model.
			if (ModelElementNameUtils.IsQualifiedName(candidateName) &&
			    ! ModelElementNameUtils.IsQualifiedName(tableName))
			{
				candidateName = ModelElementNameUtils.GetUnqualifiedName(candidateName);
			}

			// In issue tables the fully qualified table name is referenced - adapt for check-outs
			// with un-qualified candidate names:
			if (ModelElementNameUtils.IsQualifiedName(tableName) &&
			    ! ModelElementNameUtils.IsQualifiedName(candidateName))
			{
				tableName = ModelElementNameUtils.GetUnqualifiedName(tableName);
			}

			return string.Equals(candidateName, tableName, StringComparison.OrdinalIgnoreCase);
		}

		private static Dictionary<string, List<long>> GetInvolvedRows(
			[NotNull] List<Row> issueObjects,
			[NotNull] IAttributeReader attributeReader)
		{
			Dictionary<string, List<long>> involvedRows = new Dictionary<string, List<long>>();

			foreach (Row issueObject in issueObjects)
			{
				string involvedObjectsString =
					attributeReader.GetValue<string>(issueObject, Attributes.InvolvedObjects);

				if (string.IsNullOrEmpty(involvedObjectsString))
				{
					_msg.DebugFormat("Empty involved objects value in issue {0}",
					                 GdbObjectUtils.ToString(issueObject));
					continue;
				}

				IList<InvolvedTable> involvedTables =
					attributeReader.ParseInvolved(involvedObjectsString, issueObject is Feature);

				foreach (InvolvedTable involved in involvedTables)
				{
					if (! involvedRows.ContainsKey(involved.TableName))
					{
						involvedRows.Add(involved.TableName, new List<long>());
					}

					involvedRows[involved.TableName]
						.AddRange(involved.RowReferences.Select(rr => (long) rr.OID));
				}
			}

			return involvedRows;
		}

		protected virtual IAttributeReader GetInvolvedObjectsAttributeReader(
			[NotNull] Table issueTable,
			bool fromProductionModel)
		{
			if (fromProductionModel)
			{
				return null;
			}

			return new AttributeReader(issueTable.GetDefinition(), Attributes.InvolvedObjects);
		}

		protected virtual bool IsIssueTable([NotNull] Table candidate,
		                                    out bool fromProductionModel)
		{
			// This is very hacky and should be improved:
			fromProductionModel = false;
			return IssueGdbSchema.IssueFeatureClassNames.Contains(candidate.GetName());
		}
	}
}
