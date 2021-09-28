using System;
using System.Collections.Generic;
using System.Globalization;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Xml;
using ProSuite.DomainServices.AO.QA.DatasetReports.Xml;

namespace ProSuite.DomainServices.AO.QA.DatasetReports
{
	public class ObjectClassReportBuilder
	{
		private const int _defaultMaximumReportedDistinctValuesCount = 100;

		public ObjectClassReportBuilder()
		{
			ReportDistinctValues = true;
			ExcludeUniqueDistinctValues = true;
			ExcludeDistinctValuesForCodedValueDomains = true;
			MaximumReportedDistinctValuesCount = _defaultMaximumReportedDistinctValuesCount;
			ReportDistinctValuesForEditableFieldsOnly = true;
		}

		public bool ReportDistinctValues { get; set; }

		public bool ExcludeUniqueDistinctValues { get; set; }

		public bool ExcludeDistinctValuesForCodedValueDomains { get; set; }

		public int MaximumReportedDistinctValuesCount { get; set; }

		public bool ReportDistinctValuesForEditableFieldsOnly { get; set; }

		[NotNull]
		public TableReport CreateReport([NotNull] ITable table,
		                                [CanBeNull] string catalogName)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			var result = new TableReport();

			BuildStatistics(table, result, catalogName);

			return result;
		}

		private void BuildStatistics([NotNull] ITable table,
		                             [NotNull] ObjectClassReport objectClassReport,
		                             [CanBeNull] string catalogName)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(objectClassReport, nameof(objectClassReport));

			objectClassReport.CatalogName = catalogName;
			objectClassReport.Name = DatasetUtils.GetName(table);

			var objectclass = table as IObjectClass;
			if (objectclass != null)
			{
				objectClassReport.AliasName = objectclass.AliasName;

				string subtypeField = DatasetUtils.GetSubtypeFieldName(objectclass);
				objectClassReport.SubtypeField = string.IsNullOrEmpty(subtypeField)
					                                 ? null
					                                 : subtypeField;
			}

			IWorkspace workspace = DatasetUtils.GetWorkspace(table);

			IGeodatabaseRelease gdbRelease;
			if (WorkspaceUtils.HasGeodatabaseReleaseInformation(workspace, out gdbRelease))
			{
				objectClassReport.GeodatabaseRelease = FormatGeodatabaseRelease(gdbRelease);
				objectClassReport.IsCurrentGeodatabaseRelease = gdbRelease.CurrentRelease;
			}

			var versionedObject = table as IVersionedObject;
			if (versionedObject != null)
			{
				objectClassReport.IsWorkspaceVersioned = true;
				objectClassReport.IsRegisteredAsVersioned =
					versionedObject.IsRegisteredAsVersioned;
				objectClassReport.VersionName = versionedObject.Version.VersionName;
			}

			var fieldDescriptorsByFieldIndex = new Dictionary<int, FieldDescriptor>();
			var distinctValuesByFieldIndex = new Dictionary<int, DistinctValues<object>>();
			var valueRangeByFieldIndex = new Dictionary<int, FieldValueRange>();

			foreach (IField field in DatasetUtils.GetFields(table))
			{
				FieldDescriptor fieldDescriptor = GetFieldDescriptor(field);
				objectClassReport.AddField(fieldDescriptor);

				int fieldIndex = table.FindField(field.Name);

				fieldDescriptorsByFieldIndex.Add(fieldIndex, fieldDescriptor);

				if (! CanGetValueRange(field.Type))
				{
					continue;
				}

				valueRangeByFieldIndex.Add(fieldIndex, new FieldValueRange());

				if (CollectDistinctValuesForField(field))
				{
					distinctValuesByFieldIndex.Add(fieldIndex, new DistinctValues<object>());
				}
			}

			int fieldCount = table.Fields.FieldCount;
			const bool recycle = true;
			foreach (IRow row in GdbQueryUtils.GetRows(table, recycle))
			{
				objectClassReport.AddRow(row);

				for (var fieldIndex = 0; fieldIndex < fieldCount; fieldIndex++)
				{
					FieldValueRange fieldValueRange;
					if (valueRangeByFieldIndex.TryGetValue(fieldIndex, out fieldValueRange))
					{
						fieldValueRange.Add(row.Value[fieldIndex]);
					}

					DistinctValues<object> distinctValues;
					if (distinctValuesByFieldIndex.TryGetValue(fieldIndex, out distinctValues))
					{
						distinctValues.Add(row.Value[fieldIndex]);
					}
				}
			}

