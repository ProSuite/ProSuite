using System.ComponentModel;
using System.Runtime.CompilerServices;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Reflection;

namespace ProSuite.AGP.Editing.RemoveOverlaps
{
	public class RemoveOverlapsOptions : OptionsBase<PartialRemoveOverlapsOptions>
	{
		private CentralizableSetting<TargetFeatureSelection> _centralizableTargetFeatureSelection;
		private CentralizableSetting<bool> _centralizableLimitOverlapCalculationToExtent;

		[UsedImplicitly]
		public CentralizableSetting<bool> CentralizableLimitOverlapCalculationToExtent
		{
			get => _centralizableLimitOverlapCalculationToExtent;
			set => _centralizableLimitOverlapCalculationToExtent = value;
		}

		[UsedImplicitly]
		public CentralizableSetting<TargetFeatureSelection> CentralizableTargetFeatureSelection
		{
			get => _centralizableTargetFeatureSelection;
			set => _centralizableTargetFeatureSelection = value;
		}

		public CentralizableSetting<bool> CentralizableExplodeMultipartResults { get; }

		public CentralizableSetting<bool> CentralizableInsertVerticesInTarget { get; }

		public RemoveOverlapsOptions(
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
				false);

			CentralizableInsertVerticesInTarget = InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.InsertVerticesInTarget),
				false);
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

		#region Overrides of OptionsBase<PartialRemoveOverlapsOptions>

		public override void RevertToDefaults()
		{
			CentralizableLimitOverlapCalculationToExtent.RevertToDefault();

			CentralizableTargetFeatureSelection.RevertToDefault();

			CentralizableExplodeMultipartResults.RevertToDefault();

			CentralizableInsertVerticesInTarget.RevertToDefault();
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

			return result;
		}

		public override string GetLocalOverridesMessage()
		{
			const string optionsName = "Remove Overlaps Options";

			return GetLocalOverridesMessage(optionsName);
		}

		#endregion

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
