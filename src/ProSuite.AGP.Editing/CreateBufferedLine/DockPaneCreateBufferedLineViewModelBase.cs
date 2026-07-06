using System.Windows.Controls;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Geom;

namespace ProSuite.AGP.Editing.CreateBufferedLine
{
	public class DockPaneCreateBufferedLineViewModelBase : DockPaneViewModelBase
	{
		protected DockPaneCreateBufferedLineViewModelBase()
		{
			RevertToDefaultsCommand = new RelayCommand(RevertToDefaults);
		}

		protected override Control CreateView()
		{
			return new DockPaneCreateBufferedLine();
		}

		#region RestoreDefaultsButton

		public ICommand RevertToDefaultsCommand { get; }

		public bool IsRevertToDefaultsEnabled => true;

		private void RevertToDefaults()
		{
			Options?.RevertToDefaults();
		}

		#endregion

		private string _heading = "Create Buffered Line Options";

		private CreateBufferedLineToolOptions _options;

		private CentralizableSettingViewModel<double> _bufferWidth;
		private CentralizableSettingViewModel<bool> _showBufferDistanceCircle;
		private CentralizableSettingViewModel<bool> _weed;
		private CentralizableSettingViewModel<double> _weedTolerance;
		private CentralizableSettingViewModel<bool> _enforceMinimumSegmentLength;
		private CentralizableSettingViewModel<double> _minimumSegmentLength;
		private CentralizableSettingViewModel<BufferSide> _bufferSide;

		public string Heading
		{
			get => _heading;
			set { SetProperty(ref _heading, value, () => Heading); }
		}

		public CentralizableSettingViewModel<double> BufferWidth
		{
			get => _bufferWidth;
			set => SetProperty(ref _bufferWidth, value);
		}

		public CentralizableSettingViewModel<bool> ShowBufferDistanceCircle
		{
			get => _showBufferDistanceCircle;
			set => SetProperty(ref _showBufferDistanceCircle, value);
		}

		public CentralizableSettingViewModel<bool> Weed
		{
			get => _weed;
			set => SetProperty(ref _weed, value);
		}

		public CentralizableSettingViewModel<double> WeedTolerance
		{
			get => _weedTolerance;
			set => SetProperty(ref _weedTolerance, value);
		}

		public CentralizableSettingViewModel<bool> EnforceMinimumSegmentLength
		{
			get => _enforceMinimumSegmentLength;
			set => SetProperty(ref _enforceMinimumSegmentLength, value);
		}

		public CentralizableSettingViewModel<double> MinimumSegmentLength
		{
			get => _minimumSegmentLength;
			set => SetProperty(ref _minimumSegmentLength, value);
		}

		public CentralizableSettingViewModel<BufferSide> BufferSide
		{
			get => _bufferSide;
			set => SetProperty(ref _bufferSide, value);
		}

		public CreateBufferedLineToolOptions Options
		{
			get => _options;
			set
			{
				SetProperty(ref _options, value);

				DisplayUnitInfo unit = DisplayUnitInfo.FromMap(MapView.Active?.Map);

				BufferWidth = new CentralizableSettingViewModel<double>(
					              Options.CentralizableBufferWidth)
				              {
					              Decimals = unit.Decimals, Step = unit.Step,
					              UnitLabel = unit.Label
				              };

				ShowBufferDistanceCircle = new CentralizableSettingViewModel<bool>(
					Options.CentralizableShowBufferDistanceCircle);

				Weed = new CentralizableSettingViewModel<bool>(Options.CentralizableWeed);

				WeedTolerance = new CentralizableSettingViewModel<double>(
					                Options.CentralizableWeedTolerance,
					                new[] { Options.CentralizableWeed })
				                {
					                Decimals = unit.Decimals, Step = unit.Step,
					                UnitLabel = unit.Label
				                };

				EnforceMinimumSegmentLength = new CentralizableSettingViewModel<bool>(
					Options.CentralizableEnforceMinimumSegmentLength);

				MinimumSegmentLength = new CentralizableSettingViewModel<double>(
					                       Options.CentralizableMinimumSegmentLength,
					                       new[]
					                       {
						                       Options
							                       .CentralizableEnforceMinimumSegmentLength
					                       })
				                       {
					                       Decimals = unit.Decimals, Step = unit.Step,
					                       UnitLabel = unit.Label
				                       };

				BufferSide = new CentralizableSettingViewModel<BufferSide>(
					Options.CentralizableBufferSide);
			}
		}
	}
}
