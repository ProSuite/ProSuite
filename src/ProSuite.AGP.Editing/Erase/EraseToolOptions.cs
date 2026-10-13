using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Reflection;

namespace ProSuite.AGP.Editing.Erase;

public class EraseToolOptions : OptionsBase<PartialEraseOptions>
{
	public EraseToolOptions([CanBeNull] PartialEraseOptions centralOptions,
	                        [CanBeNull] PartialEraseOptions localOptions)
	{
		CentralOptions = centralOptions;
		LocalOptions = localOptions ?? new PartialEraseOptions();

		CentralizableAllowPolylineErasing =
			InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.AllowPolylineErasing), false);

		CentralizableAllowMultipointErasing =
			InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.AllowMultipointErasing), false);

		CentralizablePreventMultipartResults =
			InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.PreventMultipartResults), false);

		CentralizableAlwaysShowOptionsPane =
			InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.AlwaysShowOptionsPane),
				false);
	}

	#region Centralizable Properties

	public CentralizableSetting<bool> CentralizableAllowPolylineErasing { get; private set; }

	public CentralizableSetting<bool> CentralizableAllowMultipointErasing
	{
		get;
		private set;
	}

	public CentralizableSetting<bool> CentralizablePreventMultipartResults { get; private set; }

	public CentralizableSetting<bool> CentralizableAlwaysShowOptionsPane { get; private set; }

	#endregion

	#region Current Values

	public bool AllowPolylineErasing => CentralizableAllowPolylineErasing.CurrentValue;

	public bool AllowMultipointErasing =>
		CentralizableAllowMultipointErasing.CurrentValue;

	public bool PreventMultipartResults => CentralizablePreventMultipartResults.CurrentValue;

	public bool AlwaysShowOptionsPane => CentralizableAlwaysShowOptionsPane.CurrentValue;

	#endregion

	public override void RevertToDefaults()
	{
		CentralizableAllowPolylineErasing.RevertToDefault();
		CentralizableAllowMultipointErasing.RevertToDefault();
		CentralizablePreventMultipartResults.RevertToDefault();
		CentralizableAlwaysShowOptionsPane.RevertToDefault();
	}

	public override bool HasLocalOverrides(NotificationCollection notifications)
	{
		bool result = false;

		if (HasLocalOverride(CentralizableAllowPolylineErasing,
		                     "Allow erasing of polyline features",
		                     notifications))
		{
			result = true;
		}

		if (HasLocalOverride(CentralizableAllowMultipointErasing,
		                     "Allow erasing of multipoint features",
		                     notifications))
		{
			result = true;
		}

		if (HasLocalOverride(CentralizablePreventMultipartResults,
		                     "Prevent multipart results",
		                     notifications))
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
		const string optionsName = "Erase Tool Options";
		return GetLocalOverridesMessage(optionsName);
	}
}
