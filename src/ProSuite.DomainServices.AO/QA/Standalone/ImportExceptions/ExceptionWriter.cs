using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.QA.Issues;

namespace ProSuite.DomainServices.AO.QA.Standalone.ImportExceptions
{
	internal class ExceptionWriter : IDisposable
	{
		[NotNull] private readonly ITable _targetTable;
		[NotNull] private readonly IIssueTableFields _targetFields;

		[NotNull] private readonly IDictionary<int, int>
			_targetFieldIndicesByImportFieldIndex;

		[NotNull] private readonly IDictionary<IssueAttribute, int> _attributeFieldIndexes =
			new Dictionary<IssueAttribute, int>();

		[NotNull] private readonly IDictionary<int, int> _fieldLengths;

		[CanBeNull] private ICursor _insertCursor;
		[CanBeNull] private IRowBuffer _rowBuffer;
		[CanBeNull] private IFeatureBuffer _featureBuffer;
		private readonly int _lineageUuidFieldIndex;
		private readonly int _versionBeginDateFieldIndex;
		private readonly int _versionEndDateFieldIndex;
		private readonly int _versionUuidFieldIndex;
		private readonly int _versionOriginFieldIndex;
		private readonly int _originFieldIndex;
		private readonly int _statusFieldIndex;
		private readonly int _versionImportStatusIndex;

		public ExceptionWriter([NotNull] ITable importTable,
		                       [NotNull] IIssueTableFields importFields,
		                       [NotNull] ITable targetTable,
		                       [NotNull] IIssueTableFields targetFields)
		{
			Assert.ArgumentNotNull(importTable, nameof(importTable));
			Assert.ArgumentNotNull(importFields, nameof(importFields));
			Assert.ArgumentNotNull(targetTable, nameof(targetTable));
			Assert.ArgumentNotNull(targetFields, nameof(targetFields));

			_targetTable = targetTable;
			_targetFields = targetFields;

			_lineageUuidFieldIndex = targetFields.GetIndex(
				IssueAttribute.ManagedExceptionLineageUuid, targetTable);
			_versionBeginDateFieldIndex = targetFields.GetIndex(
				IssueAttribute.ManagedExceptionVersionBeginDate, targetTable);
			_versionEndDateFieldIndex = targetFields.GetIndex(
				IssueAttribute.ManagedExceptionVersionEndDate, targetTable);
			_versionUuidFieldIndex = targetFields.GetIndex(
				IssueAttribute.ManagedExceptionVersionUuid, targetTable);
			_versionOriginFieldIndex = targetFields.GetIndex(
				IssueAttribute.ManagedExceptionVersionOrigin, targetTable);

			_originFieldIndex = targetFields.GetIndex(
				IssueAttribute.ManagedExceptionOrigin, targetTable);
			_statusFieldIndex = targetFields.GetIndex(
				IssueAttribute.ExceptionStatus, targetTable);
			_versionImportStatusIndex = targetFields.GetIndex(
				IssueAttribute.ManagedExceptionVersionImportStatus, targetTable);

			_fieldLengths = GetFieldLengths(targetTable);

			var importFeatureClass = importTable as IFeatureClass;
			var targetFeatureClass = targetTable as IFeatureClass;

			if (importFeatureClass != null)
			{
				Assert.NotNull(targetFeatureClass);

				ValidateSpatialReferences(targetFeatureClass, importFeatureClass);
			}

			_targetFieldIndicesByImportFieldIndex = GetFieldIndexMap(importTable, importFields,
			                                                         targetTable,
			                                                         targetFields);
		}

		public void Dispose()
		{
			if (_insertCursor != null)
			{
				Flush();
				ComUtils.ReleaseComObject(_insertCursor);
				_insertCursor = null;
			}

			_rowBuffer = null;
			_featureBuffer = null;
		}

		public void Write([NotNull] IRow importExceptionRow,
		                  DateTime importDate,
		                  [NotNull] string originValue,
		                  Guid lineageGuid,
		                  string versionOriginValue,
		                  string statusValue)
		{
			if (_insertCursor == null)
			{
				_insertCursor = _targetTable.Insert(true);
				_rowBuffer = _targetTable.CreateRowBuffer();
				_featureBuffer = _rowBuffer as IFeatureBuffer;
			}

			IRowBuffer buffer = Assert.NotNull(_rowBuffer);

			TransferAttributes(importExceptionRow, buffer);

			WriteText(buffer, _originFieldIndex, originValue);
			buffer.Value[_versionBeginDateFieldIndex] = importDate;
			buffer.Value[_versionEndDateFieldIndex] = DBNull.Value;
			buffer.Value[_versionUuidFieldIndex] = GetNewVersionUuid();
			WriteText(buffer, _versionOriginFieldIndex, versionOriginValue);
			buffer.Value[_lineageUuidFieldIndex] =
				ExceptionObjectUtils.FormatGuid(lineageGuid);
			buffer.Value[_statusFieldIndex] = statusValue;

			if (_featureBuffer != null)
			{
				_featureBuffer.Shape = ((IFeature) importExceptionRow).ShapeCopy;
			}

			_insertCursor?.InsertRow(buffer);
		}

