using System.Windows.Controls;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Geom;

namespace ProSuite.AGP.Editing.CreateBufferedLine
{
	/// <summary>
	/// The non-generic part of the buffered-line options dock pane view model. It carries the
	/// bound properties (used by the XAML at design and run time) and the restore-defaults
	/// command; the strongly typed <c>Options</c> live on
	/// <see cref="DockPaneCreateBufferedLineViewModelBase{TOptions,TPartial}"/>.
	/// </summary>
	public abstract class DockPaneCreateBufferedLineViewModelBase : DockPaneViewModelBase
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

		protected abstract void RevertToDefaults();

		#endregion

		private string _heading = "Create Buffered Line Options";

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
	}

	/// <summary>
	/// The strongly typed buffered-line options dock pane view model. It is generic over the
	/// tool's options and partial-options type so the wall tool (and any future buffered-line
	/// derivative) can reuse the same dock pane logic with its own options type.
	/// </summary>
	public abstract class DockPaneCreateBufferedLineViewModelBase<TOptions, TPartial>
		: DockPaneCreateBufferedLineViewModelBase
		where TOptions : BufferedLineToolOptionsBase<TPartial>
		where TPartial : PartialCreateBufferedLineOptions, new()
	{
		private TOptions _options;

		protected override void RevertToDefaults()
		{
			Options?.RevertToDefaults();
		}

		public TOptions Options
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
