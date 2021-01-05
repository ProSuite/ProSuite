using System;
using System.Collections.Generic;
using System.Reflection;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public abstract class AttributeWriterBase
	{
		[NotNull] private readonly IDictionary<int, bool> _fieldNullabilityByIndex;
		[NotNull] private readonly IDictionary<int, int> _fieldLengthByIndex;
		[NotNull] private readonly IDictionary<int, string> _fieldNameByIndex;
		private readonly int _maxFieldLength;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		[CLSCompliant(false)]
		protected AttributeWriterBase([NotNull] ITable table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			// TODO insufficient!!
			bool isDbfTable = DatasetUtils.GetWorkspace(table).Type ==
			                  esriWorkspaceType.esriFileSystemWorkspace;

			const int dbfMaxFieldLength = 10;
			_maxFieldLength = isDbfTable
				                  ? dbfMaxFieldLength
				                  : int.MaxValue;

			GetFieldProperties(table,
			                   out _fieldLengthByIndex,
			                   out _fieldNameByIndex,
			                   out _fieldNullabilityByIndex);
		}

		[CLSCompliant(false)]
		protected int Find([NotNull] ITable table,
		                   [NotNull] string fieldName)
		{
			int index = table.FindField(fieldName);

			if (index >= 0)
			{
				return index;
			}

			if (fieldName.Length > _maxFieldLength)
			{
				string shortenedFieldName = fieldName.Substring(0, _maxFieldLength);

				index = table.FindField(shortenedFieldName);

				if (index >= 0)
				{
					return index;
				}
			}

			throw new ArgumentException(string.Format("Field '{0}' not found in '{1}'",
			                                          fieldName, DatasetUtils.GetName(table)),
			                            nameof(fieldName));
		}

		[CLSCompliant(false)]
		protected void WriteDouble([NotNull] IRowBuffer rowBuffer,
		                           int fieldIndex,
		                           [CanBeNull] double? value)
		{
			if (value == null)
			{
				rowBuffer.Value[fieldIndex] = _fieldNullabilityByIndex[fieldIndex]
					                              ? (object) DBNull.Value
					                              : 0;
			}
			else
			{
				rowBuffer.Value[fieldIndex] = value.Value;
			}
		}

		[CLSCompliant(false)]
		protected void WriteText([NotNull] IRowBuffer rowBuffer,
		                         int fieldIndex,
		                         [CanBeNull] string value,
		                         bool warnIfTooLong = true)
		{
			Assert.ArgumentNotNull(rowBuffer, nameof(rowBuffer));

			if (value == null)
			{
				rowBuffer.Value[fieldIndex] = _fieldNullabilityByIndex[fieldIndex]
					                              ? (object) DBNull.Value
					                              : string.Empty;
				return;
			}

			int length = _fieldLengthByIndex[fieldIndex];
			bool requiresTrim = value.Length > length;

			if (requiresTrim && warnIfTooLong)
			{
				_msg.WarnFormat("Text is too long for field '{0}', cutting off: {1}",
				                _fieldNameByIndex[fieldIndex], value);
			}

			string writeValue = requiresTrim
				                    ? value.Substring(0, length)
				                    : value;

			rowBuffer.Value[fieldIndex] = writeValue;
		}

		[NotNull]
		protected static string GetTestTypeName([NotNull] QualityCondition qualityCondition)
		{
			TestDescriptor testDescriptor = qualityCondition.TestDescriptor;

			if (testDescriptor == null)
			{
				return string.Empty;
			}

			if (! string.IsNullOrEmpty(testDescriptor.TestFactoryDescriptor?.TypeName))
			{
				return testDescriptor.TestFactoryDescriptor.TypeName;
			}

			if (! string.IsNullOrEmpty(testDescriptor.TestClass?.TypeName))
			{
				return string.Format("{0}[{1}]",
				                     testDescriptor.TestClass.TypeName,
				                     testDescriptor.TestConstructorId);
			}

			return string.Empty;
		}

		[NotNull]
		protected static string GetTestName([NotNull] QualityCondition qualityCondition)
		{
			TestDescriptor testDescriptor = qualityCondition.TestDescriptor;

			return testDescriptor == null
				       ? string.Empty
				       : (testDescriptor.Name ?? string.Empty);
		}

		[NotNull]
		protected static string GetTestDescription(
			[NotNull] QualityCondition qualityCondition)
		{
			TestDescriptor testDescriptor = qualityCondition.TestDescriptor;

			return testDescriptor == null
				       ? string.Empty
				       : (testDescriptor.Description ?? string.Empty);
		}

		[NotNull]
		protected static string GetCategoryValue(
			[NotNull] QualityCondition qualityCondition)
		{
			DataQualityCategory category = qualityCondition.Category;
			return category == null
				       ? string.Empty
				       : (GetCategoryText(category) ?? string.Empty);
		}

		[CanBeNull]
		private static string GetCategoryText([NotNull] DataQualityCategory category)
		{
			return StringUtils.IsNotEmpty(category.Abbreviation)
				       ? category.Abbreviation
				       : category.GetQualifiedName();
		}

		private static void GetFieldProperties(
			[NotNull] ITable table,
			[NotNull] out IDictionary<int, int> fieldLengthByIndex,
			[NotNull] out IDictionary<int, string> fieldNamesByIndex,
			[NotNull] out IDictionary<int, bool> fieldNullabilityByIndex)
		{
			fieldLengthByIndex = new Dictionary<int, int>();
			fieldNamesByIndex = new Dictionary<int, string>();
			fieldNullabilityByIndex = new Dictionary<int, bool>();

			IFields fields = table.Fields;
			int fieldCount = fields.FieldCount;
			for (var fieldIndex = 0; fieldIndex < fieldCount; fieldIndex++)
			{
				IField field = fields.Field[fieldIndex];

				fieldLengthByIndex.Add(fieldIndex, field.Length);
				fieldNamesByIndex.Add(fieldIndex, field.Name);
				fieldNullabilityByIndex.Add(fieldIndex, field.IsNullable);
			}
		}

		[CanBeNull]
		protected static string GetUrl([NotNull] QualityCondition qualityCondition)
		{
			string url = qualityCondition.Url;

			return StringUtils.IsNullOrEmptyOrBlank(url)
				       ? null
				       : GetUrlLink(url);
		}

		[NotNull]
		private static string GetUrlLink([NotNull] string url)
		{
			Assert.ArgumentNotNullOrEmpty(url, nameof(url));

			return url.IndexOf("://", StringComparison.OrdinalIgnoreCase) > 0
				       ? url
				       : $"http://{url}";
		}
	}
}
