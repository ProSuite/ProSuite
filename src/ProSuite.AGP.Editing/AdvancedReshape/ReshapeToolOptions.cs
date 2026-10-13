using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Reflection;

namespace ProSuite.AGP.Editing.AdvancedReshape;

public class ReshapeToolOptions : OptionsBase<PartialAdvancedReshapeOptions>
{
	public ReshapeToolOptions([CanBeNull] PartialAdvancedReshapeOptions centralOptions,
	                          [CanBeNull] PartialAdvancedReshapeOptions localOptions)
	{
		CentralOptions = centralOptions;
		LocalOptions = localOptions ?? new PartialAdvancedReshapeOptions();
		CentralizableShowPreview =
			InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.ShowPreview), true);
		CentralizableRemainInSketchMode =
			InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.RemainInSketchMode), false);
		CentralizableAllowOpenJawReshape =
			InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.AllowOpenJawReshape), true);
		CentralizableMoveOpenJawEndJunction =
			InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.MoveOpenJawEndJunction), false);
		CentralizableUseTopologyTypeSelection =
			InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.UseTopologyTypeSelection),
				false);

		CentralizableAlwaysShowOptionsPane =
			InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.AlwaysShowOptionsPane),
				false);
	}

	#region Centralizable Properties

	public CentralizableSetting<bool> CentralizableShowPreview { get; private set; }

	public CentralizableSetting<bool> CentralizableRemainInSketchMode { get; private set; }

	public CentralizableSetting<bool> CentralizableAllowOpenJawReshape { get; private set; }

	public CentralizableSetting<bool> CentralizableMoveOpenJawEndJunction { get; private set; }

	public CentralizableSetting<bool> CentralizableUseTopologyTypeSelection
	{
		get;
		private set;
	}

	public CentralizableSetting<bool> CentralizableAlwaysShowOptionsPane { get; private set; }

	#endregion

	#region Current Values

	public bool ShowPreview => CentralizableShowPreview.CurrentValue;

	public bool RemainInSketchMode => CentralizableRemainInSketchMode.CurrentValue;

	public bool AllowOpenJawReshape => CentralizableAllowOpenJawReshape.CurrentValue;

	public bool MoveOpenJawEndJunction
	{
		get => CentralizableMoveOpenJawEndJunction.CurrentValue;
		set => CentralizableMoveOpenJawEndJunction.CurrentValue = value;
	}

	public bool UseTopologyTypeSelection => CentralizableUseTopologyTypeSelection.CurrentValue;

	public bool AlwaysShowOptionsPane => CentralizableAlwaysShowOptionsPane.CurrentValue;

	#endregion

	public override void RevertToDefaults()
	{
		CentralizableShowPreview.RevertToDefault();
		CentralizableRemainInSketchMode.RevertToDefault();
		CentralizableAllowOpenJawReshape.RevertToDefault();
		CentralizableMoveOpenJawEndJunction.RevertToDefault();
		CentralizableUseTopologyTypeSelection.RevertToDefault();
		CentralizableAlwaysShowOptionsPane.RevertToDefault();
	}

	public override bool HasLocalOverrides(NotificationCollection notifications)
	{
		bool result = false;

		if (HasLocalOverride(CentralizableShowPreview, "Show the reshape preview",
		                     notifications))
		{
			result = true;
		}

		if (HasLocalOverride(CentralizableRemainInSketchMode,
		                     "Remain in sketch mode after reshape operation",
		                     notifications))
		{
			result = true;
		}

		if (HasLocalOverride(CentralizableAllowOpenJawReshape, "Allow Y reshape",
		                     notifications))
		{
			result = true;
		}

		if (HasLocalOverride(CentralizableMoveOpenJawEndJunction, "Move the end junction",
		                     notifications))
		{
			result = true;
		}

		if (HasLocalOverride(CentralizableUseTopologyTypeSelection,
		                     "Enable topology type selection", notifications))
		{
			result = true;
		}

		if (HasLocalOverride(CentralizableAlwaysShowOptionsPane, "Always show this dialog",
		                     notifications))
		{
			result = true;
		}

		return result;
	}

	public override string GetLocalOverridesMessage()
	{
		const string optionsName = "Reshape Tool Options";
		return GetLocalOverridesMessage(optionsName);
	}
}