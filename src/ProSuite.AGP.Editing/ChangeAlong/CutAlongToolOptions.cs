using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.GeometryProcessing.ChangeAlong;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Reflection;

namespace ProSuite.AGP.Editing.ChangeAlong;

public class CutAlongToolOptions : OptionsBase<PartialCutAlongOptions>
{
	public CutAlongToolOptions([CanBeNull] PartialCutAlongOptions centralOptions,
	                           [CanBeNull] PartialCutAlongOptions localOptions)
	{
		CentralOptions = centralOptions;
		LocalOptions = localOptions ?? new PartialCutAlongOptions();

		// Basic settings
		CentralizableInsertVertices =
			InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.InsertVerticesInTargets), true);

		// Display Performance Options
		CentralizableDisplayExcludeCutLines =
			InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.DisplayLinesInVisibleExtentOnly),
				false);
		CentralizableDisplayRecalculateCutLines =
			InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.RecalculateLinesOnExtentChange),
				false);
		CentralizableDisplayHideCutLines =
			InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.HideLinesBeyondMaxScale), false);
		CentralizableDisplayHideCutLinesScale =
			InitializeSetting<double>(
				ReflectionUtils.GetProperty(() => LocalOptions.HideLinesMaxScaleDenominator),
				10000.0);

		// Minimal Tolerance settings
		CentralizableMinimalToleranceApply =
			InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.UseCustomTolerance), false);
		CentralizableMinimalTolerance =
			InitializeSetting<double>(
				ReflectionUtils.GetProperty(() => LocalOptions.CustomTolerance), 0.0001);

		// Buffer settings
		CentralizableBufferTarget =
			InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.BufferTarget), false);
		CentralizableBufferTolerance =
			InitializeSetting<double>(
				ReflectionUtils.GetProperty(() => LocalOptions.TargetBufferDistance), 1.0);
		CentralizableEnforceMinimumBufferSegmentLength =
			InitializeSetting<bool>(
				ReflectionUtils.GetProperty(
					() => LocalOptions.EnforceMinimumBufferSegmentLength), true);
		CentralizableMinBufferSegmentLength =
			InitializeSetting<double>(
				ReflectionUtils.GetProperty(() => LocalOptions.MinBufferSegmentLength), 0.1);

		// Z Value settings
		CentralizableZValueSource =
			InitializeSetting<ZValueSource>(
				ReflectionUtils.GetProperty(() => LocalOptions.ZValueSource),
				ZValueSource.Target);

		// Target Selection
		CentralizableTargetFeatureSelection =
			InitializeSetting<TargetFeatureSelection>(
				ReflectionUtils.GetProperty(() => LocalOptions.TargetFeatureSelection),
				TargetFeatureSelection.VisibleSelectableFeatures);
	}

	#region Centralizable Properties

	public CentralizableSetting<bool> CentralizableInsertVertices { get; private set; }

	// Display Performance Options
	public CentralizableSetting<bool> CentralizableDisplayExcludeCutLines { get; private set; }

	public CentralizableSetting<bool> CentralizableDisplayRecalculateCutLines { get; private set; }

	public CentralizableSetting<bool> CentralizableDisplayHideCutLines { get; private set; }

	public CentralizableSetting<double> CentralizableDisplayHideCutLinesScale { get; private set; }

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

	public CentralizableSetting<double> CentralizableMinBufferSegmentLength { get; private set; }

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
	public bool MinimalToleranceApply => CentralizableMinimalToleranceApply.CurrentValue;
	public double MinimalTolerance => CentralizableMinimalTolerance.CurrentValue;

	// Buffer settings
	public bool BufferTarget => CentralizableBufferTarget.CurrentValue;
	public double BufferTolerance => CentralizableBufferTolerance.CurrentValue;

	public bool EnforceMinimumBufferSegmentLength =>
		CentralizableEnforceMinimumBufferSegmentLength.CurrentValue;

	public double MinBufferSegmentLength => CentralizableMinBufferSegmentLength.CurrentValue;

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
		const string optionsName = "Cut Along Tool Options";
		return GetLocalOverridesMessage(optionsName);
	}

	public TargetBufferOptions GetTargetBufferOptions()
	{
		double bufferDistance = BufferTarget ? BufferTolerance : 0;
		double minSegmentLength =
			EnforceMinimumBufferSegmentLength ? MinBufferSegmentLength : 0;

		return new TargetBufferOptions(bufferDistance, minSegmentLength);
	}
}
