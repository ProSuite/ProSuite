using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Xml;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.AO.QA.Xml
{
	// This has been largely consumed by DomainModel.Core.QA.XML.XmlDataQualityUtils.
	// It shall be deleted when the legacy condition generation is removed.
	public static class XmlQaUtils
	{
		[CanBeNull]
		public static QualityCondition CreateQualityConditionLegacy(
			[NotNull] XmlQualityCondition xmlQualityCondition,
			[NotNull] TestDescriptor testDescriptor,
			[NotNull] IDictionary<string, DdxModel> modelsByWorkspaceId,
			[NotNull] Func<string, IList<Dataset>> getDatasetsByName,
			[CanBeNull] DataQualityCategory category,
			bool ignoreForUnknownDatasets,
			out ICollection<DatasetTestParameterRecord> unknownDatasetParameters)
		{
			Assert.ArgumentNotNull(xmlQualityCondition, nameof(xmlQualityCondition));
			Assert.ArgumentNotNull(testDescriptor, nameof(testDescriptor));
			Assert.ArgumentNotNull(modelsByWorkspaceId, nameof(modelsByWorkspaceId));
			Assert.ArgumentNotNull(getDatasetsByName, nameof(getDatasetsByName));

			unknownDatasetParameters = new List<DatasetTestParameterRecord>();

			TestFactory testFactory =
				Assert.NotNull(TestFactoryUtils.GetTestFactory(testDescriptor));

			Dictionary<string, TestParameter> testParametersByName =
				testFactory.Parameters.ToDictionary(
					parameter => parameter.Name,
					StringComparer.OrdinalIgnoreCase);

			var result = new QualityCondition(xmlQualityCondition.Name, testDescriptor);

			UpdateQualityCondition(result, xmlQualityCondition, category);

			foreach (XmlTestParameterValue xmlTestParameterValue in
			         xmlQualityCondition.EnumParameterValues(ignoreEmptyValues: true))
			{
				TestParameter testParameter;
				if (! testParametersByName.TryGetValue(xmlTestParameterValue.TestParameterName,
				                                       out testParameter))
				{
					throw new InvalidConfigurationException(
						string.Format(
							"The name '{0}' as a test parameter in quality condition '{1}' " +
							"does not match test descriptor.",
							xmlTestParameterValue.TestParameterName,
							xmlQualityCondition.Name));
				}

				TestParameterValue parameterValue;

				var datasetValue = xmlTestParameterValue as XmlDatasetTestParameterValue;
				if (datasetValue != null)
				{
					parameterValue = XmlDataQualityUtils.CreateDatasetTestParameterValue(
						testParameter, datasetValue,
						Assert.NotNullOrEmpty(xmlQualityCondition.Name),
						modelsByWorkspaceId, getDatasetsByName, new TestParameterDatasetValidator(),
						ignoreForUnknownDatasets);

					if (parameterValue == null)
					{
						unknownDatasetParameters.Add(
							new DatasetTestParameterRecord(datasetValue.Value,
							                               datasetValue.WorkspaceId));
					}
				}
				else
				{
					var scalarValue = xmlTestParameterValue as XmlScalarTestParameterValue;
					if (scalarValue != null)
					{
						parameterValue = CreateScalarTestParameterValue(testParameter, scalarValue);
					}
					else
					{
						throw new InvalidProgramException("Unhandled TestParameterValue " +
						                                  xmlTestParameterValue.TestParameterName);
					}
				}

				if (parameterValue != null)
				{
					result.AddParameterValue(parameterValue);
				}
			}

			if (unknownDatasetParameters.Count > 0)
			{
				Assert.True(ignoreForUnknownDatasets, "ignoreForUnknownDatasets");

				return null;
			}

			return result;
		}

		private static void UpdateQualityCondition([NotNull] QualityCondition qualityCondition,
		                                           [NotNull] XmlQualityCondition xmlCondition,
		                                           [CanBeNull] DataQualityCategory category)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));
			Assert.ArgumentNotNull(xmlCondition, nameof(xmlCondition));

			UpdateInstanceConfiguration(qualityCondition, xmlCondition, category);

			qualityCondition.AllowErrorsOverride =
				TranslateOverride(xmlCondition.AllowErrors);
			qualityCondition.StopOnErrorOverride =
				TranslateOverride(xmlCondition.StopOnError);
			qualityCondition.NeverFilterTableRowsUsingRelatedGeometry =
				xmlCondition.NeverFilterTableRowsUsingRelatedGeometry;
			qualityCondition.NeverStoreRelatedGeometryForTableRowIssues =
				xmlCondition.NeverStoreRelatedGeometryForTableRowIssues;

			string versionUuid = xmlCondition.VersionUuid;
			if (StringUtils.IsNotEmpty(versionUuid))
			{
				qualityCondition.VersionUuid = versionUuid;
			}
		}

		private static void UpdateInstanceConfiguration<T>(
			[NotNull] T instanceConfiguration,
			[NotNull] XmlInstanceConfiguration xmlInstanceConfiguration,
			[CanBeNull] DataQualityCategory category)
			where T : InstanceConfiguration
		{
			Assert.ArgumentNotNull(instanceConfiguration, nameof(instanceConfiguration));
			Assert.ArgumentNotNull(xmlInstanceConfiguration, nameof(xmlInstanceConfiguration));

			instanceConfiguration.Name = xmlInstanceConfiguration.Name;
			instanceConfiguration.Description = xmlInstanceConfiguration.Description;
			instanceConfiguration.Notes = xmlInstanceConfiguration.Notes;
			instanceConfiguration.Url = xmlInstanceConfiguration.Url;

			string uuid = xmlInstanceConfiguration.Uuid;
			if (StringUtils.IsNotEmpty(uuid))
			{
				instanceConfiguration.Uuid = uuid;
			}

			ImportMetadata(instanceConfiguration, xmlInstanceConfiguration);

			instanceConfiguration.Category = category;
		}

		[NotNull]
		private static TestParameterValue CreateScalarTestParameterValue(
			[NotNull] TestParameter testParameter,
			[NotNull] XmlScalarTestParameterValue xmlScalarTestParameterValue)
		{
			return new ScalarTestParameterValue(testParameter, xmlScalarTestParameterValue.Value);
		}

		private static void ImportMetadata([NotNull] IEntityMetadata entity,
		                                   [NotNull] IXmlEntityMetadata fromXml)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));
			Assert.ArgumentNotNull(fromXml, nameof(fromXml));

			// created

			if (! string.IsNullOrEmpty(fromXml.CreatedByUser))
			{
				entity.CreatedByUser = fromXml.CreatedByUser;
			}

			DateTime? xmlCreatedDate = ParseDateTime(fromXml.CreatedDate);

			if (xmlCreatedDate != null &&
			    ! AreEqual(entity.CreatedDate, xmlCreatedDate))
			{
				entity.CreatedDate = xmlCreatedDate;
			}

			// last changed 

			if (! string.IsNullOrEmpty(fromXml.LastChangedByUser))
			{
				entity.LastChangedByUser = fromXml.LastChangedByUser;
			}

			DateTime? xmlLastChangedDate = ParseDateTime(fromXml.LastChangedDate);
			if (xmlLastChangedDate != null &&
			    ! AreEqual(entity.LastChangedDate, xmlLastChangedDate))
			{
				entity.LastChangedDate = xmlLastChangedDate;
			}
		}

		#region Utils

		[CanBeNull]
		private static string Format(DateTime? dateTime)
		{
			return dateTime?.ToString("s", CultureInfo.InvariantCulture);
		}

		[CanBeNull]
		private static DateTime? ParseDateTime([CanBeNull] string dateTimeString)
		{
			return string.IsNullOrEmpty(dateTimeString)
				       ? (DateTime?) null
				       : DateTime.Parse(dateTimeString,
				                        CultureInfo.InvariantCulture,
				                        DateTimeStyles.AssumeLocal);
		}

		private static bool AreEqual(DateTime? dateTime1, DateTime? dateTime2)
		{
			return Equals(Format(dateTime1), Format(dateTime2));
		}

		private static bool? TranslateOverride(Override value)
		{
			switch (value)
			{
				case Override.Null:
					return null;

				case Override.False:
					return false;

				case Override.True:
					return true;

				default:
					throw new InvalidOperationException("Unhandled Override " + value);
			}
		}

		#endregion
	}
}
