using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.AGP.Framework;

namespace ProSuite.AGP.Editing.FillHole
{
	public abstract class DockPaneFillHoleViewModelBase : DockPaneViewModelBase
	{
		protected DockPaneFillHoleViewModelBase() : base(new DockPaneFillHole())
		{
			RevertToDefaultsCommand = new RelayCommand(RevertToDefaults);
		}

		#region RestoreDefaultsButton

		public ICommand RevertToDefaultsCommand { get; }

		public bool IsRevertToDefaultsEnabled => true;

		private void RevertToDefaults()
		{
			Options?.RevertToDefaults();
		}

		#endregion
			
		private string _heading = "Fill Hole Options";

		private HoleToolOptions _options;

		private CentralizableSettingViewModel<bool> _showPreview;
		private CentralizableSettingViewModel<bool> _limitPreviewToExtent;

		public string Heading
		{
			get => _heading;
			set { SetProperty(ref _heading, value, () => Heading); }
		}

		public CentralizableSettingViewModel<bool> ShowPreview
		{
			get => _showPreview;
			set { SetProperty(ref _showPreview, value); }
		}

		public CentralizableSettingViewModel<bool> LimitPreviewToExtent
		{
			get => _limitPreviewToExtent;
			set { SetProperty(ref _limitPreviewToExtent, value); }
		}


		public HoleToolOptions Options
		{
			get => _options;
			set
			{
				SetProperty(ref _options, value);

				ShowPreview = new CentralizableSettingViewModel<bool>(Options.CentralizableShowPreview);

				LimitPreviewToExtent = new CentralizableSettingViewModel<bool>(
					Options.CentralizableLimitPreviewToExtent,
					new[] { Options.CentralizableShowPreview });
			}
		}
	}
}
