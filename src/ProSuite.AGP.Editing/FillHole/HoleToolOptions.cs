using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Reflection;

namespace ProSuite.AGP.Editing.FillHole
{
	// TODO: Make configurable and centralizable option and move to appropriate location (ProSuite.AGP.Editing\Holes)
	public class HoleToolOptions : OptionsBase<PartialHoleOptions>
	{
		public HoleToolOptions([CanBeNull] PartialHoleOptions centralOptions,
							 [CanBeNull] PartialHoleOptions localOptions)
		{
			CentralOptions = centralOptions;
			LocalOptions = localOptions ?? new PartialHoleOptions();

			CentralizableShowPreview = InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.ShowPreview), true);

			CentralizableLimitPreviewToExtent = InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.LimitPreviewToExtent),
				false);
		}

		#region Centralizable Properties

		public CentralizableSetting<bool> CentralizableShowPreview { get; private set; }

		public CentralizableSetting<bool> CentralizableLimitPreviewToExtent { get; private set; }
		#endregion

		#region Current Values

		public bool ShowPreview => CentralizableShowPreview.CurrentValue;

		public bool LimitPreviewToExtent => CentralizableLimitPreviewToExtent.CurrentValue;

		#endregion

		public override void RevertToDefaults()
		{
			CentralizableShowPreview.RevertToDefault();

			CentralizableLimitPreviewToExtent.RevertToDefault();
		}

		public override bool HasLocalOverrides(NotificationCollection notifications)
		{
			bool result = false;

			if (HasLocalOverride(CentralizableShowPreview,
								 "Show preview of holes that can be filled",
								 notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableLimitPreviewToExtent,
								 "Calculate preview only in visible extent for better performance",
								 notifications))
			{
				result = true;
			}

			return result;
		}

		public override string GetLocalOverridesMessage()
		{
			const string optionsName = "Fill Hole Options";

			return GetLocalOverridesMessage(optionsName);
		}

		//#region Overrides of Object

		//public override string ToString()
		//{
		//	return $"Generalization options: {Environment.NewLine}" +
		//	       $"Generalize: {Weed} (Tolerance {WeedTolerance}), including non-linear segments: {WeedNonLinearSegments}{Environment.NewLine}" +
		//	       $"Remove short segments: {EnforceMinimumSegmentLength} (Tolerance {MinimumSegmentLength}){Environment.NewLine}" +
		//	       $"Use 2D length: {Only2D}{Environment.NewLine}" +
		//	       $"Protect topologically shared vertices: {ProtectTopologicalVertices} with targets {VertexProtectingFeatureSelection}{Environment.NewLine}" +
		//	       $"Restricted to visible extent: {LimitToVisibleExtent}";
		//}

		//#endregion
	}
}
