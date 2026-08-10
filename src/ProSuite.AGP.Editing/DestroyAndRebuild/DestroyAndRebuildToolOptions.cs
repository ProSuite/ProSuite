using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Reflection;

namespace ProSuite.AGP.Editing.DestroyAndRebuild;

public class DestroyAndRebuildToolOptions : OptionsBase<PartialDestroyAndRebuildOptions>
{
	public DestroyAndRebuildToolOptions(
		[CanBeNull] PartialDestroyAndRebuildOptions centralOptions,
		[CanBeNull] PartialDestroyAndRebuildOptions localOptions)
	{
		CentralOptions = centralOptions;

		LocalOptions = localOptions ?? new PartialDestroyAndRebuildOptions();

		const bool factoryDefaultHighlightOriginalGeometry = true;
		CentralizableHighlightOriginalGeometry =
			InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.HighlightOriginalGeometry),
				factoryDefaultHighlightOriginalGeometry);

		const bool factoryDefaultHideEditedFeature = false;
		CentralizableHideEditedFeature =
			InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.HideEditedFeature),
				factoryDefaultHideEditedFeature);

		CentralizableMoveOpenJawEndJunction =
			InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.MoveOpenJawEndJunction), false);

		CentralizableLinearNetworkDefinition =
			InitializeSetting<DestroyAndRebuildLinearNetworkDefinition>(
				ReflectionUtils.GetProperty(() => LocalOptions.LinearNetworkDefinition),
				DestroyAndRebuildLinearNetworkDefinition.SelectionFeatureClasses);
	}

	#region Centralizable Properties

	public CentralizableSetting<bool> CentralizableHighlightOriginalGeometry { get; }

	public CentralizableSetting<bool> CentralizableHideEditedFeature { get; }

	public CentralizableSetting<bool> CentralizableMoveOpenJawEndJunction { get; }

	public CentralizableSetting<DestroyAndRebuildLinearNetworkDefinition>
		CentralizableLinearNetworkDefinition { get; }

	#endregion

	#region Current Values

	public bool HighlightOriginalGeometry
	{
		get { return CentralizableHighlightOriginalGeometry.CurrentValue; }
		set { CentralizableHighlightOriginalGeometry.CurrentValue = value; }
	}

	public bool HideEditedFeature
	{
		get { return CentralizableHideEditedFeature.CurrentValue; }
		set { CentralizableHideEditedFeature.CurrentValue = value; }
	}

	public bool MoveOpenJawEndJunction
	{
		get { return CentralizableMoveOpenJawEndJunction.CurrentValue; }
		set { CentralizableMoveOpenJawEndJunction.CurrentValue = value; }
	}

	public DestroyAndRebuildLinearNetworkDefinition LinearNetworkDefinition
	{
		get { return CentralizableLinearNetworkDefinition.CurrentValue; }
		set { CentralizableLinearNetworkDefinition.CurrentValue = value; }
	}

	#endregion

	public override void RevertToDefaults()
	{
		CentralizableHighlightOriginalGeometry.RevertToDefault();

		CentralizableHideEditedFeature.RevertToDefault();

		CentralizableMoveOpenJawEndJunction.RevertToDefault();

		CentralizableLinearNetworkDefinition.RevertToDefault();
	}

	public override bool HasLocalOverrides(NotificationCollection notifications)
	{
		return HasLocalOverride(CentralizableHighlightOriginalGeometry,
		                        "Highlight original geometry",
		                        notifications) ||
		       HasLocalOverride(CentralizableHideEditedFeature,
		                        "Hide edited feature", notifications) ||
		       HasLocalOverride(CentralizableMoveOpenJawEndJunction,
		                        "Move linear network junction when end point is changed",
		                        notifications) ||
		       HasLocalOverride(CentralizableLinearNetworkDefinition,
		                        "Linear network definition",
		                        notifications);
	}

	public override string GetLocalOverridesMessage()
	{
		const string optionsName = "Destroy and Rebuild Tool Options";

		return GetLocalOverridesMessage(optionsName);
	}
}