		public void Write([NotNull] IRow updateExceptionRow,
		                  DateTime updateDate,
		                  [NotNull] ManagedExceptionVersion managedExceptionVersion,
		                  [NotNull] string originValue,
		                  [NotNull] string versionOriginValue,
		                  [CanBeNull] string versionImportStatus)
		{
			Assert.ArgumentNotNull(updateExceptionRow, nameof(updateExceptionRow));
			Assert.ArgumentNotNull(managedExceptionVersion, nameof(managedExceptionVersion));
			Assert.ArgumentNotNullOrEmpty(originValue, nameof(originValue));
			Assert.ArgumentNotNullOrEmpty(versionOriginValue, nameof(versionOriginValue));

			if (_insertCursor == null)
			{
				_insertCursor = _targetTable.Insert(true);
				_rowBuffer = _targetTable.CreateRowBuffer();
				_featureBuffer = _rowBuffer as IFeatureBuffer;
			}

			IRowBuffer buffer = Assert.NotNull(_rowBuffer);

			TransferAttributes(updateExceptionRow, buffer);

			WriteText(buffer, _originFieldIndex, originValue);
			buffer.Value[_versionBeginDateFieldIndex] = updateDate;
			buffer.Value[_versionEndDateFieldIndex] = DBNull.Value; // even if 'Inactive'
			buffer.Value[_versionUuidFieldIndex] = GetNewVersionUuid();
			WriteText(buffer, _versionOriginFieldIndex, versionOriginValue);
			buffer.Value[_lineageUuidFieldIndex] =
				ExceptionObjectUtils.FormatGuid(managedExceptionVersion.LineageUuid);
			WriteText(buffer, _versionImportStatusIndex, versionImportStatus);

			foreach (IssueAttribute attribute in managedExceptionVersion.EditableAttributes)
			{
				WriteValue(managedExceptionVersion, attribute, buffer);
			}

			if (_featureBuffer != null)
			{
				_featureBuffer.Shape = ((IFeature) updateExceptionRow).ShapeCopy;
			}

			_insertCursor?.InsertRow(buffer);
		}

		public void Flush()
		{
			_insertCursor?.Flush();
		}

		[NotNull]
		private static string GetNewVersionUuid()
		{
			return ExceptionObjectUtils.FormatGuid(Guid.NewGuid());
		}

		[NotNull]
		private static IDictionary<int, int> GetFieldLengths([NotNull] ITable targetTable)
		{
			IFields fields = targetTable.Fields;
			int fieldCount = fields.FieldCount;

			var result = new Dictionary<int, int>();

			for (var idx = 0; idx < fieldCount; idx++)
			{
				result.Add(idx, fields.Field[idx].Length);
			}

			return result;
		}

		private void WriteValue([NotNull] ManagedExceptionVersion managedExceptionVersion,
		                        IssueAttribute attribute,
		                        [NotNull] IRowBuffer buffer)
		{
			int fieldIndex = GetFieldIndex(attribute);

			object rawValue = managedExceptionVersion.GetValue(attribute);

			buffer.Value[fieldIndex] = FormatValue(rawValue);
		}

		private static object FormatValue(object rawValue)
		{
			if (rawValue == null)
			{
				return DBNull.Value;
			}

			if (rawValue is Guid)
			{
				return ExceptionObjectUtils.FormatGuid((Guid) rawValue);
			}

			return rawValue;
		}

		private int GetFieldIndex(IssueAttribute attribute)
		{
			int fieldIndex;
			if (! _attributeFieldIndexes.TryGetValue(attribute, out fieldIndex))
			{
				fieldIndex = _targetFields.GetIndex(attribute, _targetTable);
				_attributeFieldIndexes.Add(attribute, fieldIndex);
			}

			return fieldIndex;
		}

