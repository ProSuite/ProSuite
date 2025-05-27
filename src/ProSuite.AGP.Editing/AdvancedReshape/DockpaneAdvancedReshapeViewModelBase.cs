using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.AGP.Framework;

namespace ProSuite.AGP.Editing.AdvancedReshape
{
	public abstract class DockPaneAdvancedReshapeViewModelBase : DockPaneViewModelBase
	{
		protected DockPaneAdvancedReshapeViewModelBase() : base(new DockPaneAdvancedReshape())
		{
			RevertToDefaultsCommand = new RelayCommand(RevertToDefaults);
		}
		#region RestoreDefaultsButton

		public TargetFeatureSelectionViewModel TargetFeatureSelectionVM { get; private set; }


		public ICommand RevertToDefaultsCommand { get; }

		public bool IsRevertToDefaultsEnabled => true;

		private void RevertToDefaults()
		{
			Options?.RevertToDefaults();
		}
		#endregion

		private string _heading = "Reshape Options";

		private ReshapeToolOptions _options;
		private CentralizableSettingViewModel<bool> _remainInSketchMode;
		private CentralizableSettingViewModel<bool> _showPreview;
		private CentralizableSettingViewModel<bool> _moveOpenJawEndJunction;

		public string Heading
		{
			get { return _heading; }
			set { SetProperty(ref _heading, value, () => Heading); }
		}

		public CentralizableSettingViewModel<bool> RemainInSketchMode
		{
			get => _remainInSketchMode;
			set => SetProperty(ref _remainInSketchMode, value);
		}

		public CentralizableSettingViewModel<bool> ShowPreview
		{
			get => _showPreview;
			set => SetProperty(ref _showPreview, value);
		}

		public CentralizableSettingViewModel<bool> MoveOpenJawEndJunction
		{
			get => _moveOpenJawEndJunction;
			set => SetProperty(ref _moveOpenJawEndJunction, value);
		}

		public ReshapeToolOptions Options
		{
			get { return _options; }
			set
			{
				SetProperty(ref _options, value);

				RemainInSketchMode =
					new CentralizableSettingViewModel<bool>(
						Options.CentralizableRemainInSketchMode);
				ShowPreview =
					new CentralizableSettingViewModel<bool>(
						Options.CentralizableShowPreview);
				MoveOpenJawEndJunction =
					new CentralizableSettingViewModel<bool>(
						Options.CentralizableMoveOpenJawEndJunction);
			}
		}


	}
}
