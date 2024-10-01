using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.RemoveOverlaps
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

		[CanBeNull]
		public OverridableSetting<ChangeAlongZSource> ZSource { get; set; }

		[CanBeNull]
		public List<DatasetSpecificValue<ChangeAlongZSource>> DatasetSpecificZSource { get; set; }

		#region Overrides of PartialOptionsBase

		public override PartialOptionsBase Clone()
		{
			var result = new PartialRemoveOverlapsOptions();

			result.LimitOverlapCalculationToExtent =
				TryClone(LimitOverlapCalculationToExtent);

			result.TargetFeatureSelection = TryClone(TargetFeatureSelection);

			result.ExplodeMultipartResults = TryClone(ExplodeMultipartResults);

			result.InsertVerticesInTarget = TryClone(InsertVerticesInTarget);

			result.ZSource = TryClone(ZSource);

			if (DatasetSpecificZSource != null)
			{
				result.DatasetSpecificZSource =
					new List<DatasetSpecificValue<ChangeAlongZSource>>();

				result.DatasetSpecificZSource.AddRange(
					DatasetSpecificZSource.Select(dsz => dsz.Clone()));
			}

			return result;
		}

		#endregion
	}
}
