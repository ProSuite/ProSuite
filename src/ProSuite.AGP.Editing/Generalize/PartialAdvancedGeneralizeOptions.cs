using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.Generalize
{
	public class PartialAdvancedGeneralizeOptions : PartialOptionsBase
	{
		[CanBeNull]
		public OverridableSetting<bool> LimitToVisibleExtent { get; set; }

		[CanBeNull]
		public OverridableSetting<bool> LimitToWorkPerimeter { get; set; }

		[CanBeNull]
		public OverridableSetting<bool> Weed { get; set; }

		[CanBeNull]
		public OverridableSetting<double> WeedTolerance { get; set; }

		[CanBeNull]
		public OverridableSetting<bool> WeedNonLinearSegments { get; set; }

		[CanBeNull]
		public OverridableSetting<bool> EnforceMinimumSegmentLength { get; set; }

		[CanBeNull]
		public OverridableSetting<double> MinimumSegmentLength { get; set; }

		[CanBeNull]
		public OverridableSetting<bool> ProtectTopologicalVertices { get; set; }

		[CanBeNull]
		public OverridableSetting<TargetFeatureSelection> VertexProtectingFeatureSelection
		{
			get;
			set;
		}

		[CanBeNull]
		public OverridableSetting<bool> Only2D { get; set; }

		#region Overrides of PartialOptionsBase

		public override PartialOptionsBase Clone()
		{
			var result = new PartialAdvancedGeneralizeOptions();

			result.LimitToVisibleExtent = TryClone(LimitToVisibleExtent);
			result.LimitToWorkPerimeter = TryClone(LimitToWorkPerimeter);

			result.Weed = TryClone(Weed);
			result.WeedTolerance = TryClone(WeedTolerance);
			result.WeedNonLinearSegments = TryClone(WeedNonLinearSegments);
			result.EnforceMinimumSegmentLength = TryClone(EnforceMinimumSegmentLength);
			result.MinimumSegmentLength = TryClone(MinimumSegmentLength);

			result.ProtectTopologicalVertices = TryClone(ProtectTopologicalVertices);
			result.VertexProtectingFeatureSelection = VertexProtectingFeatureSelection;

			result.Only2D = TryClone(Only2D);

			return result;
		}

		#endregion
	}
}
