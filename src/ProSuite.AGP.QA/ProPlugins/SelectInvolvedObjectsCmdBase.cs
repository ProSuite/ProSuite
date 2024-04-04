using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.Commons.UI;
using ProSuite.DomainModel.AGP.QA;

namespace ProSuite.AGP.QA.ProPlugins
{
	public abstract class SelectInvolvedObjectsCmdBase : Button
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected SelectInvolvedObjectsCmdBase()
		{
			//Anything to do here?
		}

		protected override async void OnClick()
		{
			await ViewUtils.TryAsync(QueuedTask.Run(OnClickCore), _msg);
		}

		private static async Task OnClickCore()
		{
			Map map = MapView.Active?.Map;

			if (! MapUtils.HasSelection(map))
			{
				_msg.Debug("No features or rows selected");
				return;
			}

			//get selected issue objects from issue layers
			List<Row> issueObjects = new List<Row>();

			IEnumerable<FeatureLayer> layers = MapUtils.GetFeatureLayers<FeatureLayer>(
				map,
				fl => IssueGdbSchema.IssueFeatureClassNames.Contains(
					fl.GetFeatureClass().GetName()));

			foreach (FeatureLayer layer in layers)
			{
				issueObjects.AddRange(
					await QueuedTask.Run(() => SelectionUtils.GetSelectedFeatures(layer)));
			}

			var tables = MapUtils.GetStandaloneTables(
				map,
				tbl => IssueGdbSchema.IssueFeatureClassNames.Contains(
					tbl.GetTable().GetName()));

			foreach (StandaloneTable table in tables)
			{
				issueObjects.AddRange(
					await QueuedTask.Run(() => StandaloneTableUtils.GetSelectedRows(table)));
			}

			_msg.DebugFormat("{0} issue objects selected", issueObjects.Count);

			if (issueObjects.Count == 0)
			{
				return;
			}

			//get involved rows (OIDs grouped by table) from issue objects
			Dictionary<string, List<long>> involvedRows = new Dictionary<string, List<long>>();

			foreach (Row issueObject in issueObjects)
			{
				int fieldIndex =
					issueObject.GetTable().GetDefinition().FindField("InvolvedObjects");

				string involvedString = (string) issueObject[fieldIndex];

				// todo daro see IssueWorkListViewModel.GetInvolvedMapMembersByLayer
				// it is the same problem.

				var involvedTables =
					IssueUtils.ParseInvolvedTables(involvedString, issueObject is Feature);
				foreach (var involved in involvedTables)
				{
					if (! involvedRows.ContainsKey(involved.TableName))
					{
						involvedRows.Add(involved.TableName, new List<long>());
					}

					involvedRows[involved.TableName]
						.AddRange(involved.RowReferences.Select(rR => (long) rR.OID));
				}
			}

			_msg.DebugFormat("Involved rows found from {0} object classes.", involvedRows.Count);

			//select features or rows based on involved rows
			foreach (KeyValuePair<string, List<long>> keyValuePair in involvedRows)
			{
				string tableName = keyValuePair.Key;
				List<long> objectIds = keyValuePair.Value;

				// TODO: More robust table comparison than just the name
				Predicate<IDisplayTable> layerPredicate =
					l => string.Equals(l.GetTable()?.GetName(), tableName,
					                   StringComparison.OrdinalIgnoreCase);

				long selectedCount = SelectionUtils.SelectRows(map, layerPredicate, objectIds);

				if (selectedCount == 0)
				{
					_msg.Warn(
						$"No object selected for {tableName}. No visible and selectable layer " +
						$"contains the objects <oid> {StringUtils.Concatenate(objectIds, ", ")}");
				}
			}
		}
	}
}
