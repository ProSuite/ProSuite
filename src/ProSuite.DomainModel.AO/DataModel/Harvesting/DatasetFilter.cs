using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.DataModel.Harvesting
{
	public class DatasetFilter : IDatasetFilter
	{
		private readonly List<IDatasetMatchCriterion> _inclusionCriteria;
		private readonly List<IDatasetMatchCriterion> _exclusionCriteria;

		public DatasetFilter(
			[NotNull] IEnumerable<IDatasetMatchCriterion> inclusionCriteria,
			[NotNull] IEnumerable<IDatasetMatchCriterion> exclusionCriteria)
		{
			Assert.ArgumentNotNull(inclusionCriteria, nameof(inclusionCriteria));
			Assert.ArgumentNotNull(exclusionCriteria, nameof(exclusionCriteria));

			_inclusionCriteria = new List<IDatasetMatchCriterion>(inclusionCriteria);
			_exclusionCriteria = new List<IDatasetMatchCriterion>(exclusionCriteria);
		}

		public bool Exclude(IDatasetName datasetName, out string reason)
		{
			Assert.ArgumentNotNull(datasetName, nameof(datasetName));

			return ! SatisfiesInclusionCriteria(datasetName, out reason) ||
			       SatisfiesExclusionCriteria(datasetName, out reason);
		}

		private bool SatisfiesInclusionCriteria([NotNull] IDatasetName datasetName,
		                                        [NotNull] out string reason)
		{
			if (_inclusionCriteria.Count == 0)
			{
				reason = "No inclusion criteria, included by default";
				return true;
			}

			foreach (IDatasetMatchCriterion criterion in _inclusionCriteria)
			{
				if (criterion.IsSatisfied(datasetName, out reason))
				{
					return true;
				}
			}

			reason = "None of the defined inclusion criteria is satisfied";
			return false;
		}

		private bool SatisfiesExclusionCriteria([NotNull] IDatasetName datasetName,
		                                        [NotNull] out string reason)
		{
			foreach (IDatasetMatchCriterion criterion in _exclusionCriteria)
			{
				if (criterion.IsSatisfied(datasetName, out reason))
				{
					return true;
				}
			}

			reason = string.Empty;
			return false;
		}
	}
}
