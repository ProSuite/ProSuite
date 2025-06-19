using System;
using System.Windows.Controls;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Framework
{
	public abstract class DockPaneViewModelBase : DockPane
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly Control _contentControl;

		protected DockPaneViewModelBase([NotNull] Control contentControl)
		{
			_contentControl =
				contentControl ?? throw new ArgumentNullException(nameof(contentControl));
		}

		protected override Control OnCreateContent()
		{
			_contentControl.DataContext = this;

			return _contentControl;
		}

		/// <summary>
		/// Gets the OperationManager associated with the current map or null
		/// </summary>
		public override OperationManager OperationManager => MapView.Active?.Map.OperationManager;

		protected override void OnShow(bool isVisible)
		{
			try
			{
				OnShowCore(isVisible);
			}
			catch (Exception ex)
			{
				_msg.Error(ex.Message);
			}
		}

		protected virtual void OnShowCore(bool isVisible) {}

		protected override void OnHidden()
		{
			try
			{
				OnHiddenCore();
			}
			catch (Exception ex)
			{
				_msg.Error(ex.Message);
			}
		}

		protected virtual void OnHiddenCore() {}
	}
}
