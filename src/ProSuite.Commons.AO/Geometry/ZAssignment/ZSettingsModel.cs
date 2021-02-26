using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry.ZAssignment
{
	public class ZSettingsModel : IZSettingsModel, IZSettingsDefaults
	{
		// default values

		private const double _defaultConstantZ = 0;
		private const double _defaultDrapeTolerance = 0.5;
		private const DtmSubMode _defaultDtmMode = DtmSubMode.DtmOnly;
		private const double _defaultDtmOffset = 0;

		private const MultiTargetSubMode _defaultMultiTargetMode =
			MultiTargetSubMode.Lowest;

		private const double _defaultOffset = 0;
		private const ZMode _defaultZMode = ZMode.ConstantZ;

		[NotNull] private readonly IDictionary<DtmSubMode, bool> _dtmSubModeSelectableStates
			=
			new Dictionary<DtmSubMode, bool>();

		[NotNull] private readonly IDictionary<ZMode, bool> _modeSelectableStates =
			new Dictionary<ZMode, bool>();

		[NotNull] private readonly IDictionary<ZMode, bool> _modeVisibleStates =
			new Dictionary<ZMode, bool>();

		[NotNull] private readonly IDictionary<MultiTargetSubMode, bool>
			_multiTargetSubModeSelectableStates = new Dictionary<MultiTargetSubMode, bool>();

		private double _constantZ = _defaultConstantZ;

		// Selected mode
		private ZMode _currentMode = _defaultZMode;
		private double _dtmDrapeTolerance = _defaultDrapeTolerance;
		private DtmSubMode _dtmMode = _defaultDtmMode;

		// Mode parameters
		private double _dtmOffset = _defaultDtmOffset;
		private ISurface _dtmSurface;
		private MultiTargetSubMode _multiTargetMode = _defaultMultiTargetMode;
		private double _offset = _defaultOffset;
		private IGeometry _sourceGeometry;
		private IList<IGeometry> _targetGeometries = new List<IGeometry>();
		private IZSettingsDefaults _zSettingsDefaults;

		#region Constructors

		public ZSettingsModel() : this(null) { }

		public ZSettingsModel([CanBeNull] IZSettingsDefaults zSettingsDefaults)
		{
			_modeSelectableStates.Clear();
			foreach (int value in Enum.GetValues(typeof(ZMode)))
			{
				var mode = (ZMode) value;
				bool isSelectable = mode == ZMode.None ||
				                    mode == ZMode.ConstantZ;
				_modeSelectableStates.Add((ZMode) value, isSelectable);
				_modeVisibleStates.Add((ZMode) value, isSelectable);
			}

			_dtmSubModeSelectableStates.Clear();
			foreach (int value in Enum.GetValues(typeof(DtmSubMode)))
			{
				var mode = (DtmSubMode) value;
				bool isSelectable = mode == DtmSubMode.DtmOnly ||
				                    mode == DtmSubMode.DtmOffset ||
				                    mode == DtmSubMode.DtmDrape ||
				                    mode == DtmSubMode.DtmDrapeOffset;
				_dtmSubModeSelectableStates.Add(mode, isSelectable);
			}

			_multiTargetSubModeSelectableStates.Clear();
			foreach (int value in Enum.GetValues(typeof(MultiTargetSubMode)))
			{
				_multiTargetSubModeSelectableStates.Add((MultiTargetSubMode) value, true);
			}

			if (zSettingsDefaults == null)
			{
				_zSettingsDefaults = this;
				SetInitialValues();
			}
			else
			{
				_zSettingsDefaults = zSettingsDefaults;
				ResetValues();
			}
		}

		#endregion

		#region IZSettingsDefaults Members

		public double DefaultConstantZ => _defaultConstantZ;

		public double DefaultOffset => _defaultOffset;

		public ISurface DefaultSurface => null;

		public double DefaultDtmOffset => _defaultDtmOffset;

		public double DefaultDtmDrapeTolerance => _defaultDrapeTolerance;

		public ZMode DefaultZMode => _defaultZMode;

		public DtmSubMode DefaultDtmSubMode => _defaultDtmMode;

		public MultiTargetSubMode DefaultMultiTargetSubMode => _defaultMultiTargetMode;

		public ISurface PrepareVirtualSurface(IEnvelope envelope, ISurface surface,
		                                      double minimalResolution)
		{
			return surface;
		}

		#endregion

		#region Non-public methods

		protected virtual bool IsModeSelectableCore(ZMode mode)
		{
			switch (mode)
			{
				case ZMode.Dtm:
					return _dtmSurface != null;

				case ZMode.Targets:
					return _targetGeometries.Count > 0;

				// Extrapolate should be selectable even if the source is not yet assigned!
				//case ZMode.Extrapolate:
				//    return _sourceGeometry != null;

				default:
					return true;
			}
		}

		private void SetInitialValues()
		{
			_currentMode = _defaultZMode;
			_dtmMode = _defaultDtmMode;
			_multiTargetMode = _defaultMultiTargetMode;

			_dtmOffset = _defaultDtmOffset;
			_dtmSurface = null;
			_dtmDrapeTolerance = _defaultDrapeTolerance;

			_constantZ = _defaultConstantZ;
			_offset = _defaultOffset;

			_targetGeometries.Clear();
			_sourceGeometry = null;
		}

		private void GetDefaultValues()
		{
			Assert.NotNull(_zSettingsDefaults, "_zSettingsDefaults");

			_constantZ = _zSettingsDefaults.DefaultConstantZ;
			_offset = _zSettingsDefaults.DefaultOffset;

			_dtmSurface = _zSettingsDefaults.DefaultSurface;
			_dtmDrapeTolerance = _zSettingsDefaults.DefaultDtmDrapeTolerance;
			_dtmOffset = _zSettingsDefaults.DefaultDtmOffset;

			ZMode newCurrentMode = _zSettingsDefaults.DefaultZMode;
			if (CanSelectMode(newCurrentMode))
			{
				_currentMode = _zSettingsDefaults.DefaultZMode;
			}

			_dtmMode = _zSettingsDefaults.DefaultDtmSubMode;
			_multiTargetMode = _zSettingsDefaults.DefaultMultiTargetSubMode;
		}

		private void AllowTargetIndexing()
		{
			if (_targetGeometries == null)
			{
				return;
			}

			foreach (IGeometry targetGeo in _targetGeometries)
			{
				GeometryUtils.AllowIndexing(targetGeo);
			}
		}

		private void OnSettingsChanged(EventArgs e)
		{
			if (SettingsChanged != null)
			{
				SettingsChanged(this, e);
			}
		}

		private void OnCurrentModeChanged(EventArgs e)
		{
			if (CurrentModeChanged != null)
			{
				CurrentModeChanged(this, e);
			}

			OnSettingsChanged(e);
		}

		private void OnMultipleTargetSubModeChanged(EventArgs e)
		{
			if (MultipleTargetSubModeChanged != null)
			{
				MultipleTargetSubModeChanged(this, e);
			}

			OnSettingsChanged(e);
		}

		private void OnDtmSubModeChanged(EventArgs e)
		{
			if (DtmSubModeChanged != null)
			{
				DtmSubModeChanged(this, e);
			}

			OnSettingsChanged(e);
		}

		private void OnConstantZValueChanged(EventArgs e)
		{
			if (ConstantZValueChanged != null)
			{
				ConstantZValueChanged(this, e);
			}

			OnSettingsChanged(e);
		}

		private void OnOffsetValueChanged(EventArgs e)
		{
			if (OffsetValueChanged != null)
			{
				OffsetValueChanged(this, e);
			}

			OnSettingsChanged(e);
		}

		private void OnDtmOffsetValueChanged(EventArgs e)
		{
			if (DtmOffsetValueChanged != null)
			{
				DtmOffsetValueChanged(this, e);
			}

			OnSettingsChanged(e);
		}

		private void OnDtmDrapeToleranceChanged(EventArgs e)
		{
			if (DtmDrapeToleranceChanged != null)
			{
				DtmDrapeToleranceChanged(this, e);
			}

			OnSettingsChanged(e);
		}

		private void OnDtmSurfaceChanged(EventArgs e)
		{
			if (DtmSurfaceChanged != null)
			{
				DtmSurfaceChanged(this, e);
			}

			OnSettingsChanged(e);
		}

		private void OnTargetGeometriesChanged(EventArgs e)
		{
			if (TargetGeometriesChanged != null)
			{
				TargetGeometriesChanged(this, e);
			}

			OnSettingsChanged(e);
		}

		private void OnSourceGeometryChanged(EventArgs e)
		{
			if (SourceGeometryChanged != null)
			{
				SourceGeometryChanged(this, e);
			}

			OnSettingsChanged(e);
		}

		private void OnSelectableModesChanged(EventArgs e)
		{
			if (SelectableModesChanged != null)
			{
				SelectableModesChanged(this, e);
			}
		}

		#endregion // Non-public methods

		// Mode selectable states

		#region IZSettingsModel Members

		public event EventHandler SettingsChanged;
		public event EventHandler CurrentModeChanged;
		public event EventHandler MultipleTargetSubModeChanged;
		public event EventHandler DtmSubModeChanged;
		public event EventHandler ConstantZValueChanged;
		public event EventHandler OffsetValueChanged;
		public event EventHandler DtmOffsetValueChanged;
		public event EventHandler DtmDrapeToleranceChanged;
		public event EventHandler DtmSurfaceChanged;
		public event EventHandler TargetGeometriesChanged;
		public event EventHandler SourceGeometryChanged;
		public event EventHandler SelectableModesChanged;

		public IZSettingsDefaults ZSettingsDefaults
		{
			get { return _zSettingsDefaults; }
			set
			{
				_zSettingsDefaults = value ?? this;

				ResetValues();
			}
		}

		public ZMode CurrentMode
		{
			get { return _currentMode; }
			set
			{
				if (_currentMode == value || ! CanSelectMode(value))
				{
					return;
				}

				_currentMode = value;
				OnCurrentModeChanged(EventArgs.Empty);
			}
		}

		public DtmSubMode CurrentDtmSubMode
		{
			get { return _dtmMode; }
			set
			{
				if (_dtmMode == value || ! CanSelectDtmSubMode(value))
				{
					return;
				}

				_dtmMode = value;
				OnDtmSubModeChanged(EventArgs.Empty);
			}
		}

		public MultiTargetSubMode CurrentMultiTargetSubMode
		{
			get { return _multiTargetMode; }
			set
			{
				if (_multiTargetMode == value || ! CanSelectMultiTargetSubMode(value))
				{
					return;
				}

				_multiTargetMode = value;
				OnMultipleTargetSubModeChanged(EventArgs.Empty);
			}
		}

		public ISurface DtmSurface
		{
			get { return _dtmSurface; }
			set
			{
				_dtmSurface = value;
				OnDtmSurfaceChanged(EventArgs.Empty);
			}
		}

		public double DtmOffset
		{
			get { return _dtmOffset; }
			set
			{
				if (Math.Abs(_dtmOffset - value) < double.Epsilon)
				{
					return;
				}

				_dtmOffset = value;
				OnDtmOffsetValueChanged(EventArgs.Empty);
			}
		}

		public double DtmDrapeTolerance
		{
			get { return _dtmDrapeTolerance; }
			set
			{
				if (Math.Abs(_dtmDrapeTolerance - value) < double.Epsilon)
				{
					return;
				}

				_dtmDrapeTolerance = value;
				OnDtmDrapeToleranceChanged(EventArgs.Empty);
			}
		}

		public bool DrapeToDtm =>
			_dtmMode == DtmSubMode.DtmDrape || _dtmMode == DtmSubMode.DtmDrapeOffset;

		public double ConstantZ
		{
			get { return _constantZ; }
			set
			{
				if (Math.Abs(_constantZ - value) < double.Epsilon)
				{
					return;
				}

				_constantZ = value;
				OnConstantZValueChanged(EventArgs.Empty);
			}
		}

		public double Offset
		{
			get { return _offset; }
			set
			{
				if (Math.Abs(_offset - value) < double.Epsilon)
				{
					return;
				}

				_offset = value;
				OnOffsetValueChanged(EventArgs.Empty);
			}
		}

		public IList<IGeometry> TargetGeometries
		{
			get { return _targetGeometries; }
			set
			{
				_targetGeometries = value;
				AllowTargetIndexing();
				OnTargetGeometriesChanged(EventArgs.Empty);
			}
		}

		public IGeometry SourceGeometry
		{
			get { return _sourceGeometry; }
			set
			{
				if (_sourceGeometry == value)
				{
					return;
				}

				_sourceGeometry = value;
				OnSourceGeometryChanged(EventArgs.Empty);
			}
		}

		public void ResetValues()
		{
			SetInitialValues();
			GetDefaultValues();

			OnCurrentModeChanged(EventArgs.Empty);
			OnSettingsChanged(EventArgs.Empty);
		}

		public bool IsVisible(ZMode mode)
		{
			return _modeVisibleStates[mode];
		}

		public bool CanSelectMode(ZMode mode)
		{
			return _modeSelectableStates[mode] && _modeVisibleStates[mode];
		}

		public bool CanSelectDtmSubMode(DtmSubMode subMode)
		{
			return _dtmSubModeSelectableStates[subMode] && CanSelectMode(ZMode.Dtm);
		}

		public bool CanSelectMultiTargetSubMode(MultiTargetSubMode subMode)
		{
			return _multiTargetSubModeSelectableStates[subMode] &&
			       CanSelectMode(ZMode.Targets);
		}

		public void SetModeVisible(ZMode mode, bool visible)
		{
			_modeVisibleStates[mode] = visible;
		}

		public void SetModeSelectable(ZMode mode, bool selectable)
		{
			bool oldState = _modeSelectableStates[mode];

			if (selectable && IsModeSelectableCore(mode))
			{
				_modeSelectableStates[mode] = true;
				_modeVisibleStates[mode] = true;
			}
			else
			{
				_modeSelectableStates[mode] = false;
			}

			if (oldState != _modeSelectableStates[mode])
			{
				OnSelectableModesChanged(EventArgs.Empty);
			}
		}

		public void SetDtmSubModeSelectable(DtmSubMode subMode, bool selectable)
		{
			_dtmSubModeSelectableStates[subMode] = selectable &&
			                                       CanSelectMode(ZMode.Dtm);
		}

		public void SetMultiTargetsubModeSelectable(MultiTargetSubMode subMode,
		                                            bool selectable)
		{
			_multiTargetSubModeSelectableStates[subMode] = selectable &&
			                                               CanSelectMode(ZMode.Targets);
		}

		public void SetDtmSubModeByFlags(bool useDtmOffset, bool drape)
		{
			if (useDtmOffset)
			{
				CurrentDtmSubMode = drape
					                    ? DtmSubMode.DtmDrapeOffset
					                    : DtmSubMode.DtmOffset;
			}
			else
			{
				CurrentDtmSubMode = drape
					                    ? DtmSubMode.DtmDrape
					                    : DtmSubMode.DtmOnly;
			}
		}

		public void PrepareSurface(IList<IFeature> features, double minimalResolution)
		{
			Assert.NotNull(_zSettingsDefaults, "_zSettingsDefaults");

			IEnvelope envelope = GeometryUtils.UnionFeatureEnvelopes(features);

			PrepareSurface(envelope, minimalResolution);
		}

		public void PrepareSurface(IList<IGeometry> geometries, double minimalResolution)
		{
			Assert.NotNull(_zSettingsDefaults, "_zSettingsDefaults");

			IEnvelope envelope = GeometryUtils.UnionGeometryEnvelopes(geometries);

			PrepareSurface(envelope, minimalResolution);
		}

		public void PrepareSurface(IEnvelope envelope, double minimalResolution)
		{
			Assert.NotNull(_zSettingsDefaults, "_zSettingsDefaults");

			if (DtmSurface == null)
			{
				return;
			}

			_dtmSurface = _zSettingsDefaults.PrepareVirtualSurface(envelope, DtmSurface,
			                                                       minimalResolution);
		}

		#endregion
	}
}
