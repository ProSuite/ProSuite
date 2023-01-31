using System;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Events;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.Selection
{
	[UsedImplicitly]
	public abstract class SelectionToolButtonBase : Button
	{
		private void WireEvents()
		{
			ActiveToolChangedEvent.Subscribe(OnActiveToolChanged);
		}

		private void UnwireEvents()
		{
			ActiveToolChangedEvent.Unsubscribe(OnActiveToolChanged);
		}

		private void OnActiveToolChanged(ToolEventArgs e)
		{
			if (string.Equals("ProSuiteTools_Selection_SelectionTool", e.CurrentID,
			                  StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			UnwireEvents();

			IsChecked = false;
		}

		protected override void OnClick()
		{
			if (! (FrameworkApplication.GetPlugInWrapper(
				       "ProSuiteTools_Selection_SelectionTool") is ICommand selectionToolCmd))
			{
				return;
			}

			if (! selectionToolCmd.CanExecute(null))
			{
				return;
			}

			WireEvents();

			IsChecked = ! IsChecked;

			selectionToolCmd.Execute(null);
		}
	}
}
