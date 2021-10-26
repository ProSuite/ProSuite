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
using ProSuite.Commons.AGP.WPF;
using ProSuite.Commons.Logging;
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
			MapView mapView = MapView.Active;

			if (! MapUtils.HasSelection(mapView))
			{
				_msg.Debug("No selected features");
				return;
			}

			//get selected issue features from issue layers
			List<Row> issueObjects = new List<Row>();

			var selection = await QueuedTask.Run(
				                () => SelectionUtils.GetSelectedFeatures(mapView)
				                                    .Cast<Row>().ToList());

			foreach (Row row in selection)
			{
				var table = row.GetTable();

				if (IssueGdbSchema.IssueFeatureClassNames.Contains(table.GetName()))
				{
					issueObjects.Add(row);
				}
			}

			//get involved rows from issue features
			Dictionary<string, List<long>> involvedRows = new Dictionary<string, List<long>>();

			foreach (Row issueObject in issueObjects)
			{
				int fieldIndex =
					issueObject.GetTable().GetDefinition().FindField("InvolvedObjects");

				string involvedString = (string) issueObject[fieldIndex];

				var involvedTables = IssueUtils.ParseInvolvedTables(involvedString);
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

			//select features based on involved rows
			foreach (KeyValuePair<string, List<long>> keyValuePair in involvedRows)
			{
				FeatureLayer layer =
					MapUtils.GetLayers<FeatureLayer>(
						lyr => string.Equals(
							lyr.GetFeatureClass().GetName(),
							keyValuePair.Key,
							StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

				await QueuedTask.Run(
					() =>
						layer?.Select(
							new QueryFilter { ObjectIDs = keyValuePair.Value },
							SelectionCombinationMethod.Add)
				);
			}
		}
	}
}
