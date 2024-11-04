using ProSuite.AGP.Editing.YReshape;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Reflection;

namespace ProSuite.AGP.Editing.YReshape
{
	internal class YReshapeToolOptions : OptionsBase<YReshapeToolOptions>
	{
	public YReshapeToolOptions([CanBeNull] YReshapeToolOptions centralOptions,
	                           [CanBeNull] YReshapeToolOptions localOptions)
	{
		CentralOptions = centralOptions;

		LocalOptions = localOptions ??
		               new YReshapeToolOptions(); // TODO Partial?

		CentralizableTargetFeatureSelection =
			InitializeSetting<TargetFeatureSelection>(
				ReflectionUtils.GetProperty(() => LocalOptions.TargetFeatureSelection),
				TargetFeatureSelection.VisibleFeatures);

		CentralizableRespectMinimumSegmentLength =
			InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.RespectMinimumSegmentLength),
				false);
		CentralizableMinimumSegmentLength =
			InitializeSetting<double>(
				ReflectionUtils.GetProperty(() => LocalOptions.MinimumSegmentLength), 0.0);

		CentralizableSnapToTargetVertices =
			InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.SnapToTargetVertices), false);
		CentralizableSnapTolerance =
			InitializeSetting<double>(
				ReflectionUtils.GetProperty(() => LocalOptions.SnapTolerance), 0.0);

		CentralizableUseSourceZs =
			InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.UseSourceZs), false);
	}

	public YReshapeToolOptions CentralOptions { get; set; }

	#region Centralizable Properties

	public CentralizableSetting<bool> CentralizableRespectMinimumSegmentLength
	{
		get;
		private set;
	}

	public CentralizableSetting<double> CentralizableMinimumSegmentLength { get; private set; }

	public CentralizableSetting<bool> CentralizableSnapToTargetVertices { get; private set; }

	public CentralizableSetting<double> CentralizableSnapTolerance { get; private set; }

	public CentralizableSetting<TargetFeatureSelection>
		CentralizableTargetFeatureSelection { get; private set; }

	public CentralizableSetting<bool> CentralizableRemoveUnnecessaryVertices
	{
		get;
		private set;
	}

	public CentralizableSetting<bool> CentralizableUseSourceZs { get; private set; }

	#endregion

	#region Current Values

	public TargetFeatureSelection TargetFeatureSelection
	{
		get { return CentralizableTargetFeatureSelection.CurrentValue; }
	}

	public bool RespectMinimumSegmentLength
	{
		get { return CentralizableRespectMinimumSegmentLength.CurrentValue; }
	}

	public double MinimumSegmentLength
	{
		get { return CentralizableMinimumSegmentLength.CurrentValue; }
	}

	public bool SnapToTargetVertices
	{
		get { return CentralizableSnapToTargetVertices.CurrentValue; }
	}

	public double SnapTolerance
	{
		get { return CentralizableSnapTolerance.CurrentValue; }
	}

	public bool UseSourceZs
	{
		get { return CentralizableUseSourceZs.CurrentValue; }
	}

	public bool ExcludeInteriorInteriorIntersections => false;

	#endregion

	public override void RevertToDefaults()
	{
		CentralizableTargetFeatureSelection.RevertToDefault();

		CentralizableRespectMinimumSegmentLength.RevertToDefault();
		CentralizableMinimumSegmentLength.RevertToDefault();

		CentralizableSnapToTargetVertices.RevertToDefault();
		CentralizableSnapTolerance.RevertToDefault();

		CentralizableRemoveUnnecessaryVertices.RevertToDefault();

		CentralizableUseSourceZs.RevertToDefault();
	}

	public override bool HasLocalOverrides(NotificationCollection notifications)
	{
		bool result = false;

		if (HasLocalOverride(CentralizableSnapToTargetVertices, "Snap to target vertices",
		                     notifications))
		{
			result = true;
		}

		if (HasLocalOverride(CentralizableSnapTolerance, "Snap tolerance", notifications))
		{
			result = true;
		}

		if (HasLocalOverride(CentralizableRespectMinimumSegmentLength,
		                     "Use minimum segment length", notifications))
		{
			result = true;
		}

		if (HasLocalOverride(CentralizableMinimumSegmentLength, "Minimum segment length",
		                     notifications))
		{
			result = true;
		}

		if (HasLocalOverride(CentralizableTargetFeatureSelection,
		                     "Target feature selection", notifications))
		{
			result = true;
		}

		if (HasLocalOverride(CentralizableUseSourceZs, "Use source feature's Z",
		                     notifications))
		{
			result = true;
		}

		if (HasLocalOverride(CentralizableRemoveUnnecessaryVertices,
		                     "Remove unnecessary vertices", notifications))
		{
			result = true;
		}

		return result;
	}

	public override string GetLocalOverridesMessage()
	{
		const string optionsName = "Cracker Tool Options";

		return GetLocalOverridesMessage(optionsName);
	}
	}
}
