using ProSuite.AGP.Editing.AdvancedReshape;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Reflection;

namespace ProSuite.AGP.Editing.AdvancedReshapeReshape
{
	public class ReshapeToolOptions : OptionsBase<PartialReshapeToolOptions>
	{
		public ReshapeToolOptions([CanBeNull] PartialReshapeToolOptions centralOptions,
		                          [CanBeNull] PartialReshapeToolOptions localOptions)
		{
			CentralOptions = centralOptions;
			LocalOptions = localOptions ?? new PartialReshapeToolOptions();
			CentralizableAllowOpenJawReshape =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.AllowOpenJawReshape), true);
			CentralizableMultiReshapeAsUnion =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.MultiReshapeAsUnion), false);
			CentralizableTryReshapeNonDefault =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.TryReshapeNonDefault), false);
			CentralizableUseNonDefaultReshapeSide =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.UseNonDefaultReshapeSide),
					false);
		}

		#region Centralizable Properties

		public CentralizableSetting<bool> CentralizableAllowOpenJawReshape { get; private set; }
		public CentralizableSetting<bool> CentralizableMultiReshapeAsUnion { get; private set; }
		public CentralizableSetting<bool> CentralizableTryReshapeNonDefault { get; private set; }

		public CentralizableSetting<bool> CentralizableUseNonDefaultReshapeSide
		{
			get;
			private set;
		}

		#endregion

		#region Current Values

		public bool AllowOpenJawReshape => CentralizableAllowOpenJawReshape.CurrentValue;
		public bool MultiReshapeAsUnion => CentralizableMultiReshapeAsUnion.CurrentValue;
		public bool TryReshapeNonDefault => CentralizableTryReshapeNonDefault.CurrentValue;
		public bool UseNonDefaultReshapeSide => CentralizableUseNonDefaultReshapeSide.CurrentValue;

		#endregion

		public override void RevertToDefaults()
		{
			CentralizableAllowOpenJawReshape.RevertToDefault();
			CentralizableMultiReshapeAsUnion.RevertToDefault();
			CentralizableTryReshapeNonDefault.RevertToDefault();
			CentralizableUseNonDefaultReshapeSide.RevertToDefault();
		}

		public override bool HasLocalOverrides(NotificationCollection notifications)
		{
			bool result = false;
			if (HasLocalOverride(CentralizableAllowOpenJawReshape, "Allow open-jaw reshape",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableMultiReshapeAsUnion, "Multi-reshape as union",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableTryReshapeNonDefault, "Try reshape non-default",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableUseNonDefaultReshapeSide,
			                     "Use non-default reshape side", notifications))
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
}
