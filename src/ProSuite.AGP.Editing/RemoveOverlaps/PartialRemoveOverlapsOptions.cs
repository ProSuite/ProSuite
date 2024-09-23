using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.RemoveOverlaps
{
	public class PartialRemoveOverlapsOptions : PartialOptionsBase
	{
		[CanBeNull]
		public OverridableSetting<bool> LimitOverlapCalculationToExtent { get; set; }

		[CanBeNull]
		public OverridableSetting<TargetFeatureSelection> TargetFeatureSelection { get; set; }

		[CanBeNull]
		public OverridableSetting<bool> ExplodeMultipartResults { get; set; }

		[CanBeNull]
		public OverridableSetting<bool> InsertVerticesInTarget { get; set; }

		#region Overrides of PartialOptionsBase

		public override PartialOptionsBase Clone()
		{
			var result = new PartialRemoveOverlapsOptions();

			result.LimitOverlapCalculationToExtent =
				TryClone(LimitOverlapCalculationToExtent);

			result.TargetFeatureSelection = TryClone(TargetFeatureSelection);

			result.ExplodeMultipartResults = TryClone(ExplodeMultipartResults);

			result.InsertVerticesInTarget = TryClone(InsertVerticesInTarget);

			return result;
		}

		#endregion
	}
}
