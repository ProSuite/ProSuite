using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.Commons.AO.Geometry.ZAssignment
{
	[CLSCompliant(false)]
	public interface IZSettingsModel
	{
		// Main Z mode
		ZMode CurrentMode { get; set; }

		// Values for [ConstantZ], [Offset] and [Factor] mode
		double ConstantZ { get; set; }
		double Offset { get; set; }

		// Values for [Dtm] mode
		DtmSubMode CurrentDtmSubMode { get; set; }

		double DtmOffset { get; set; }

		double DtmDrapeTolerance { get; set; }

		bool DrapeToDtm { get; }

		ISurface DtmSurface { get; set; }

		// Values for [Target] mode
		MultiTargetSubMode CurrentMultiTargetSubMode { get; set; }
		IList<IGeometry> TargetGeometries { get; set; }

		// Values for [Extrapolate] mode
		IGeometry SourceGeometry { get; set; }

		// ZSettingsModel Provider
		IZSettingsDefaults ZSettingsDefaults { get; set; }

		// Gets the values from the editor
		void ResetValues();

		// checks if modes are visible
		bool IsVisible(ZMode mode);

		// Checks if modes are selectable
		bool CanSelectMode(ZMode mode);

		bool CanSelectDtmSubMode(DtmSubMode subMode);

		bool CanSelectMultiTargetSubMode(MultiTargetSubMode subMode);

		// Setting flag if modes are visible
		void SetModeVisible(ZMode mode, bool visible);

		// Setting flag if modes are selectable
		void SetModeSelectable(ZMode mode, bool selectable);

		void SetDtmSubModeSelectable(DtmSubMode subMode, bool selectable);

		void SetMultiTargetsubModeSelectable(MultiTargetSubMode subMode,
		                                     bool selectable);

		void SetDtmSubModeByFlags(bool useDtmOffset, bool drape);

		void PrepareSurface(IList<IFeature> features, double minimalResolution);

		void PrepareSurface(IList<IGeometry> geometries, double minimalResolution);

		void PrepareSurface(IEnvelope envelope, double minimalResolution);

		event EventHandler SettingsChanged;

		event EventHandler CurrentModeChanged;
		event EventHandler MultipleTargetSubModeChanged;
		event EventHandler DtmSubModeChanged;

		event EventHandler ConstantZValueChanged;
		event EventHandler OffsetValueChanged;

		event EventHandler DtmOffsetValueChanged;
		event EventHandler DtmDrapeToleranceChanged;
		event EventHandler DtmSurfaceChanged;

		event EventHandler TargetGeometriesChanged;
		event EventHandler SourceGeometryChanged;

		event EventHandler SelectableModesChanged;
	}
}
