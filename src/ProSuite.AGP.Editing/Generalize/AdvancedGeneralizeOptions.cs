using System;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Reflection;

namespace ProSuite.AGP.Editing.Generalize
{
	// NOTE regarding duplicated options classes: Consider new project ProSuite.Commons.GIS that can contain
	//      classes used both in a server and client environment such as these options without adding extra
	//      load to the ProSuite.Commons project.
	//      This project could also include the service abstractions, such as IAdvancedGeneralizeService.

	public class AdvancedGeneralizeOptions :
		OptionsBase<PartialAdvancedGeneralizeOptions>
	{
		public CentralizableSetting<bool> CentralizableLimitToVisibleExtent { get; private set; }

		public CentralizableSetting<bool> CentralizableLimitToWorkPerimeter { get; private set; }

		public CentralizableSetting<bool> CentralizableWeed { get; private set; }

		public CentralizableSetting<double> CentralizableWeedTolerance { get; private set; }

		public CentralizableSetting<bool> CentralizableWeedNonLinearSegments { get; private set; }

		public CentralizableSetting<bool> CentralizableEnforceMinimumSegmentLength
		{
			get;
			private set;
		}

		public CentralizableSetting<double> CentralizableMinimumSegmentLength { get; private set; }

		public CentralizableSetting<bool> CentralizableProtectTopologicalVertices
		{
			get;
			private set;
		}

		public CentralizableSetting<TargetFeatureSelection>
			CentralizableVertexProtectingFeatureSelection { get; private set; }

		public CentralizableSetting<bool> CentralizableOnly2D { get; private set; }

		public CentralizableSetting<bool> CentralizableShowDialog { get; private set; }

		public AdvancedGeneralizeOptions(
			[CanBeNull] PartialAdvancedGeneralizeOptions centralOptions,
			[CanBeNull] PartialAdvancedGeneralizeOptions localOptions)
		{
			CentralOptions = centralOptions;

			LocalOptions = localOptions ?? new PartialAdvancedGeneralizeOptions();

			CentralizableLimitToVisibleExtent = InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.LimitToVisibleExtent),
				false);

