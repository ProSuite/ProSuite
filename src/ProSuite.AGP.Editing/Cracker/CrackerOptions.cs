//using ProSuite.Commons.AGP.Carto;
//using ProSuite.Commons.Essentials.CodeAnnotations;
//using ProSuite.Commons.ManagedOptions;
//using ProSuite.Commons.Notifications;
//using ProSuite.Commons.Reflection;

//namespace ProSuite.AGP.Editing.Cracker
//{
//	public class CrackerOptions : OptionsBase<CrackerOptions>
//	{
//		public CentralizableSetting<bool> CentralizableLimitOverlapCalculationToExtent { get; }

//		public CentralizableSetting<TargetFeatureSelection> CentralizableTargetFeatureSelection
//		{
//			get;
//		}

//		public CentralizableSetting<bool> CentralizableExplodeMultipartResults { get; }

//		public CentralizableSetting<bool> CentralizableInsertVerticesInTarget { get; }

//		public CrackerOptions(
//			[CanBeNull] CrackerOptions centralOptions,
//			[CanBeNull] CrackerOptions localOptions)
//		{
//			CentralOptions = centralOptions;

//			LocalOptions = localOptions ?? new CrackerOptions();

//			CentralizableLimitOverlapCalculationToExtent = InitializeSetting<bool>(
//				ReflectionUtils.GetProperty(
//					() => LocalOptions.LimitOverlapCalculationToExtent),
//				false);

//			CentralizableTargetFeatureSelection =
//				InitializeSetting<TargetFeatureSelection>(
//					ReflectionUtils.GetProperty(
//						() => LocalOptions.TargetFeatureSelection),
//					TargetFeatureSelection.VisibleFeatures);

//			CentralizableExplodeMultipartResults = InitializeSetting<bool>(
//				ReflectionUtils.GetProperty(() => LocalOptions.ExplodeMultipartResults),
//				false);

//			CentralizableInsertVerticesInTarget = InitializeSetting<bool>(
//				ReflectionUtils.GetProperty(() => LocalOptions.InsertVerticesInTarget),
//				false);
//		}

//		public bool LimitOverlapCalculationToExtent
//		{
//			get { return CentralizableLimitOverlapCalculationToExtent.CurrentValue; }
//			set { CentralizableLimitOverlapCalculationToExtent.CurrentValue = value; }
//		}

//		public TargetFeatureSelection TargetFeatureSelection
//		{
//			get { return CentralizableTargetFeatureSelection.CurrentValue; }
//			set { CentralizableTargetFeatureSelection.CurrentValue = value; }
//		}

//		public bool ExplodeMultipartResults
//		{
//			get { return CentralizableExplodeMultipartResults.CurrentValue; }
//			set { CentralizableExplodeMultipartResults.CurrentValue = value; }
//		}

//		public bool InsertVerticesInTarget
//		{
//			get { return CentralizableInsertVerticesInTarget.CurrentValue; }
//			set { CentralizableInsertVerticesInTarget.CurrentValue = value; }
//		}

//		#region Overrides of OptionsBase<PartialRemoveOverlapsOptions>

//		public override void RevertToDefaults()
//		{
//			CentralizableLimitOverlapCalculationToExtent.RevertToDefault();

//			CentralizableTargetFeatureSelection.RevertToDefault();

//			CentralizableExplodeMultipartResults.RevertToDefault();

//			CentralizableInsertVerticesInTarget.RevertToDefault();
//		}

//		public override bool HasLocalOverrides(NotificationCollection notifications)
//		{
//			var result = false;

//			if (HasLocalOverride(CentralizableLimitOverlapCalculationToExtent,
//			                     "Calculate overlaps only in current map extent",
//			                     notifications))
//			{
//				result = true;
//			}

//			if (HasLocalOverride(CentralizableTargetFeatureSelection,
//			                     "Target features for overlap calculation",
//			                     notifications))
//			{
//				result = true;
//			}

//			if (HasLocalOverride(CentralizableExplodeMultipartResults,
//			                     "Explode multipart results into separate features",
//			                     notifications))
//			{
//				result = true;
//			}

//			if (HasLocalOverride(CentralizableInsertVerticesInTarget,
//			                     "Insert vertices in target fetures for topological correctness",
//			                     notifications))
//			{
//				result = true;
//			}

//			return result;
//		}

//		public override string GetLocalOverridesMessage()
//		{
//			const string optionsName = "Remove Overlaps Options";

//			return GetLocalOverridesMessage(optionsName);
//		}

//		#endregion
//	}
//}