			foreach (KeyValuePair<int, FieldDescriptor> pair in fieldDescriptorsByFieldIndex)
			{
				int fieldIndex = pair.Key;
				FieldDescriptor fieldDescriptor = pair.Value;

				FieldValueRange fieldValueRange;
				if (! valueRangeByFieldIndex.TryGetValue(fieldIndex, out fieldValueRange))
				{
					continue;
				}

				FieldStatistics fieldStatistics = GetFieldStatistics(fieldValueRange);
				fieldDescriptor.Statistics = fieldStatistics;

				DistinctValues<object> distinctValues;
				if (! distinctValuesByFieldIndex.TryGetValue(fieldIndex, out distinctValues))
				{
					continue;
				}

				fieldStatistics.DistinctValues = GetFieldDistinctValues(distinctValues);
			}
		}

		[NotNull]
		private static string FormatGeodatabaseRelease(
			[NotNull] IGeodatabaseRelease gdbRelease)
		{
			const int majorVersionOffset = 7; // version 1 was for arcgis 8

			return string.Format("{0}.{1}.{2}",
			                     gdbRelease.MajorVersion + majorVersionOffset,
			                     gdbRelease.MinorVersion,
			                     gdbRelease.BugfixVersion);
		}

		[NotNull]
		private FieldDistinctValues GetFieldDistinctValues(
			[NotNull] DistinctValues<object> distinctValues)
		{
			Assert.ArgumentNotNull(distinctValues, nameof(distinctValues));

			var uniqueValuesCount = 0;
			var distinctValueCount = 0;
			var maximumValueCountExceeded = false;

			var result = new FieldDistinctValues();
			foreach (DistinctValue<object> distinctValue in distinctValues.Values)
			{
				distinctValueCount++;
				if (distinctValue.Count == 1)
				{
					uniqueValuesCount++;
				}

				if (! ReportDistinctValues)
				{
					continue;
				}

				if (ExcludeUniqueDistinctValues && distinctValue.Count <= 1)
				{
					continue;
				}

				if (distinctValueCount > MaximumReportedDistinctValuesCount)
				{
					maximumValueCountExceeded = true;
				}
				else
				{
					result.Add(new FieldDistinctValue(distinctValue));
				}
			}

			result.UniqueValuesExcluded =
				ReportDistinctValues && ExcludeUniqueDistinctValues && uniqueValuesCount > 0;
			result.MaximumReportedValueCountExceeded =
				ReportDistinctValues && maximumValueCountExceeded;

			result.DistinctValueCount = distinctValueCount;
			result.UniqueValuesCount = uniqueValuesCount;
			result.SortDistinctValues();

			return result;
		}

		[NotNull]
		private static FieldStatistics GetFieldStatistics(
			[NotNull] FieldValueRange fieldValueRange)
		{
			return new FieldStatistics
			       {
				       MinimumValue = FormatValue(fieldValueRange.MinimumValue),
				       MaximumValue = FormatValue(fieldValueRange.MaximumValue),
				       NullValueCount = fieldValueRange.NullCount,
				       ValueCount = fieldValueRange.ValueCount
			       };
		}

		[NotNull]
		private static string FormatValue([CanBeNull] object value)
		{
			if (value == null || value is DBNull)
			{
				return string.Empty;
			}

			var stringValue = value as string;

			return stringValue != null
				       ? XmlUtils.EscapeInvalidCharacters(stringValue)
				       : string.Format(CultureInfo.InvariantCulture, "{0}", value);
		}

		private static bool CanGetValueRange(esriFieldType esriFieldType)
		{
			switch (esriFieldType)
			{
				case esriFieldType.esriFieldTypeOID:
				case esriFieldType.esriFieldTypeSmallInteger:
				case esriFieldType.esriFieldTypeInteger:
				case esriFieldType.esriFieldTypeSingle:
				case esriFieldType.esriFieldTypeDouble:
				case esriFieldType.esriFieldTypeString:
				case esriFieldType.esriFieldTypeDate:
				case esriFieldType.esriFieldTypeGUID:
				case esriFieldType.esriFieldTypeGlobalID:
					return true;

				case esriFieldType.esriFieldTypeGeometry:
				case esriFieldType.esriFieldTypeBlob:
				case esriFieldType.esriFieldTypeRaster:
				case esriFieldType.esriFieldTypeXML:
					return false;

				default:
					throw new ArgumentOutOfRangeException(nameof(esriFieldType));
			}
		}

		private bool CollectDistinctValuesForField([NotNull] IField field)
		{
			if (ReportDistinctValuesForEditableFieldsOnly && ! field.Editable)
			{
				return false;
			}

			if (! CanGetDistinctValues(field.Type))
			{
				return false;
			}

			if (ExcludeDistinctValuesForCodedValueDomains && field.Domain is ICodedValueDomain)
			{
				// there is a coded value domain on the field
				return false;
			}

			return true;
		}

		private static bool CanGetDistinctValues(esriFieldType esriFieldType)
		{
			switch (esriFieldType)
			{
				case esriFieldType.esriFieldTypeOID:
				case esriFieldType.esriFieldTypeSmallInteger:
				case esriFieldType.esriFieldTypeInteger:
				case esriFieldType.esriFieldTypeSingle:
				case esriFieldType.esriFieldTypeDouble:
				case esriFieldType.esriFieldTypeString:
				case esriFieldType.esriFieldTypeDate:
				case esriFieldType.esriFieldTypeGUID:
				case esriFieldType.esriFieldTypeGlobalID:
					return true;

				case esriFieldType.esriFieldTypeGeometry:
				case esriFieldType.esriFieldTypeBlob:
				case esriFieldType.esriFieldTypeRaster:
				case esriFieldType.esriFieldTypeXML:
					return false;

				default:
					throw new ArgumentOutOfRangeException(nameof(esriFieldType));
			}
		}

		[NotNull]
		private static FieldDescriptor GetFieldDescriptor([NotNull] IField field)
		{
			return new FieldDescriptor
			       {
				       Name = field.Name,
				       AliasName = field.AliasName,
				       Type = field.Type,
				       Length = field.Length,
				       Precision = field.Precision,
				       Scale = field.Scale,
				       Editable = field.Editable,
				       IsNullable = field.IsNullable,
				       DomainName = field.Domain?.Name
			       };
		}

		[NotNull]
		public FeatureClassReport CreateReport([NotNull] IFeatureClass featureClass,
		                                       [CanBeNull] string catalogName)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			var result = new FeatureClassReport();

			BuildStatistics((ITable) featureClass, result, catalogName);

			result.FeatureType = featureClass.FeatureType;
			result.ShapeType = featureClass.ShapeType;
			result.HasZ = DatasetUtils.HasZ(featureClass);
			result.HasM = DatasetUtils.HasM(featureClass);

			result.SpatialReference =
				XmlSpatialReferenceDescriptorUtils.CreateXmlSpatialReferenceDescriptor(
					featureClass);

			return result;
		}
	}
}
