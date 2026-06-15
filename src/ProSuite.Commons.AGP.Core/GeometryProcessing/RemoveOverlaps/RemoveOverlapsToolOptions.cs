using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Reflection;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.RemoveOverlaps;

public class RemoveOverlapsToolOptions : OptionsBase<PartialRemoveOverlapsOptions>
{
	public CentralizableSetting<bool> CentralizableLimitOverlapCalculationToExtent { get; }

	public CentralizableSetting<TargetFeatureSelection> CentralizableTargetFeatureSelection { get; }

	public CentralizableSetting<bool> CentralizableExplodeMultipartResults { get; }

	public CentralizableSetting<bool> CentralizableInsertVerticesInTarget { get; }

	public CentralizableSetting<ChangeAlongZSource> CentralizableZSource { get; }

	public RemoveOverlapsToolOptions(
		[CanBeNull] PartialRemoveOverlapsOptions centralOptions,
		[CanBeNull] PartialRemoveOverlapsOptions localOptions)
	{
		CentralOptions = centralOptions;

		LocalOptions = localOptions ?? new PartialRemoveOverlapsOptions();

		CentralizableLimitOverlapCalculationToExtent = InitializeSetting<bool>(
			ReflectionUtils.GetProperty(
				() => LocalOptions.LimitOverlapCalculationToExtent),
			false);

		CentralizableTargetFeatureSelection =
			InitializeSetting<TargetFeatureSelection>(
				ReflectionUtils.GetProperty(
					() => LocalOptions.TargetFeatureSelection),
				TargetFeatureSelection.VisibleFeatures);

		CentralizableExplodeMultipartResults = InitializeSetting<bool>(
			ReflectionUtils.GetProperty(() => LocalOptions.ExplodeMultipartResults),
			true);

		CentralizableInsertVerticesInTarget = InitializeSetting<bool>(
			ReflectionUtils.GetProperty(() => LocalOptions.InsertVerticesInTarget), true);

		CentralizableZSource =
			InitializeSetting<ChangeAlongZSource>(
				ReflectionUtils.GetProperty(() => LocalOptions.ZSource),
				ChangeAlongZSource.Target);

		CentralizableZSource.TooltipAppendix = GetTooltipAppendix(ZSourceByDataset);
	}

	public bool LimitOverlapCalculationToExtent
	{
		get { return CentralizableLimitOverlapCalculationToExtent.CurrentValue; }
		set { CentralizableLimitOverlapCalculationToExtent.CurrentValue = value; }
	}

	public TargetFeatureSelection TargetFeatureSelection
	{
		get { return CentralizableTargetFeatureSelection.CurrentValue; }
		set { CentralizableTargetFeatureSelection.CurrentValue = value; }
	}

	public bool ExplodeMultipartResults
	{
		get { return CentralizableExplodeMultipartResults.CurrentValue; }
		set { CentralizableExplodeMultipartResults.CurrentValue = value; }
	}

	public bool InsertVerticesInTarget
	{
		get { return CentralizableInsertVerticesInTarget.CurrentValue; }
		set { CentralizableInsertVerticesInTarget.CurrentValue = value; }
	}

	public ChangeAlongZSource ZSource
	{
		get { return CentralizableZSource.CurrentValue; }
		set { CentralizableZSource.CurrentValue = value; }
	}

	[NotNull]
	private List<DatasetSpecificValue<ChangeAlongZSource>> ZSourceByDataset
	{
		get
		{
			List<DatasetSpecificValue<ChangeAlongZSource>> zSourceByDataset = null;

			if (LocalOptions.DatasetSpecificZSource != null &&
			    LocalOptions.DatasetSpecificZSource.Count > 0)
			{
				zSourceByDataset = LocalOptions.DatasetSpecificZSource;
			}
			else if (CentralOptions != null)
			{
				zSourceByDataset = CentralOptions.DatasetSpecificZSource;
			}

			return zSourceByDataset ?? new List<DatasetSpecificValue<ChangeAlongZSource>>();
		}
	}

	public IFlexibleSettingProvider<ChangeAlongZSource> GetZSourceOptionProvider()
	{
		return new DatasetSpecificSettingProvider<ChangeAlongZSource>(
			"Z values for changed vertices", ZSource, ZSourceByDataset);
	}

	#region Overrides of OptionsBase<PartialRemoveOverlapsOptions>

	public override void RevertToDefaults()
	{
		CentralizableLimitOverlapCalculationToExtent.RevertToDefault();

		CentralizableTargetFeatureSelection.RevertToDefault();

		CentralizableExplodeMultipartResults.RevertToDefault();

		CentralizableInsertVerticesInTarget.RevertToDefault();

		CentralizableZSource.RevertToDefault();
	}

	public override bool HasLocalOverrides(NotificationCollection notifications)
	{
		var result = false;

		if (HasLocalOverride(CentralizableLimitOverlapCalculationToExtent,
		                     "Calculate overlaps only in current map extent",
		                     notifications))
		{
			result = true;
		}

		if (HasLocalOverride(CentralizableTargetFeatureSelection,
		                     "Target features for overlap calculation",
		                     notifications))
		{
			result = true;
		}

		if (HasLocalOverride(CentralizableExplodeMultipartResults,
		                     "Explode multipart results into separate features",
		                     notifications))
		{
			result = true;
		}

		if (HasLocalOverride(CentralizableInsertVerticesInTarget,
		                     "Insert vertices in target fetures for topological correctness",
		                     notifications))
		{
			result = true;
		}

		if (HasLocalOverride(CentralizableZSource,
		                     "Take Z values for changed vertices", notifications))
		{
			result = true;
		}

		return result;
	}

	public override string GetLocalOverridesMessage()
	{
		const string optionsName = "Remove Overlaps Options";

		return GetLocalOverridesMessage(optionsName);
	}

	#endregion
}