			CentralizableLimitToWorkPerimeter = InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.LimitToWorkPerimeter),
				false);

			CentralizableWeed = InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.Weed),
				false);

			CentralizableWeedTolerance = InitializeSetting<double>(
				ReflectionUtils.GetProperty(() => LocalOptions.WeedTolerance),
				0.0);

			CentralizableWeedNonLinearSegments = InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.WeedNonLinearSegments),
				false);

			CentralizableEnforceMinimumSegmentLength = InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.EnforceMinimumSegmentLength),
				true);

			CentralizableMinimumSegmentLength = InitializeSetting<double>(
				ReflectionUtils.GetProperty(() => LocalOptions.MinimumSegmentLength),
				0.0);

			CentralizableProtectTopologicalVertices = InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.ProtectTopologicalVertices),
				false);

			CentralizableVertexProtectingFeatureSelection = InitializeSetting
				<TargetFeatureSelection>(
					ReflectionUtils.GetProperty(
						() => LocalOptions.VertexProtectingFeatureSelection),
					TargetFeatureSelection.VisibleFeatures);

			CentralizableOnly2D = InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.Only2D),
				false);

			CentralizableShowDialog = InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.ShowDialog),
				false);
		}

		public bool LimitToVisibleExtent
		{
			get { return CentralizableLimitToVisibleExtent.CurrentValue; }
			set { CentralizableLimitToVisibleExtent.CurrentValue = value; }
		}

		public bool LimitToWorkPerimeter
		{
			get { return CentralizableLimitToWorkPerimeter.CurrentValue; }
			set { CentralizableLimitToWorkPerimeter.CurrentValue = value; }
		}

		public bool Weed
		{
			get { return CentralizableWeed.CurrentValue; }
			set { CentralizableWeed.CurrentValue = value; }
		}

		public double WeedTolerance
		{
			get { return CentralizableWeedTolerance.CurrentValue; }
			set { CentralizableWeedTolerance.CurrentValue = value; }
		}

		public bool WeedNonLinearSegments
		{
			get { return CentralizableWeedNonLinearSegments.CurrentValue; }
			set { CentralizableWeedNonLinearSegments.CurrentValue = value; }
		}

		public bool EnforceMinimumSegmentLength
		{
			get { return CentralizableEnforceMinimumSegmentLength.CurrentValue; }
			set { CentralizableEnforceMinimumSegmentLength.CurrentValue = value; }
		}

		public double MinimumSegmentLength
		{
			get { return CentralizableMinimumSegmentLength.CurrentValue; }
			set { CentralizableMinimumSegmentLength.CurrentValue = value; }
		}

		public bool ProtectTopologicalVertices
		{
			get { return CentralizableProtectTopologicalVertices.CurrentValue; }
			set { CentralizableProtectTopologicalVertices.CurrentValue = value; }
		}

		public TargetFeatureSelection VertexProtectingFeatureSelection
		{
			get { return CentralizableVertexProtectingFeatureSelection.CurrentValue; }
			set { CentralizableVertexProtectingFeatureSelection.CurrentValue = value; }
		}

		public bool Only2D
		{
			get { return CentralizableOnly2D.CurrentValue; }
			set { CentralizableOnly2D.CurrentValue = value; }
		}

		public bool ShowDialog
		{
			get { return CentralizableShowDialog.CurrentValue; }
			set { CentralizableShowDialog.CurrentValue = value; }
		}

		#region Overrides of OptionsBase<PartialAdvancedGeneralizeOptions>

		public override void RevertToDefaults()
		{
			CentralizableLimitToVisibleExtent.RevertToDefault();

			CentralizableLimitToWorkPerimeter.RevertToDefault();

			CentralizableWeed.RevertToDefault();

			CentralizableWeedTolerance.RevertToDefault();

			CentralizableWeedNonLinearSegments.RevertToDefault();

			CentralizableEnforceMinimumSegmentLength.RevertToDefault();

			CentralizableMinimumSegmentLength.RevertToDefault();

			CentralizableProtectTopologicalVertices.RevertToDefault();

			CentralizableVertexProtectingFeatureSelection.RevertToDefault();

			CentralizableOnly2D.RevertToDefault();

			CentralizableShowDialog.RevertToDefault();
		}

		public override bool HasLocalOverrides(NotificationCollection notifications)
		{
			bool result = false;

			if (HasLocalOverride(CentralizableLimitToVisibleExtent,
			                     "Limit processing to current map extent",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableLimitToWorkPerimeter,
			                     "Limit processing to current work unit perimeter",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableWeed,
			                     "Generalize segments",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableWeedTolerance,
			                     "Generalization tolerance",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableWeedNonLinearSegments,
			                     "Weed includes non-linear segments", notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableEnforceMinimumSegmentLength,
			                     "Enforce minimum segment length",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableMinimumSegmentLength,
			                     "Minimum segment length",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableProtectTopologicalVertices,
			                     "Protect topologically important points from being removed",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableOnly2D,
			                     "Measure segment length in 2D only",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableShowDialog,
			                     "Automatically show this dialog after making a selection",
			                     notifications))
			{
				result = true;
			}

			return result;
		}

		public override string GetLocalOverridesMessage()
		{
			const string optionsName = "Advanced Generalize Options";

			return GetLocalOverridesMessage(optionsName);
		}

		#endregion

		#region Overrides of Object

		public override string ToString()
		{
			return $"Generalization options: {Environment.NewLine}" +
			       $"Generalize: {Weed} (Tolerance {WeedTolerance}), including non-linear segments: {WeedNonLinearSegments}{Environment.NewLine}" +
			       $"Remove short segments: {EnforceMinimumSegmentLength} (Tolerance {MinimumSegmentLength}){Environment.NewLine}" +
			       $"Use 2D length: {Only2D}{Environment.NewLine}" +
			       $"Protect topologically shared vertices: {ProtectTopologicalVertices} with targets {VertexProtectingFeatureSelection}{Environment.NewLine}" +
			       $"Restricted to visible extent: {LimitToVisibleExtent}";
		}

		#endregion
	}
}
