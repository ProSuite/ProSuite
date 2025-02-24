using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.GeometryProcessing.ChangeAlong;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Reflection;

namespace ProSuite.AGP.Editing.ChangeAlong
{
	public class ReshapeAlongToolOptions : OptionsBase<PartialReshapeAlongToolOptions>
	{
		public ReshapeAlongToolOptions([CanBeNull] PartialReshapeAlongToolOptions centralOptions,
		                               [CanBeNull] PartialReshapeAlongToolOptions localOptions)
		{
			CentralOptions = centralOptions;
			LocalOptions = localOptions ?? new PartialReshapeAlongToolOptions();

			// Basic settings
			CentralizableInsertVertices =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.InsertVertices), true);

			// Display Performance Options
			CentralizableDisplayExcludeCutLines =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.DisplayExcludeCutLines), true);
			CentralizableDisplayRecalculateCutLines =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.DisplayRecalculateCutLines),
					false);
			CentralizableDisplayHideCutLines =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.DisplayHideCutLines), false);
			CentralizableDisplayHideCutLinesScale =
				InitializeSetting<double>(
					ReflectionUtils.GetProperty(() => LocalOptions.DisplayHideCutLinesScale),
					10000.0);

			// Minimal Tolerance settings
			CentralizableMinimalToleranceApply =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.MinimalToleranceApply), false);
			CentralizableMinimalTolerance =
				InitializeSetting<double>(
					ReflectionUtils.GetProperty(() => LocalOptions.MinimalTolerance), 0.1);

			// Buffer settings
			CentralizableBufferTarget =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.BufferTarget), true);
			CentralizableBufferTolerance =
				InitializeSetting<double>(
					ReflectionUtils.GetProperty(() => LocalOptions.BufferTolerance), 1.0);
			CentralizableEnforceMinimumBufferSegmentLength =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(
						() => LocalOptions.EnforceMinimumBufferSegmentLength), true);
			CentralizableMinBufferSegmentLength =
				InitializeSetting<double>(
					ReflectionUtils.GetProperty(() => LocalOptions.MinBufferSegmentLength), 0.1);

			// Reshape line filter settings
			CentralizableExcludeLinesOutsideSource =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.ExcludeLinesOutsideSource),
					false);
			CentralizableExcludeLinesTolerance =
				InitializeSetting<double>(
					ReflectionUtils.GetProperty(() => LocalOptions.ExcludeLinesTolerance), 1.0);
			CentralizableExcludeLinesDisplay =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.ExcludeLinesDisplay), false);
			CentralizableExcludeLinesShowOnlyRemove =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.ExcludeLinesShowOnlyRemove),
					false);
			CentralizableExcludeLinesOverlaps =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.ExcludeLinesOverlaps), false);

			// Z Value settings
			CentralizableZValueSource =
				InitializeSetting<ZValueSource>(
					ReflectionUtils.GetProperty(() => LocalOptions.ZValueSource),
					ZValueSource.Target);

			// Target Selection
			CentralizableTargetFeatureSelection =
				InitializeSetting<TargetFeatureSelection>(
					ReflectionUtils.GetProperty(() => LocalOptions.TargetFeatureSelection),
					TargetFeatureSelection.SameClass);
		}

		#region Centralizable Properties

		public CentralizableSetting<bool> CentralizableInsertVertices { get; private set; }

		// Display Performance Options
		public CentralizableSetting<bool> CentralizableDisplayExcludeCutLines { get; private set; }

		public CentralizableSetting<bool> CentralizableDisplayRecalculateCutLines
		{
			get;
			private set;
		}

		public CentralizableSetting<bool> CentralizableDisplayHideCutLines { get; private set; }

		public CentralizableSetting<double> CentralizableDisplayHideCutLinesScale
		{
			get;
			private set;
		}

		// Minimal Tolerance settings
		public CentralizableSetting<bool> CentralizableMinimalToleranceApply { get; private set; }
		public CentralizableSetting<double> CentralizableMinimalTolerance { get; private set; }

		// Buffer settings
		public CentralizableSetting<bool> CentralizableBufferTarget { get; private set; }
		public CentralizableSetting<double> CentralizableBufferTolerance { get; private set; }

		public CentralizableSetting<bool> CentralizableEnforceMinimumBufferSegmentLength
		{
			get;
			private set;
		}

		public CentralizableSetting<double> CentralizableMinBufferSegmentLength
		{
			get;
			private set;
		}

		// Reshape line filter settings
		public CentralizableSetting<bool> CentralizableExcludeLinesOutsideSource
		{
			get;
			private set;
		}

		public CentralizableSetting<double> CentralizableExcludeLinesTolerance { get; private set; }

		public CentralizableSetting<bool> CentralizableExcludeLinesDisplay { get; private set; }

		public CentralizableSetting<bool> CentralizableExcludeLinesShowOnlyRemove
		{
			get;
			private set;
		}

		public CentralizableSetting<bool> CentralizableExcludeLinesOverlaps { get; private set; }

		// Z Value settings
		public CentralizableSetting<ZValueSource> CentralizableZValueSource { get; private set; }

		// Target Selection
		public CentralizableSetting<TargetFeatureSelection> CentralizableTargetFeatureSelection
		{
			get;
			private set;
		}

		#endregion

		#region Current Values

		public bool InsertVerticesInTarget => CentralizableInsertVertices.CurrentValue;

		// Display Performance Options
		public bool ClipLinesOnVisibleExtent => CentralizableDisplayExcludeCutLines.CurrentValue;

		public bool DisplayRecalculateCutLines =>
			CentralizableDisplayRecalculateCutLines.CurrentValue;

		public bool DisplayHideCutLines => CentralizableDisplayHideCutLines.CurrentValue;

		public double DisplayHideCutLinesScale =>
			CentralizableDisplayHideCutLinesScale.CurrentValue;

		// Minimal Tolerance settings
		public bool UseCustomTolerance => CentralizableMinimalToleranceApply.CurrentValue;
		public double CustomTolerance => CentralizableMinimalTolerance.CurrentValue;

		// Buffer settings
		public bool BufferTarget => CentralizableBufferTarget.CurrentValue;
		public double BufferTolerance => CentralizableBufferTolerance.CurrentValue;

		public bool EnforceMinimumBufferSegmentLength =>
			CentralizableEnforceMinimumBufferSegmentLength.CurrentValue;

		public double MinBufferSegmentLength => CentralizableMinBufferSegmentLength.CurrentValue;

		// Reshape line filter settings
		public bool ExcludeLinesOutsideBufferDistance =>
			CentralizableExcludeLinesOutsideSource.CurrentValue;

		public double ExcludeLinesTolerance => CentralizableExcludeLinesTolerance.CurrentValue;

		// TODO: Rename centralizable property, UI text
		public bool ShowExcludeReshapeLinesToleranceBuffer =>
			CentralizableExcludeLinesDisplay.CurrentValue;

		public bool ExcludeLinesShowOnlyRemove =>
			CentralizableExcludeLinesShowOnlyRemove.CurrentValue;

		public bool ExcludeLinesOverlaps => CentralizableExcludeLinesOverlaps.CurrentValue;

		// Z Value settings
		public ZValueSource ZValueSource => CentralizableZValueSource.CurrentValue;

		// Target Selection
		public TargetFeatureSelection TargetFeatureSelection =>
			CentralizableTargetFeatureSelection.CurrentValue;

		#endregion

		public override void RevertToDefaults()
		{
			CentralizableInsertVertices.RevertToDefault();

			// Display Performance Options
			CentralizableDisplayExcludeCutLines.RevertToDefault();
			CentralizableDisplayRecalculateCutLines.RevertToDefault();
			CentralizableDisplayHideCutLines.RevertToDefault();
			CentralizableDisplayHideCutLinesScale.RevertToDefault();

			// Minimal Tolerance settings
			CentralizableMinimalToleranceApply.RevertToDefault();
			CentralizableMinimalTolerance.RevertToDefault();

			// Buffer settings
			CentralizableBufferTarget.RevertToDefault();
			CentralizableBufferTolerance.RevertToDefault();
			CentralizableEnforceMinimumBufferSegmentLength.RevertToDefault();
			CentralizableMinBufferSegmentLength.RevertToDefault();

			// Reshape line filter settings
			CentralizableExcludeLinesOutsideSource.RevertToDefault();
			CentralizableExcludeLinesTolerance.RevertToDefault();
			CentralizableExcludeLinesDisplay.RevertToDefault();
			CentralizableExcludeLinesShowOnlyRemove.RevertToDefault();
			CentralizableExcludeLinesOverlaps.RevertToDefault();

			// Z Value settings
			CentralizableZValueSource.RevertToDefault();

			// Target Selection
			CentralizableTargetFeatureSelection.RevertToDefault();
		}

		public override bool HasLocalOverrides(NotificationCollection notifications)
		{
			bool result = false;

			if (HasLocalOverride(CentralizableInsertVertices,
			                     "Insert vertices on targets for topological correctness",
			                     notifications))
			{
				result = true;
			}

			// Display Performance Options
			if (HasLocalOverride(CentralizableDisplayExcludeCutLines,
			                     "Exclude cut lines that are not completely within main map",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableDisplayRecalculateCutLines,
			                     "Recalculate cut lines when the map extent changes",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableDisplayHideCutLines,
			                     "Hide cut lines when zoomed beyond scale",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableDisplayHideCutLinesScale,
			                     "Scale threshold for hiding cut lines",
			                     notifications))
			{
				result = true;
			}

			// Minimal Tolerance settings
			if (HasLocalOverride(CentralizableMinimalToleranceApply, "Use custom minimal tolerance",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableMinimalTolerance, "Minimal tolerance value",
			                     notifications))
			{
				result = true;
			}

			// Buffer settings
			if (HasLocalOverride(CentralizableBufferTarget, "Buffer the target geometry",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableBufferTolerance, "Buffer tolerance", notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableEnforceMinimumBufferSegmentLength,
			                     "Enforce minimum buffer segment length", notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableMinBufferSegmentLength,
			                     "Minimum buffer segment length", notifications))
			{
				result = true;
			}

			// Reshape line filter settings
			if (HasLocalOverride(CentralizableExcludeLinesOutsideSource,
			                     "Exclude reshape lines outside tolerance of source",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableExcludeLinesTolerance, "Exclude lines tolerance",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableExcludeLinesDisplay,
			                     "Display reshape lines that remove areas from polygons",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableExcludeLinesShowOnlyRemove,
			                     "Only show reshape lines that remove areas from polygons",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableExcludeLinesOverlaps,
			                     "Exclude reshape lines that result in overlaps with target features",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableZValueSource, "Z Value Source", notifications))
			{
				result = true;
			}

			// Target Selection
			if (HasLocalOverride(CentralizableTargetFeatureSelection, "Target Feature Selection",
			                     notifications))
			{
				result = true;
			}

			return result;
		}

		public override string GetLocalOverridesMessage()
		{
			const string optionsName = "Reshape Along Tool Options";
			return GetLocalOverridesMessage(optionsName);
		}

		public TargetBufferOptions GetTargetBufferOptions()
		{
			double bufferDistance = BufferTarget ? BufferTolerance : 0;
			double minSegmentLength =
				EnforceMinimumBufferSegmentLength ? MinBufferSegmentLength : 0;

			return new TargetBufferOptions(bufferDistance, minSegmentLength);
		}

		public ReshapeCurveFilterOptions GetReshapeLineFilterOptions([NotNull] MapView mapView)
		{
			Envelope envelope = mapView.Extent;

			EnvelopeXY envelopeXY = ClipLinesOnVisibleExtent
				                        ? new EnvelopeXY(envelope.XMin, envelope.YMin,
				                                         envelope.XMax, envelope.YMax)
				                        : null;

			bool excludeOutsideTolerance = ExcludeLinesOutsideBufferDistance;

			return new ReshapeCurveFilterOptions(
				       envelopeXY,
				       excludeLInesOutsideSourceBuffer: excludeOutsideTolerance,
				       excludeOutSideSourceTolerance: ExcludeLinesTolerance)
			       {
				       OnlyResultingInRemovals = ExcludeLinesShowOnlyRemove,
				       ExcludeResultingInOverlaps = ExcludeLinesOverlaps
			       };
		}
	}
}
