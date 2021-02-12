using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.DataModel.Harvesting
{
	public class DatasetMatchCriterionConjunction : IDatasetMatchCriterion
	{
		[NotNull] private readonly List<IDatasetMatchCriterion> _criteria =
			new List<IDatasetMatchCriterion>();

		public void Add([NotNull] IDatasetMatchCriterion criterion)
		{
			_criteria.Add(criterion);
		}

		public bool IsSatisfied(IDatasetName datasetName, out string reason)
		{
			foreach (IDatasetMatchCriterion criterion in _criteria)
			{
				if (! criterion.IsSatisfied(datasetName, out reason))
				{
					return false;
				}
			}

			reason = string.Empty;
			return true;
		}
	}
}
