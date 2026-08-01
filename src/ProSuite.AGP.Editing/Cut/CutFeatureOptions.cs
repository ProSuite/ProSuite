using System.Collections.Generic;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Reflection;

namespace ProSuite.AGP.Editing.Cut;

public class CutFeatureOptions : OptionsBase<PartialCutFeatureOptions>
{
	public CutFeatureOptions([CanBeNull] PartialCutFeatureOptions centralOptions,
	                         [CanBeNull] PartialCutFeatureOptions localOptions)
	{
		CentralOptions = centralOptions;
		LocalOptions = localOptions ?? new PartialCutFeatureOptions();

		CentralizableZValueSource =
			InitializeSetting<ChangeAlongZSource>(
				ReflectionUtils.GetProperty(() => LocalOptions.ZValueSource),
				ChangeAlongZSource.Target);
	}

	public CentralizableSetting<ChangeAlongZSource> CentralizableZValueSource { get; private set; }

	public ChangeAlongZSource ZValueSource => CentralizableZValueSource.CurrentValue;

	public DatasetSpecificSettingProvider<ChangeAlongZSource> GetZSourceOptionProvider()
	{
		return new DatasetSpecificSettingProvider<ChangeAlongZSource>(
			"Z values for changed vertices", ZValueSource, ZSourceByDataset);
	}

	private List<DatasetSpecificValue<ChangeAlongZSource>> ZSourceByDataset
	{
		get
		{
			if (LocalOptions.DatasetSpecificZSource != null &&
			    LocalOptions.DatasetSpecificZSource.Count > 0)
			{
				return LocalOptions.DatasetSpecificZSource;
			}

			return CentralOptions?.DatasetSpecificZSource;
		}
	}

	public override void RevertToDefaults()
	{
		CentralizableZValueSource.RevertToDefault();
	}

	public override bool HasLocalOverrides(NotificationCollection notifications)
	{
		return HasLocalOverride(CentralizableZValueSource, "Z Value Source", notifications);
	}

	public override string GetLocalOverridesMessage()
	{
		const string optionsName = "Cut Feature Options";
		return GetLocalOverridesMessage(optionsName);
	}
}
