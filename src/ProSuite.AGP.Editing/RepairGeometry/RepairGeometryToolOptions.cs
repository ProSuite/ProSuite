using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Reflection;

namespace ProSuite.AGP.Editing.RepairGeometry;

public class RepairGeometryToolOptions : OptionsBase<PartialRepairGeometryOptions>
{
	public CentralizableSetting<bool> CentralizableEnforceMinimumSegmentLength { get; private set; }
	public CentralizableSetting<double> CentralizableMinimumSegmentLength { get; private set; }
	public CentralizableSetting<bool> CentralizableAllowLoops { get; private set; }

	public CentralizableSetting<bool> CentralizableAllowLinearSelfIntersections
	{
		get;
		private set;
	}

	public CentralizableSetting<bool> CentralizableAddCrackPointsBetweenParts { get; private set; }
	public CentralizableSetting<double> CentralizableCrackPointTolerance { get; private set; }
	public CentralizableSetting<bool> CentralizableUse2D { get; private set; }

	public RepairGeometryToolOptions(
		[CanBeNull] PartialRepairGeometryOptions centralOptions,
		[CanBeNull] PartialRepairGeometryOptions localOptions)
	{
		CentralOptions = centralOptions;
		LocalOptions = localOptions ?? new PartialRepairGeometryOptions();

		CentralizableEnforceMinimumSegmentLength = InitializeSetting<bool>(
			ReflectionUtils.GetProperty(() => LocalOptions.EnforceMinimumSegmentLength), true);

		CentralizableMinimumSegmentLength = InitializeSetting<double>(
			ReflectionUtils.GetProperty(() => LocalOptions.MinimumSegmentLength), 0.5);

		CentralizableAllowLoops = InitializeSetting<bool>(
			ReflectionUtils.GetProperty(() => LocalOptions.AllowLoops), false);

		CentralizableAllowLinearSelfIntersections = InitializeSetting<bool>(
			ReflectionUtils.GetProperty(() => LocalOptions.AllowLinearSelfIntersections), false);

		CentralizableAddCrackPointsBetweenParts = InitializeSetting<bool>(
			ReflectionUtils.GetProperty(() => LocalOptions.AddCrackPointsBetweenParts), true);

		CentralizableCrackPointTolerance = InitializeSetting<double>(
			ReflectionUtils.GetProperty(() => LocalOptions.CrackPointTolerance), 0.0);

		CentralizableUse2D = InitializeSetting<bool>(
			ReflectionUtils.GetProperty(() => LocalOptions.Use2D), false);
	}

	public bool EnforceMinimumSegmentLength
	{
		get => CentralizableEnforceMinimumSegmentLength.CurrentValue;
		set => CentralizableEnforceMinimumSegmentLength.CurrentValue = value;
	}

	public double MinimumSegmentLength
	{
		get => CentralizableMinimumSegmentLength.CurrentValue;
		set => CentralizableMinimumSegmentLength.CurrentValue = value;
	}

	public bool AllowLoops
	{
		get => CentralizableAllowLoops.CurrentValue;
		set => CentralizableAllowLoops.CurrentValue = value;
	}

	public bool AllowLinearSelfIntersections
	{
		get => CentralizableAllowLinearSelfIntersections.CurrentValue;
		set => CentralizableAllowLinearSelfIntersections.CurrentValue = value;
	}

	public bool AddCrackPointsBetweenParts
	{
		get => CentralizableAddCrackPointsBetweenParts.CurrentValue;
		set => CentralizableAddCrackPointsBetweenParts.CurrentValue = value;
	}

	public double CrackPointTolerance
	{
		get => CentralizableCrackPointTolerance.CurrentValue;
		set => CentralizableCrackPointTolerance.CurrentValue = value;
	}

	public bool Use2D
	{
		get => CentralizableUse2D.CurrentValue;
		set => CentralizableUse2D.CurrentValue = value;
	}

	#region Overrides of OptionsBase<PartialRepairGeometryOptions>

	public override void RevertToDefaults()
	{
		CentralizableEnforceMinimumSegmentLength.RevertToDefault();
		CentralizableMinimumSegmentLength.RevertToDefault();
		CentralizableAllowLoops.RevertToDefault();
		CentralizableAllowLinearSelfIntersections.RevertToDefault();
		CentralizableAddCrackPointsBetweenParts.RevertToDefault();
		CentralizableCrackPointTolerance.RevertToDefault();
		CentralizableUse2D.RevertToDefault();
	}

	public override bool HasLocalOverrides(NotificationCollection notifications)
	{
		bool result = false;

		if (HasLocalOverride(CentralizableEnforceMinimumSegmentLength,
		                     "Enforce minimum segment length", notifications))
			result = true;

		if (HasLocalOverride(CentralizableMinimumSegmentLength,
		                     "Minimum segment length", notifications))
			result = true;

		if (HasLocalOverride(CentralizableAllowLoops,
		                     "Allow loops", notifications))
			result = true;

		if (HasLocalOverride(CentralizableAllowLinearSelfIntersections,
		                     "Allow linear self-intersections", notifications))
			result = true;

		if (HasLocalOverride(CentralizableAddCrackPointsBetweenParts,
		                     "Add crack points between parts", notifications))
			result = true;

		if (HasLocalOverride(CentralizableCrackPointTolerance,
		                     "Crack point tolerance", notifications))
			result = true;

		if (HasLocalOverride(CentralizableUse2D,
		                     "Use 2D distance", notifications))
			result = true;

		return result;
	}

	public override string GetLocalOverridesMessage()
	{
		const string optionsName = "Repair Geometry Options";
		return GetLocalOverridesMessage(optionsName);
	}

	#endregion

	#region Overrides of Object

	public override string ToString()
	{
		return $"Repair Geometry options: {Environment.NewLine}" +
		       $"Enforce minimum segment length: {EnforceMinimumSegmentLength} ({MinimumSegmentLength}){Environment.NewLine}" +
		       $"Allow loops: {AllowLoops}{Environment.NewLine}" +
		       $"Allow linear self-intersections: {AllowLinearSelfIntersections}{Environment.NewLine}" +
		       $"Add crack points between parts: {AddCrackPointsBetweenParts} (tolerance {CrackPointTolerance}){Environment.NewLine}" +
		       $"Use 2D distance: {Use2D}";
	}

	#endregion
}
