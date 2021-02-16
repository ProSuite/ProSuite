using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainModel.AO.QA
{
	public class QualityConditionObjectDatasetResolver :
		IQualityConditionObjectDatasetResolver
	{
		private readonly IWorkspaceContextLookup _workspaceContextLookup;

		private readonly Dictionary<QualityCondition, Dictionary<string, Dataset>>
			_gdbDatasetMap = new Dictionary<QualityCondition,
				Dictionary<string, Dataset>>();

		private readonly Dictionary<QualityCondition, Dictionary<string, Dataset>>
			_modelDatasetMap = new Dictionary<QualityCondition,
				Dictionary<string, Dataset>>();

		public QualityConditionObjectDatasetResolver(
			[NotNull] IWorkspaceContextLookup workspaceContextLookup)
		{
			Assert.ArgumentNotNull(workspaceContextLookup, nameof(workspaceContextLookup));

			_workspaceContextLookup = workspaceContextLookup;
		}

		public IObjectDataset GetDatasetByGdbTableName(
			string gdbTableName, QualityCondition qualityCondition)
		{
			Assert.ArgumentNotNullOrEmpty(gdbTableName, nameof(gdbTableName));
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

			Dictionary<string, Dataset> datasetsByGdbTableName;
			if (! _gdbDatasetMap.TryGetValue(qualityCondition, out datasetsByGdbTableName))
			{
				datasetsByGdbTableName = new Dictionary<string, Dataset>(
					StringComparer.OrdinalIgnoreCase);

				_gdbDatasetMap.Add(qualityCondition, datasetsByGdbTableName);
			}

			Dataset dataset;
			if (! datasetsByGdbTableName.TryGetValue(gdbTableName, out dataset))
			{
				dataset = GetDatasetByGdbTableNameCore(gdbTableName, qualityCondition);

				// can be null
				datasetsByGdbTableName.Add(gdbTableName, dataset);
			}

			return dataset as IObjectDataset;
		}

		// TODO pass parameter to control if match is REQUIRED (when errors are reported) or optional (stored involved rows)
		public IObjectDataset GetDatasetByInvolvedRowTableName(
			string involvedRowTableName, QualityCondition qualityCondition)
		{
			Assert.ArgumentNotNullOrEmpty(involvedRowTableName, nameof(involvedRowTableName));
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

			// the tablename in the involved row for stored error objects corresponds
			// to the dataset name (model name), for errors written after the respective
			// change in the domain model (allowing to use unqualified model names)
			// 
			// error objects written before use the gdb table name
			// -> search first in dataset names
			// -> if not found: search in gdb dataset names

			Dataset result = GetDatasetByModelName(involvedRowTableName, qualityCondition);

			if (result != null)
			{
				// match found by model name
				return result as IObjectDataset;
			}

			// TODO handle changed "harvest qualified" / "unqualified" setting
			// -> qualified involved dataset name -> unqualified model name --> already done, see below
			// -> unqualified involved dataset name -> qualified model name --> TODO qualify by harvested schema owner/database (IF UNIQUE)
			// the involved row name is qualified, maybe it was saved with an earlier version

			// TODO store error object "version" -> allow adaptive rules?

			return GetDatasetByGdbTableName(involvedRowTableName, qualityCondition);
		}

		[CanBeNull]
		private Dataset GetDatasetByModelName(
			[NotNull] string datasetModelName,
			[NotNull] QualityCondition qualityCondition)
		{
			Dictionary<string, Dataset> datasetsByModelName;
			if (! _modelDatasetMap.TryGetValue(qualityCondition, out datasetsByModelName))
			{
				datasetsByModelName = new Dictionary<string, Dataset>(
					StringComparer.OrdinalIgnoreCase);

				_modelDatasetMap.Add(qualityCondition, datasetsByModelName);
			}

			Dataset result;
			if (! datasetsByModelName.TryGetValue(datasetModelName, out result))
			{
				result = GetDatasetByModelNameCore(datasetModelName, qualityCondition);

				// can be null
				datasetsByModelName.Add(datasetModelName, result);
			}

			return result;
		}

		[CanBeNull]
		private Dataset GetDatasetByModelNameCore(
			[NotNull] string modelName,
			[NotNull] QualityCondition qualityCondition)
		{
			List<Dataset> parameterDatasets =
				qualityCondition.GetDatasetParameterValues().ToList();

			// search first for directly involved dataset
			foreach (Dataset dataset in parameterDatasets)
			{
				if (string.Equals(dataset.Name, modelName,
				                  StringComparison.OrdinalIgnoreCase))
				{
					return dataset;
				}
			}

			// if not found: search for other dataset in same workspace context as one of the directly involved datasets
			// (-> base datasets for topology, geometric network, terrain, etc.)
			foreach (Dataset involvedDataset in parameterDatasets)
			{
				IWorkspaceContext workspaceContext =
					_workspaceContextLookup.GetWorkspaceContext(involvedDataset);
				if (workspaceContext == null)
				{
					// TODO assertion exception?
					continue;
				}

				Dataset dataset = workspaceContext.GetDatasetByModelName(modelName);
				if (dataset != null)
				{
					return dataset;
				}
			}

			return null; // not found
		}

		[CanBeNull]
		private Dataset GetDatasetByGdbTableNameCore(
			[NotNull] string gdbDatasetName,
			[NotNull] QualityCondition qualityCondition)
		{
			foreach (Dataset involvedDataset in qualityCondition.GetDatasetParameterValues())
			{
				IWorkspaceContext workspaceContext =
					_workspaceContextLookup.GetWorkspaceContext(involvedDataset);
				if (workspaceContext == null)
				{
					// TODO assertion exception?
					continue;
				}

				Dataset dataset = workspaceContext.GetDatasetByGdbName(gdbDatasetName);
				if (dataset != null)
				{
					return dataset;
				}
			}

			return null; // not found
		}
	}
}
