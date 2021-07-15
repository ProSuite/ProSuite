using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Reflection;

namespace ProSuite.Commons.AO.Geometry.CreateFootprint
{
	public class FootprintOptions : OptionsBase<PartialFootprintOptions>
	{
		public CentralizableSetting<double> CentralizableZOffset { get; private set; }
		public CentralizableSetting<bool> CentralizableAutoFootprintMode { get; private set; }
		public CentralizableSetting<bool> CentralizableUpdateZIfNoBuffer { get; private set; }
		public CentralizableSetting<bool> CentralizableRelateExistingTargets { get; private set; }

		public FootprintOptions([CanBeNull] PartialFootprintOptions centralOptions,
		                        [CanBeNull] PartialFootprintOptions localOptions)
		{
			CentralOptions = centralOptions;

			LocalOptions = localOptions ?? new PartialFootprintOptions();

			CentralizableZOffset = InitializeSetting<double>(
				ReflectionUtils.GetProperty(() => LocalOptions.ZOffset), 5.0);

			CentralizableAutoFootprintMode = InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.AutoFootprintMode), false);

			CentralizableUpdateZIfNoBuffer = InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.UpdateZIfNoBuffer), false);

			CentralizableRelateExistingTargets = InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.RelateExistingTargets), false);
		}

		public double ZOffset
		{
			get { return CentralizableZOffset.CurrentValue; }
		}

		public bool AutoFootprintMode
		{
			get { return CentralizableAutoFootprintMode.CurrentValue; }
		}

		public bool UpdateZIfNoBuffer
		{
			get { return CentralizableUpdateZIfNoBuffer.CurrentValue; }
		}

		public bool RelateExistingTargets
		{
			get { return CentralizableRelateExistingTargets.CurrentValue; }
		}

		public List<FootprintSourceTargetMapping> SourceTargetMappings
		{
			get
			{
				return CentralOptions == null
					       ? LocalOptions
						       .SourceTargetMappings // typically null as well, but let's allow it for special cases
					       : CentralOptions.SourceTargetMappings;
			}
		}

		public override void RevertToDefaults()
		{
			CentralizableZOffset.RevertToDefault();
			CentralizableAutoFootprintMode.RevertToDefault();
			CentralizableUpdateZIfNoBuffer.RevertToDefault();
			CentralizableRelateExistingTargets.RevertToDefault();
		}

		public override bool HasLocalOverrides(NotificationCollection notifications)
		{
			bool result = false;

			if (HasLocalOverride(CentralizableAutoFootprintMode,
			                     "Automatically create / update footprints when editing roof features",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableZOffset,
			                     "Z offset", notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableUpdateZIfNoBuffer,
			                     "Update Z values even if no roof overhang is defined",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableRelateExistingTargets,
			                     "Create a relationship if an existing footprint is found",
			                     notifications))
			{
				result = true;
			}

			return result;
		}

		public override string GetLocalOverridesMessage()
		{
			const string optionsName = "Create Footprint Options";

			return GetLocalOverridesMessage(optionsName);
		}
	}
}
