using System;
using System.Linq;
using System.Reflection;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Logging;

namespace Clients.AGP.ProSuiteSolution.WorkListTrials
{
	internal class ButtonCreateTestList : Button
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		protected override void OnClick()
		{
			QueuedTask.Run(() => { CreateTestList(); });
		}

		private void CreateTestList()
		{
			_msg.Debug("Test: Debug");
			_msg.Info("Test: Info");
			_msg.Warn("Test: Warn");
			_msg.Error("Test: Error");
			_msg.VerboseDebug("Test: Verbose Debug");

			var workList = WorkListTrialsModule.Current.GetTestWorkList();
			var workListName = workList.Name;

			var connector = WorkListTrialsModule.Current.GetWorkListConnectionPath(workListName);

			using (var datastore = new PluginDatastore(connector))
			{
				var tableNames = datastore.GetTableNames();
				foreach (var tableName in tableNames)
				{
					using (var table = datastore.OpenTable(tableName))
					{
						LayerFactory.Instance.CreateFeatureLayer(
							(FeatureClass) table, MapView.Active.Map);
					}
				}
			}
		}
	}

	internal class ButtonTestItemDone : Button
	{
		protected override void OnClick()
		{
			QueuedTask.Run(() => SetTestItemDone());
		}

		private void SetTestItemDone()
		{
			var workList = WorkListTrialsModule.Current.GetTestWorkList();

			var current = workList.Current;
			if (current != null)
			{
				// note daro: should be done by IWorkListRepository (called by IWorkList)
				//current.SetDone();

				WorkListTrialsModule.Current.Refresh();
			}
		}
	}

	internal class WorkListVisibilityComboBox : ComboBox
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private bool _isInitialized;

		public WorkListVisibilityComboBox()
		{
			UpdateCombo();
		}

		private void UpdateCombo()
		{
			// TODO – customize this method to populate the combobox with your desired items  
			if (_isInitialized)
				SelectedItem = ItemCollection.FirstOrDefault(); //set the default item in the comboBox


			if (!_isInitialized)
			{
				Clear();

				Add(new ComboBoxItem(WorkItemVisibility.Todo.ToString()));
				Add(new ComboBoxItem(WorkItemVisibility.Done.ToString()));
				Add(new ComboBoxItem(WorkItemVisibility.All.ToString()));

				_isInitialized = true;
			}

			Enabled = true; //enables the ComboBox
			SelectedItem = ItemCollection.FirstOrDefault(); //set the default item in the comboBox
		}

		/// <summary>
		/// The on comboBox selection change event. 
		/// </summary>
		/// <param name="item">The newly selected combo box item</param>
		protected override void OnSelectionChange(ComboBoxItem item)
		{
			if (item == null) return;
			if (string.IsNullOrEmpty(item.Text)) return;

			var workList = WorkListTrialsModule.Current.GetTestWorkList();

			const bool ignoreCase = true;
			if (Enum.TryParse<WorkItemVisibility>(item.Text, ignoreCase, out var value))
			{
				workList.Visibility = value;
				QueuedTask.Run(() => WorkListTrialsModule.Current.Refresh());
			}
			else
			{
				_msg.WarnFormat("Cannot parse '{0}' as {1}", item.Text, nameof(WorkItemVisibility));
			}
		}
	}

	internal class SplitButtonTestListNav_GoNext : Button
	{
		protected override void OnClick()
		{
			var workList = WorkListTrialsModule.Current.GetTestWorkList();
			workList.GoNext();
			//workList.Current?.SetVisited();
			QueuedTask.Run(() => WorkListTrialsModule.Current.Refresh());
		} 
	}

	internal class SplitButtonTestListNav_GoPrevious : Button
	{
		protected override void OnClick()
		{
			var workList = WorkListTrialsModule.Current.GetTestWorkList();
			workList.GoPrevious();
			//workList.Current?.SetVisited();
			QueuedTask.Run(() => WorkListTrialsModule.Current.Refresh());
		}
	}

	internal class SplitButtonTestListNav_GoFirst : Button
	{
		protected override void OnClick()
		{
			var workList = WorkListTrialsModule.Current.GetTestWorkList();
			workList.GoFirst();
			//workList.Current?.SetVisited();
			QueuedTask.Run(() => WorkListTrialsModule.Current.Refresh());
		}
	}
}
