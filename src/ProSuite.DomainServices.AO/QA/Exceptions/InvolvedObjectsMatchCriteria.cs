using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public class InvolvedObjectsMatchCriteria
	{
		[NotNull] private readonly HashSet<string> _ignoredDataSources =
			new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		[NotNull] private readonly IDictionary<string, HashSet<string>>
			_ignoredDatasetsByDataSource =
				new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

		public InvolvedObjectsMatchCriteria(
			[NotNull] IEnumerable<XmlInvolvedObjectsMatchCriterionIgnoredDatasets> ignored)
		{
			foreach (XmlInvolvedObjectsMatchCriterionIgnoredDatasets ignoredDatasets in ignored
			)
			{
				string modelName = ignoredDatasets.ModelName;
				if (StringUtils.IsNullOrEmptyOrBlank(modelName))
				{
					continue;
				}

				string dataSourceKey = modelName.Trim();

				List<string> datasetNames = ignoredDatasets.DatasetNames;
				if (datasetNames == null || datasetNames.Count == 0)
				{
					_ignoredDataSources.Add(dataSourceKey);
				}
				else
				{
					HashSet<string> datasetNameSet;
					if (! _ignoredDatasetsByDataSource.TryGetValue(
						    dataSourceKey, out datasetNameSet)
					)
					{
						datasetNameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
						_ignoredDatasetsByDataSource.Add(dataSourceKey, datasetNameSet);
					}

					foreach (string datasetName in datasetNames)
					{
						datasetNameSet.Add(datasetName);
					}
				}
			}
		}

		public bool IgnoreDataset([NotNull] IObjectDataset objectDataset)
		{
			if (_ignoredDataSources.Count == 0 && _ignoredDatasetsByDataSource.Count == 0)
			{
				return false;
			}

			string dataSourceKey = objectDataset.Model.Name.Trim();

			if (_ignoredDataSources.Contains(dataSourceKey))
			{
				return true;
			}

			HashSet<string> ignoredDatasetNames;
			if (! _ignoredDatasetsByDataSource.TryGetValue(dataSourceKey,
			                                               out ignoredDatasetNames))
			{
				return false;
			}

			return ignoredDatasetNames.Contains(objectDataset.Name.Trim());
		}
	}
}
