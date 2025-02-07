using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Reflection;

namespace ProSuite.AGP.Editing.ChangeAlong
{
	public class ChangeAlongToolOptions : OptionsBase<PartialChangeAlongToolOptions>
	{
		public ChangeAlongToolOptions([CanBeNull] PartialChangeAlongToolOptions centralOptions,
		                              [CanBeNull] PartialChangeAlongToolOptions localOptions)
		{
			CentralOptions = centralOptions;
			LocalOptions = localOptions ?? new PartialChangeAlongToolOptions();

			// Basic settings
			CentralizableInsertVertices =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.InsertVertices), true);
			CentralizableExcludeCutLines =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.ExcludeCutLines), false);

			// Display Performance Options
			CentralizableDisplayExcludeCutLines =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.DisplayExcludeCutLines), true);
			CentralizableDisplayRecalculateCutLines =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.DisplayRecalculateCutLines), false);
			CentralizableDisplayHideCutLines =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.DisplayHideCutLines), false);
			CentralizableDisplayHideCutLinesScale =
				InitializeSetting<double>(
					ReflectionUtils.GetProperty(() => LocalOptions.DisplayHideCutLinesScale), 10000.0);

			// Adjust settings
			CentralizableAdjust =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.Adjust), true);
			CentralizableAdjustTolerance =
				InitializeSetting<double>(
					ReflectionUtils.GetProperty(() => LocalOptions.AdjustTolerance), 1.0);
			CentralizableAdjustExcludeCurves =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.AdjustExcludeCurves), false);
			CentralizableAdjustShowTolerance =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.AdjustShowTolerance), false);

			// Buffer settings
			CentralizableBufferTolerance =
				InitializeSetting<double>(
					ReflectionUtils.GetProperty(() => LocalOptions.BufferTolerance), 1.0);
			CentralizableMinBufferSegmentLength =
				InitializeSetting<double>(
					ReflectionUtils.GetProperty(() => LocalOptions.MinBufferSegmentLength), 0.1);

			// Reshape line filter settings
			CentralizableExcludeLines =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.ExcludeLines), false);
			CentralizableExcludeLinesTolerance =
				InitializeSetting<double>(
					ReflectionUtils.GetProperty(() => LocalOptions.ExcludeLinesTolerance), 1.0);
			CentralizableExcludeLinesDisplay =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.ExcludeLinesDisplay), false);
			CentralizableExcludeLinesShowOnlyRemove =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.ExcludeLinesShowOnlyRemove), false);
			CentralizableExcludeLinesOverlaps =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.ExcludeLinesOverlaps), false);

			// Z Value settings
			CentralizableZValueSource =
				InitializeSetting<ZValueSource>(
					ReflectionUtils.GetProperty(() => LocalOptions.ZValueSource), ZValueSource.Target);
		}

		#region Centralizable Properties

		public CentralizableSetting<bool> CentralizableInsertVertices { get; private set; }
		public CentralizableSetting<bool> CentralizableExcludeCutLines { get; private set; }

		// Display Performance Options
		public CentralizableSetting<bool> CentralizableDisplayExcludeCutLines { get; private set; }
		public CentralizableSetting<bool> CentralizableDisplayRecalculateCutLines { get; private set; }
		public CentralizableSetting<bool> CentralizableDisplayHideCutLines { get; private set; }
		public CentralizableSetting<double> CentralizableDisplayHideCutLinesScale { get; private set; }

		// Adjust settings
		public CentralizableSetting<bool> CentralizableAdjust { get; private set; }
		public CentralizableSetting<double> CentralizableAdjustTolerance { get; private set; }
		public CentralizableSetting<bool> CentralizableAdjustExcludeCurves { get; private set; }
		public CentralizableSetting<bool> CentralizableAdjustShowTolerance { get; private set; }

		// Buffer settings
		public CentralizableSetting<double> CentralizableBufferTolerance { get; private set; }
		public CentralizableSetting<double> CentralizableMinBufferSegmentLength { get; private set; }

		// Reshape line filter settings
		public CentralizableSetting<bool> CentralizableExcludeLines { get; private set; }
		public CentralizableSetting<double> CentralizableExcludeLinesTolerance { get; private set; }
		public CentralizableSetting<bool> CentralizableExcludeLinesDisplay { get; private set; }
		public CentralizableSetting<bool> CentralizableExcludeLinesShowOnlyRemove { get; private set; }
		public CentralizableSetting<bool> CentralizableExcludeLinesOverlaps { get; private set; }

		// Z Value settings
		public CentralizableSetting<ZValueSource> CentralizableZValueSource { get; private set; }

		#endregion

		#region Current Values

		public bool InsertVertices => CentralizableInsertVertices.CurrentValue;
		public bool ExcludeCutLines => CentralizableExcludeCutLines.CurrentValue;

		// Display Performance Options
		public bool DisplayExcludeCutLines => CentralizableDisplayExcludeCutLines.CurrentValue;
		public bool DisplayRecalculateCutLines => CentralizableDisplayRecalculateCutLines.CurrentValue;
		public bool DisplayHideCutLines => CentralizableDisplayHideCutLines.CurrentValue;
		public double DisplayHideCutLinesScale => CentralizableDisplayHideCutLinesScale.CurrentValue;

		// Adjust settings
		public bool Adjust => CentralizableAdjust.CurrentValue;
		public double AdjustTolerance => CentralizableAdjustTolerance.CurrentValue;
		public bool AdjustExcludeCurves => CentralizableAdjustExcludeCurves.CurrentValue;
		public bool AdjustShowTolerance => CentralizableAdjustShowTolerance.CurrentValue;

		// Buffer settings
		public double BufferTolerance => CentralizableBufferTolerance.CurrentValue;
		public double MinBufferSegmentLength => CentralizableMinBufferSegmentLength.CurrentValue;

		// Reshape line filter settings
		public bool ExcludeLines => CentralizableExcludeLines.CurrentValue;
		public double ExcludeLinesTolerance => CentralizableExcludeLinesTolerance.CurrentValue;
		public bool ExcludeLinesDisplay => CentralizableExcludeLinesDisplay.CurrentValue;
		public bool ExcludeLinesShowOnlyRemove => CentralizableExcludeLinesShowOnlyRemove.CurrentValue;
		public bool ExcludeLinesOverlaps => CentralizableExcludeLinesOverlaps.CurrentValue;

		// Z Value settings
		public ZValueSource ZValueSource => CentralizableZValueSource.CurrentValue;

		#endregion

		public override void RevertToDefaults()
		{
			CentralizableInsertVertices.RevertToDefault();
			CentralizableExcludeCutLines.RevertToDefault();
			
			// Display Performance Options
			CentralizableDisplayExcludeCutLines.RevertToDefault();
			CentralizableDisplayRecalculateCutLines.RevertToDefault();
			CentralizableDisplayHideCutLines.RevertToDefault();
			CentralizableDisplayHideCutLinesScale.RevertToDefault();

			// Adjust settings
			CentralizableAdjust.RevertToDefault();
			CentralizableAdjustTolerance.RevertToDefault();
			CentralizableAdjustExcludeCurves.RevertToDefault();
			CentralizableAdjustShowTolerance.RevertToDefault();

			// Buffer settings
			CentralizableBufferTolerance.RevertToDefault();
			CentralizableMinBufferSegmentLength.RevertToDefault();

			// Reshape line filter settings
			CentralizableExcludeLines.RevertToDefault();
			CentralizableExcludeLinesTolerance.RevertToDefault();
			CentralizableExcludeLinesDisplay.RevertToDefault();
			CentralizableExcludeLinesShowOnlyRemove.RevertToDefault();
			CentralizableExcludeLinesOverlaps.RevertToDefault();

			// Z Value settings
			CentralizableZValueSource.RevertToDefault();
		}

		public override bool HasLocalOverrides(NotificationCollection notifications)
		{
			bool result = false;

			if (HasLocalOverride(CentralizableInsertVertices, "Insert vertices on targets for topological correctness",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableExcludeCutLines,
			                     "Exclude cut lines that are not completely within main map extent",
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

			// Adjust settings
			if (HasLocalOverride(CentralizableAdjust, "Calculate adjust lines with tolerance", notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableAdjustTolerance, "Adjust tolerance", notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableAdjustExcludeCurves, "Exclude curves that can be reshaped without adjust line", notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableAdjustShowTolerance, "Show adjust tolerance buffer", notifications))
			{
				result = true;
			}

			// Buffer settings
			if (HasLocalOverride(CentralizableBufferTolerance, "Buffer tolerance", notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableMinBufferSegmentLength, "Minimum buffer segment length", notifications))
			{
				result = true;
			}

			// Reshape line filter settings
			if (HasLocalOverride(CentralizableExcludeLines, "Exclude reshape lines outside tolerance of source", notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableExcludeLinesTolerance, "Exclude lines tolerance", notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableExcludeLinesDisplay, "Display reshape lines that remove areas from polygons", notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableExcludeLinesShowOnlyRemove, "Only show reshape lines that remove areas from polygons", notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableExcludeLinesOverlaps, "Exclude reshape lines that result in overlaps with target features", notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableZValueSource, "Z Value Source", notifications))
			{
				result = true;
			}

			return result;
		}

		public override string GetLocalOverridesMessage()
		{
			const string optionsName = "Change Along Tool Options";
			return GetLocalOverridesMessage(optionsName);
		}
	}
}