		private static void ValidateSpatialReferences(
			[NotNull] IFeatureClass targetFeatureClass,
			[NotNull] IFeatureClass importFeatureClass)
		{
			if (! SpatialReferenceUtils.AreEqual(
				    DatasetUtils.GetSpatialReference(targetFeatureClass),
				    DatasetUtils.GetSpatialReference(importFeatureClass),
				    comparePrecisionAndTolerance: false,
				    compareVerticalCoordinateSystems: false))
			{
				// TODO throw exception
			}
		}

		private void TransferAttributes([NotNull] IRow importRow,
		                                [NotNull] IRowBuffer targetRowBuffer)
		{
			foreach (KeyValuePair<int, int> pair in _targetFieldIndicesByImportFieldIndex)
			{
				int importFieldIndex = pair.Key;
				int targetFieldIndex = pair.Value;

				targetRowBuffer.Value[targetFieldIndex] = importRow.Value[importFieldIndex];
			}
		}

		private void WriteText([NotNull] IRowBuffer rowBuffer,
		                       int fieldIndex,
		                       [CanBeNull] string value)
		{
			Assert.ArgumentNotNull(rowBuffer, nameof(rowBuffer));

			object writeValue;

			if (value == null)
			{
				writeValue = DBNull.Value;
			}
			else
			{
				int length = _fieldLengths[fieldIndex];

				bool requiresTrim = value.Length > length;

				writeValue = requiresTrim
					             ? value.Substring(0, length)
					             : value;
			}

			rowBuffer.Value[fieldIndex] = writeValue;
		}

		[NotNull]
		private static IDictionary<int, int> GetFieldIndexMap(
			[NotNull] ITable importTable, [NotNull] IIssueTableFields importFields,
			[NotNull] ITable targetTable, [NotNull] IIssueTableFields targetFields)
		{
			var result = new Dictionary<int, int>();

			var mappedTargetIndices = new HashSet<int>();
			foreach (IssueAttribute issueAttribute in EnumUtils.GetList<IssueAttribute>())
			{
				int importIndex = importFields.GetIndex(issueAttribute, importTable,
				                                        optional: true);
				if (importIndex < 0)
				{
					continue;
				}

				int targetIndex = targetFields.GetIndex(issueAttribute, targetTable,
				                                        optional: true);
				if (targetIndex < 0)
				{
					continue;
				}

				result.Add(importIndex, targetIndex);
				mappedTargetIndices.Add(targetIndex);
			}

			// map additional attributes, if field name and type are equal and if target field is editable
			int importFieldCount = importTable.Fields.FieldCount;

			for (var importIndex = 0; importIndex < importFieldCount; importIndex++)
			{
				if (result.ContainsKey(importIndex))
				{
					// already mapped
					continue;
				}

				IField importField = importTable.Fields.Field[importIndex];

				if (! CanTransfer(importField.Type))
				{
					continue;
				}

				int targetIndex = GetMatchingTargetFieldIndex(importField, targetTable);

				if (targetIndex < 0 || mappedTargetIndices.Contains(targetIndex))
				{
					// target field already mapped to an import field
					continue;
				}

				result.Add(importIndex, targetIndex);
			}

			return result;
		}

		private static int GetMatchingTargetFieldIndex([NotNull] IField importField,
		                                               [NotNull] ITable targetTable)
		{
			int targetIndex = targetTable.FindField(importField.Name);
			if (targetIndex < 0)
			{
				// no target field with same name
				return -1;
			}

			IField targetField = targetTable.Fields.Field[targetIndex];

			return targetField.Editable && targetField.Type == importField.Type
				       ? targetIndex
				       : -1;
		}

		private static bool CanTransfer(esriFieldType fieldType)
		{
			switch (fieldType)
			{
				case esriFieldType.esriFieldTypeSmallInteger:
				case esriFieldType.esriFieldTypeInteger:
				case esriFieldType.esriFieldTypeSingle:
				case esriFieldType.esriFieldTypeDouble:
				case esriFieldType.esriFieldTypeString:
				case esriFieldType.esriFieldTypeDate:
				case esriFieldType.esriFieldTypeGUID:
					return true;

				case esriFieldType.esriFieldTypeOID:
				case esriFieldType.esriFieldTypeGeometry:
				case esriFieldType.esriFieldTypeBlob:
				case esriFieldType.esriFieldTypeRaster:
				case esriFieldType.esriFieldTypeGlobalID:
				case esriFieldType.esriFieldTypeXML:
					return false;

				default:
					return false;
			}
		}
	}
}
