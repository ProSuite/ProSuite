using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.Commons.AGP.Framework;
using ArcGIS.Desktop.Framework.Controls;
using ProSuite.Commons.UI.ManagedOptions;

namespace ProSuite.AGP.Editing.ChangeAlong
{
	public abstract class DockPaneReshapeAlongViewModelBase : DockPane
	{
		private ReshapeAlongToolOptions _options;

		public DockPaneReshapeAlongViewModelBase() : base()
		{
		}

		public ReshapeAlongToolOptions Options
		{
			get => _options;
			set
			{
				SetProperty(ref _options, value, () => Options);
				NotifyPropertyChanged();
			}
		}
	}
}
