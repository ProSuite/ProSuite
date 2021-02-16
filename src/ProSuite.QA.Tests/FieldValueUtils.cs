using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	internal static class FieldValueUtils
	{
		[NotNull]
		public static string GetTypeConversionErrorDescription(
			[NotNull] ITable table,
			[CanBeNull] object foreignKey,
			[NotNull] string fkField,
			[NotNull] string pkField,
			[NotNull] string errorMessage)
		{
			return string.Format(
				"Unable to convert value [{0}] in field '{1}' to the type of field '{2}' in table '{3}': {4}",
				FormatValue(foreignKey), fkField, pkField,
				DatasetUtils.GetName(table),
				errorMessage);
		}

		[NotNull]
		public static string FormatValue([CanBeNull] object value)
		{
			if (value == null || value is DBNull)
			{
				return "<null>";
			}

			return value is string
				       ? string.Format("'{0}'", value)
				       : value.ToString();
		}
	}
}
