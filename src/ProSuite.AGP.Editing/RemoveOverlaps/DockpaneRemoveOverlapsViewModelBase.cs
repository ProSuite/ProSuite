using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.AGP.Core.GeometryProcessing.RemoveOverlaps;
using ProSuite.Commons.AGP.Framework;

namespace ProSuite.AGP.Editing.RemoveOverlaps
{
	public abstract class DockPaneRemoveOverlapsViewModelBase : DockPaneViewModelBase
	{
		protected DockPaneRemoveOverlapsViewModelBase() : base(new DockPaneRemoveOverlaps())
		{
			RevertToDefaultsCommand = new RelayCommand(RevertToDefaults);
		}

		#region RestoreDefaultsButton

		public TargetFeatureSelectionViewModel TargetFeatureSelectionVM { get; private set; }

		public ICommand RevertToDefaultsCommand { get; }

		public bool IsButtonEnabled => _options?.CentralOptions != null;

		private void RevertToDefaults()
		{
			Options?.RevertToDefaults();
		}

		#endregion

		/// Text shown near the top of the DockPane.
		private string _heading = "Remove Overlaps Options";

		private RemoveOverlapsOptions _options;

		public string Heading
		{
			get => _heading;
			set { SetProperty(ref _heading, value, () => Heading); }
		}

		public RemoveOverlapsOptions Options
		{
			get => _options;
			set
			{
				SetProperty(ref _options, value);

				TargetFeatureSelectionVM =
					new TargetFeatureSelectionViewModel(
						_options.CentralizableTargetFeatureSelection);
			}
		}
	}
}
