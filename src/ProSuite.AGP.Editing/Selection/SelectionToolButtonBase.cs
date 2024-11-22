using System;
using System.Windows.Input;
using ArcGIS.Core.Events;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Events;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing.Selection
{
	/// <summary>
	/// Activate the Selection Tool on click. Can be used
	/// in places (e.g. context menus) where tools cannot.
	/// </summary>
	[UsedImplicitly]
	public abstract class SelectionToolButtonBase : Button
	{
		private SubscriptionToken _activeToolChangedToken;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected abstract string SelectionToolDamlID { get; }

		protected override void OnClick()
		{
			Gateway.LogEntry(_msg);

			try
			{
				var wrapper = FrameworkApplication.GetPlugInWrapper(SelectionToolDamlID);

				if (wrapper is not ICommand selectionToolCmd)
				{
					return;
				}

				if (! selectionToolCmd.CanExecute(null))
				{
					return;
				}

				WireEvents();

				IsChecked = true;

				selectionToolCmd.Execute(null);
			}
			catch (Exception ex)
			{
				Gateway.ShowError(ex, _msg);
			}
		}

		private void OnActiveToolChanged(ToolEventArgs e)
		{
			if (string.Equals(SelectionToolDamlID, e.CurrentID,
			                  StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			UnwireEvents();

			IsChecked = false;
		}

		private void WireEvents()
		{
			if (_activeToolChangedToken == null)
			{
				_activeToolChangedToken = ActiveToolChangedEvent.Subscribe(OnActiveToolChanged);
			}
		}

		private void UnwireEvents()
		{
			if (_activeToolChangedToken != null)
			{
				ActiveToolChangedEvent.Unsubscribe(_activeToolChangedToken);
				_activeToolChangedToken = null;
			}
		}
	}
}
