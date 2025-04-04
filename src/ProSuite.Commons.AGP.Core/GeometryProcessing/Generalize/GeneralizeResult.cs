using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.Generalize
{
	public class GeneralizeResult
	{
		public IList<GeneralizedFeature> ResultsByFeature { get; set; } =
			new List<GeneralizedFeature>();

		[NotNull]
		public IList<string> NonStorableMessages { get; } = new List<string>(0);

		public bool HasRemovableSegments =>
			ResultsByFeature.Sum(
				f => f.RemovableSegments.Count + f.DeletablePoints?.PointCount ?? 0) > 0;
	}
}
