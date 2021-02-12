using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options;

namespace ProSuite.DomainServices.AO.QA.Standalone.XmlBased
{
	public class KeyFieldLookup
	{
		[CanBeNull] private readonly string _defaultKeyField;
		private readonly Dictionary<string, DataSourceKeyFieldLookup> _dataSources;

		public KeyFieldLookup([NotNull] XmlKeyFields keyFields)
		{
			Assert.ArgumentNotNull(keyFields, nameof(keyFields));

			string defaultKeyField = keyFields.DefaultKeyField;
			_defaultKeyField = StringUtils.IsNotEmpty(defaultKeyField)
				                   ? defaultKeyField.Trim()
				                   : null;

			List<XmlDataSourceKeyFields> dataSourceKeyFields = keyFields.DataSourceKeyFields;

			if (dataSourceKeyFields != null && dataSourceKeyFields.Count > 0)
			{
				_dataSources = GetDataSources(dataSourceKeyFields);
			}
		}

		[NotNull]
		private static Dictionary<string, DataSourceKeyFieldLookup> GetDataSources(
			[NotNull] IEnumerable<XmlDataSourceKeyFields> dataSourceKeyFields)
		{
			var result = new Dictionary<string, DataSourceKeyFieldLookup>(
				StringComparer.OrdinalIgnoreCase);

			foreach (XmlDataSourceKeyFields dataSourceKeyField in dataSourceKeyFields)
			{
				string modelName = dataSourceKeyField.ModelName;
				if (StringUtils.IsNullOrEmptyOrBlank(modelName))
				{
					throw new InvalidConfigurationException("Data source name not defined");
				}

				string modelKey = modelName.Trim();
				if (result.ContainsKey(modelKey))
				{
					throw new InvalidConfigurationException(
						$"Duplicate data source name: {modelKey}");
				}

				result.Add(modelKey, new DataSourceKeyFieldLookup(dataSourceKeyField));
			}

			return result;
		}

		[CanBeNull]
		[CLSCompliant(false)]
		public string GetKeyField([NotNull] IObjectDataset objectDataset)
		{
			if (_dataSources == null || _dataSources.Count == 0)
			{
				return _defaultKeyField;
			}

			string key = objectDataset.Model.Name.Trim();

			DataSourceKeyFieldLookup lookup;
			return _dataSources.TryGetValue(key, out lookup)
				       ? lookup.GetKeyField(objectDataset.Name)
				       : _defaultKeyField;
		}

		private class DataSourceKeyFieldLookup
		{
			[CanBeNull] private readonly string _defaultKeyField;
			[CanBeNull] private readonly Dictionary<string, string> _datasets;

			public DataSourceKeyFieldLookup(
				[NotNull] XmlDataSourceKeyFields dataSourceKeyFields)
			{
				string defaultKeyField = dataSourceKeyFields.DefaultKeyField;
				_defaultKeyField = StringUtils.IsNotEmpty(defaultKeyField)
					                   ? defaultKeyField.Trim()
					                   : null;

				List<XmlDatasetKeyField> datasetKeyFields = dataSourceKeyFields.DatasetKeyFields;
				if (datasetKeyFields != null && datasetKeyFields.Count > 0)
				{
					_datasets = GetDatasets(datasetKeyFields);
				}
			}

			[NotNull]
			private static Dictionary<string, string> GetDatasets(
				[NotNull] IEnumerable<XmlDatasetKeyField> datasetKeyFields)
			{
				var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

				foreach (XmlDatasetKeyField datasetKeyField in datasetKeyFields)
				{
					string datasetName = datasetKeyField.DatasetName;
					if (StringUtils.IsNullOrEmptyOrBlank(datasetName))
					{
						throw new InvalidConfigurationException("Dataset name not defined");
					}

					string datasetKey = datasetName.Trim();
					if (result.ContainsKey(datasetKey))
					{
						throw new InvalidConfigurationException(
							string.Format("Duplicate dataset name: {0}", datasetKey));
					}

					string keyField = datasetKeyField.KeyField;
					result.Add(datasetKey, keyField); // may be null or empty --> use object id
				}

				return result;
			}

			public string GetKeyField([NotNull] string datasetName)
			{
				Assert.ArgumentNotNullOrEmpty(datasetName, nameof(datasetName));

				if (_datasets == null || _datasets.Count == 0)
				{
					return _defaultKeyField;
				}

				string key = datasetName.Trim();

				string keyField;
				return _datasets.TryGetValue(key, out keyField)
					       ? keyField
					       : _defaultKeyField;
			}
		}
	}
}
