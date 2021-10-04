using System;
using System.Reflection;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class FieldMapping
	{
		private readonly int _sourceFieldIndex;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		public FieldMapping([NotNull] IField sourceField, int sourceFieldIndex,
		                    [NotNull] IField targetField, int targetFieldIndex)
			: this(sourceField.Name, sourceField.Type, sourceFieldIndex,
			       targetField.Name, targetField.Type, targetFieldIndex) { }

		public FieldMapping([NotNull] string sourceFieldName,
		                    esriFieldType sourceFieldType,
		                    int sourceFieldIndex,
		                    [NotNull] string targetFieldName,
		                    esriFieldType targetFieldType,
		                    int targetFieldIndex)
		{
			Assert.ArgumentNotNullOrEmpty(sourceFieldName, nameof(sourceFieldName));
			Assert.ArgumentNotNullOrEmpty(targetFieldName, nameof(targetFieldName));
			Assert.ArgumentCondition(sourceFieldIndex >= 0, "Invalid source field index: {0}",
			                         sourceFieldIndex);
			Assert.ArgumentCondition(targetFieldIndex >= 0, "Invalid target field index: {0}",
			                         targetFieldIndex);

			SourceFieldName = sourceFieldName;
			TargetFieldName = targetFieldName;
			_sourceFieldIndex = sourceFieldIndex;
			TargetFieldIndex = targetFieldIndex;
			SourceFieldType = sourceFieldType;
			TargetFieldType = targetFieldType;
		}

		[NotNull]
		public string SourceFieldName { get; }

		[NotNull]
		public string TargetFieldName { get; }

		public esriFieldType SourceFieldType { get; }

		public esriFieldType TargetFieldType { get; }

		public int TargetFieldIndex { get; }

		public override string ToString()
		{
			return string.Format("SourceFieldName: {0}, TargetFieldName: {1}, " +
			                     "SourceFieldIndex: {2}, TargetFieldIndex: {3}, " +
			                     "SourceFieldType: {4}, TargetFieldType: {5}",
			                     SourceFieldName, TargetFieldName,
			                     _sourceFieldIndex, TargetFieldIndex,
			                     SourceFieldType, TargetFieldType);
		}

		public void TransferValue(
			[NotNull] IFeature sourceFeature,
			[NotNull] IFeature targetFeature,
			FieldValueTransferLogLevel logLevel = FieldValueTransferLogLevel.Debug)
		{
			Assert.ArgumentNotNull(sourceFeature, nameof(sourceFeature));
			Assert.ArgumentNotNull(targetFeature, nameof(targetFeature));

			object sourceValue = sourceFeature.Value[_sourceFieldIndex];

			LogTransfer(sourceValue, logLevel);

			object targetValue = FieldUtils.ConvertAttributeValue(
				sourceValue, SourceFieldType, TargetFieldType);

			targetFeature.set_Value(TargetFieldIndex, targetValue);
		}

		private void LogTransfer([CanBeNull] object sourceValue,
		                         FieldValueTransferLogLevel logLevel)
		{
			string formattedValue = GetFormattedValue(sourceValue);

			string message =
				string.Format("Transferring value from field '{0}' to field '{1}': {2}",
				              SourceFieldName, TargetFieldName, formattedValue);

			switch (logLevel)
			{
				case FieldValueTransferLogLevel.Debug:
					_msg.Debug(message);
					break;

				case FieldValueTransferLogLevel.VerboseDebug:
					_msg.VerboseDebug(() => message);
					break;

				case FieldValueTransferLogLevel.Info:
					_msg.Info(message);
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(logLevel));
			}
		}

		[NotNull]
		private static string GetFormattedValue([CanBeNull] object sourceValue)
		{
			if (sourceValue == null || sourceValue is DBNull)
			{
				return "<null>";
			}

			return sourceValue is string
				       ? $@"""{sourceValue}"""
				       : sourceValue.ToString();
		}
	}
}
