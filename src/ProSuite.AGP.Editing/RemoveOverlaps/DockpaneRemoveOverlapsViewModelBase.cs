using System.Windows;
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

		public TargetFeatureSelectionViewModel TargetFeatureSelectionVM
		{
			get => _targetFeatureSelectionVm;
			private set => SetProperty(ref _targetFeatureSelectionVm, value);
		}

		public ICommand RevertToDefaultsCommand { get; }

		public bool IsRevertToDefaultsEnabled => true;

		private void RevertToDefaults()
		{
			Options?.RevertToDefaults();
		}

		#endregion

		/// Text shown near the top of the DockPane.
		private string _heading = "Remove Overlaps Options";

		private RemoveOverlapsToolOptions _options;
		private CentralizableSettingViewModel<bool> _limitOverlapCalculationToExtent;
		private CentralizableSettingViewModel<bool> _explodeMultipartResults;
		private CentralizableSettingViewModel<bool> _InsertVerticesInTarget;

		private TargetFeatureSelectionViewModel _targetFeatureSelectionVm;

		public string Heading
		{
			get => _heading;
			set { SetProperty(ref _heading, value, () => Heading); }
		}

		public CentralizableSettingViewModel<bool> LimitOverlapCalculationToExtent
		{
			get => _limitOverlapCalculationToExtent;
			set => SetProperty(ref _limitOverlapCalculationToExtent, value);
		}

		public CentralizableSettingViewModel<bool> ExplodeMultipartResults
		{
			get => _explodeMultipartResults;
			set => SetProperty(ref _explodeMultipartResults, value);
		}

		public CentralizableSettingViewModel<bool> InsertVerticesInTarget
		{
			get => _InsertVerticesInTarget;
			set => SetProperty(ref _InsertVerticesInTarget, value);
		}

		public RemoveOverlapsToolOptions Options
		{
			get => _options;
			set
			{
				SetProperty(ref _options, value);

				LimitOverlapCalculationToExtent =
					new CentralizableSettingViewModel<bool>(
						Options.CentralizableLimitOverlapCalculationToExtent);
				ExplodeMultipartResults =
					new CentralizableSettingViewModel<bool>(
						Options.CentralizableExplodeMultipartResults);
				InsertVerticesInTarget =
					new CentralizableSettingViewModel<bool>(
						Options.CentralizableInsertVerticesInTarget);

				TargetFeatureSelectionVM =
					new TargetFeatureSelectionViewModel(
						_options.CentralizableTargetFeatureSelection)
					{
						SelectedFeaturesVisibility = Visibility.Collapsed
					};
			}
		}
	}
}
