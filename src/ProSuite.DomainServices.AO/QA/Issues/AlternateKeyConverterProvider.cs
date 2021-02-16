using System;
using System.Collections.Generic;
using System.Globalization;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public class AlternateKeyConverterProvider : IAlternateKeyConverterProvider
	{
		[NotNull] private readonly IDictionary<Guid, AlternateKeyConverter>
			_keyConverterByGuid = new Dictionary<Guid, AlternateKeyConverter>();

		[NotNull] private readonly IDictionary<Guid, QualityCondition> _conditionsByGuid;
		[NotNull] private readonly IQualityConditionObjectDatasetResolver _datasetResolver;
		[NotNull] private readonly IDatasetContext _datasetContext;

		public AlternateKeyConverterProvider(
			[NotNull] IDictionary<Guid, QualityCondition> conditionsByGuid,
			[NotNull] IQualityConditionObjectDatasetResolver datasetResolver,
			[NotNull] IDatasetContext datasetContext)
		{
			_conditionsByGuid = conditionsByGuid;
			_datasetResolver = datasetResolver;
			_datasetContext = datasetContext;
		}

		public IAlternateKeyConverter GetConverter(Guid qualityConditionGuid)
		{
			AlternateKeyConverter keyConverter;
			if (! _keyConverterByGuid.TryGetValue(qualityConditionGuid, out keyConverter))
			{
				keyConverter = CreateKeyConverter(qualityConditionGuid);
				_keyConverterByGuid.Add(qualityConditionGuid, keyConverter);
			}

			return keyConverter;
		}

		[CanBeNull]
		private AlternateKeyConverter CreateKeyConverter(Guid qualityConditionGuid)
		{
			QualityCondition qualityCondition;
			return _conditionsByGuid.TryGetValue(qualityConditionGuid, out qualityCondition)
				       ? new AlternateKeyConverter(qualityCondition,
				                                   _datasetResolver,
				                                   _datasetContext)
				       : null;
		}

		private class AlternateKeyConverter : IAlternateKeyConverter
		{
			[NotNull] private readonly QualityCondition _qualityCondition;
			[NotNull] private readonly IQualityConditionObjectDatasetResolver _datasetResolver;
			[NotNull] private readonly IDatasetContext _datasetContext;

			[NotNull] private readonly IDictionary<string, IDictionary<string, esriFieldType?>>
				_fieldTypesByTableName =
					new Dictionary<string, IDictionary<string, esriFieldType?>>(
						StringComparer.OrdinalIgnoreCase);

			public AlternateKeyConverter(
				[NotNull] QualityCondition qualityCondition,
				[NotNull] IQualityConditionObjectDatasetResolver datasetResolver,
				[NotNull] IDatasetContext datasetContext)
			{
				Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));
				Assert.ArgumentNotNull(datasetResolver, nameof(datasetResolver));
				Assert.ArgumentNotNull(datasetContext, nameof(datasetContext));

				_qualityCondition = qualityCondition;
				_datasetResolver = datasetResolver;
				_datasetContext = datasetContext;
			}

			object IAlternateKeyConverter.Convert(string tableName,
			                                      string fieldName,
			                                      string keyString)
			{
				// can be extended with data source identifier to allow disambiguation between datasets of same name from different data sources

				esriFieldType? targetType = GetFieldType(tableName, fieldName);

				if (targetType == null)
				{
					return keyString;
				}

				return FieldUtils.ConvertAttributeValue(keyString,
				                                        esriFieldType.esriFieldTypeString,
				                                        targetType.Value,
				                                        CultureInfo.InvariantCulture);
			}

			private esriFieldType? GetFieldType([NotNull] string tableName,
			                                    [NotNull] string fieldName)
			{
				IDictionary<string, esriFieldType?> fieldTypesByField;
				if (! _fieldTypesByTableName.TryGetValue(tableName, out fieldTypesByField))
				{
					fieldTypesByField = new Dictionary<string, esriFieldType?>(
						StringComparer.OrdinalIgnoreCase);
					_fieldTypesByTableName.Add(tableName, fieldTypesByField);
				}

				esriFieldType? fieldType;
				if (! fieldTypesByField.TryGetValue(fieldName, out fieldType))
				{
					fieldType = DetermineFieldType(tableName, fieldName);
					fieldTypesByField.Add(fieldName, fieldType);
				}

				return fieldType;
			}

			private esriFieldType? DetermineFieldType([NotNull] string tableName,
			                                          [NotNull] string fieldName)
			{
				IObjectDataset objectDataset = _datasetResolver.GetDatasetByInvolvedRowTableName(
					tableName, _qualityCondition);

				if (objectDataset == null)
				{
					return null;
				}

				ITable table = _datasetContext.OpenTable(objectDataset);
				if (table == null)
				{
					return null;
				}

				int fieldIndex = table.FindField(fieldName);
				if (fieldIndex < 0)
				{
					return null;
				}

				IField field = table.Fields.Field[fieldIndex];
				return field.Type;
			}
		}
	}
}
