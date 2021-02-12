using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DomainServices.AO.QA.Issues;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public abstract class ExceptionObjectFactoryBase
	{
		[NotNull] private readonly ITable _table;
		[NotNull] private readonly IIssueTableFields _fields;

		protected ExceptionObjectFactoryBase([NotNull] ITable table,
		                                     [NotNull] IIssueTableFields fields)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(fields, nameof(fields));

			_table = table;
			_fields = fields;
		}

		protected int GetIndex(IssueAttribute attribute, bool optional = false)
		{
			return _fields.GetIndex(attribute, _table, optional);
		}

		[CanBeNull]
		protected static object GetValue([NotNull] IRow row, int fieldIndex)
		{
			if (fieldIndex < 0)
			{
				return null;
			}

			object value = row.Value[fieldIndex];
			if (value == null || value is DBNull)
			{
				// NOTE: can be null (not DBNull) in case of shapefiles
				return null;
			}

			return value;
		}

		[CanBeNull]
		protected static string GetString([NotNull] IRow row, int fieldIndex)
		{
			if (fieldIndex < 0)
			{
				return null;
			}

			object value = row.Value[fieldIndex];
			if (value == null || value is DBNull)
			{
				// NOTE: can be null (not DBNull) in case of shapefiles
				return null;
			}

			return (string) value;
		}

		protected static DateTime? GetDateTime([NotNull] IRow row, int fieldIndex)
		{
			if (fieldIndex < 0)
			{
				return null;
			}

			object value = row.Value[fieldIndex];
			if (value == null || value is DBNull)
			{
				return null;
			}

			var dateTimeValue = (DateTime) value;

			return dateTimeValue;
		}

		protected static Guid? GetGuid([NotNull] IRow row, int fieldIndex)
		{
			if (fieldIndex < 0)
			{
				return null;
			}

			object value = row.Value[fieldIndex];
			if (value == null || value is DBNull)
			{
				return null;
			}

			var stringValue = (string) value;

			if (StringUtils.IsNullOrEmptyOrBlank(stringValue))
			{
				return null;
			}

			return new Guid(stringValue);
		}
	}
}
