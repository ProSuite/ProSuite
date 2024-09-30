using System;
using System.Globalization;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.GIS.Geodatabase.API;

namespace ProSuite.GIS.Geodatabase
{
	public static class GdbSqlUtils
	{
		/// <summary>
		/// Gets the query literal for a given value, given a field type and target workspace.
		/// </summary>
		/// <param name="value">The field value.</param>
		/// <param name="fieldType">Type of the field.</param>
		/// <param name="workspace">The workspace for which the query literal should be valid.</param>
		/// <returns></returns>
		[NotNull]
		public static string GetLiteral([NotNull] object value,
		                                esriFieldType fieldType,
		                                [NotNull] IFeatureWorkspace workspace)
		{
			return GetLiteral(value, fieldType, (IWorkspace) workspace);
		}

		/// <summary>
		/// Gets the query literal for a given value, given a field type and target workspace.
		/// </summary>
		/// <param name="value">The field value.</param>
		/// <param name="fieldType">Type of the field.</param>
		/// <param name="workspace">The workspace for which the query literal should be valid.</param>
		/// <returns></returns>
		[NotNull]
		public static string GetLiteral([NotNull] object value,
		                                esriFieldType fieldType,
		                                [NotNull] IWorkspace workspace)
		{
			switch (fieldType)
			{
				case esriFieldType.esriFieldTypeOID:
				case esriFieldType.esriFieldTypeInteger:
				case esriFieldType.esriFieldTypeSmallInteger:
					return string.Format("{0}", value);

				case esriFieldType.esriFieldTypeDouble:
				case esriFieldType.esriFieldTypeSingle:
					return GetFloatingPointLiteral(value);

				case esriFieldType.esriFieldTypeString:
					return GetStringLiteral(value);

				case esriFieldType.esriFieldTypeDate:
					return GetDateLiteral((DateTime) value, workspace);

				case esriFieldType.esriFieldTypeGUID:
				case esriFieldType.esriFieldTypeGlobalID:
					return GetGuidLiteral(value);

				case esriFieldType.esriFieldTypeGeometry:
				case esriFieldType.esriFieldTypeBlob:
				case esriFieldType.esriFieldTypeRaster:
				case esriFieldType.esriFieldTypeXML:
					throw new ArgumentException(
						string.Format("Unable to get query literal for field type {0}", fieldType),
						nameof(fieldType));

				default:
					throw new ArgumentException(
						string.Format("Unknown field type {0}", fieldType), nameof(fieldType));
			}
		}

		/// <summary>
		/// Gets the date literal that can be used in queries, given a date time value and target workspace.
		/// </summary>
		/// <param name="dateTime">The date time.</param>
		/// <param name="workspace">The workspace for which the date literal should be valid.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException">Unsupported workspace type</exception>
		[NotNull]
		public static string GetDateLiteral(DateTime dateTime,
		                                    [NotNull] IFeatureWorkspace workspace)
		{
			return GetDateLiteral(dateTime, (IWorkspace) workspace);
		}

		/// <summary>
		/// Gets the date literal that can be used in queries, given a date time value and target workspace.
		/// </summary>
		/// <param name="dateTime">The date time.</param>
		/// <param name="workspace">The workspace for which the date literal should be valid.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException">Unsupported workspace type</exception>
		[NotNull]
		public static string GetDateLiteral(DateTime dateTime,
		                                    [NotNull] IWorkspace workspace)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			switch (workspace.Type)
			{
				case esriWorkspaceType.esriLocalDatabaseWorkspace:

					return GetFGDBDateLiteral(dateTime);

				case esriWorkspaceType.esriRemoteDatabaseWorkspace:

					return GetDateLiteral(dateTime, workspace.DbmsType);

				default:
					throw new ArgumentOutOfRangeException(
						string.Format("Unsupported workspace type: {0}", workspace.Type));
			}
		}

		/// <summary>
		/// Gets the date literal that can be used in queries, given a date time value and a dbms type.
		/// </summary>
		/// <param name="dateTime">The date time.</param>
		/// <param name="dbmsType">Type of the DBMS.</param>
		/// <returns></returns>
		/// <exception cref="NotSupportedException">Unsupported dbms type</exception>
		[NotNull]
		public static string GetDateLiteral(DateTime dateTime, esriConnectionDBMS dbmsType)
		{
			switch (dbmsType)
			{
				case esriConnectionDBMS.esriDBMS_Oracle:
					return GetOracleDateLiteral(dateTime);

				case esriConnectionDBMS.esriDBMS_SQLServer:
					return GetSqlServerDateLiteral(dateTime);

				case esriConnectionDBMS.esriDBMS_PostgreSQL:
					return GetPostgreSQLDateLiteral(dateTime);

				default:
					throw new NotSupportedException(
						string.Format("Unsupported dbms type for date queries: {0}", dbmsType));
			}
		}

		[NotNull]
		public static string GetFGDBDateLiteral(DateTime dateTime)
		{
			return $"date '{dateTime:yyyy-MM-dd HH:mm:ss}'";
		}

		[NotNull]
		public static string GetPGDBDateLiteral(DateTime dateTime)
		{
			return $"#{dateTime:MM-dd-yyyy HH:mm:ss}#";
		}

		[NotNull]
		public static string GetSqlServerDateLiteral(DateTime dateTime)
		{
			return $"'{dateTime:yyyy-MM-dd HH:mm:ss}'";
		}

		[NotNull]
		public static string GetPostgreSQLDateLiteral(DateTime dateTime)
		{
			return $"'{dateTime:yyyy-MM-dd HH:mm:ss}'";
		}

		[NotNull]
		public static string GetOracleDateLiteral(DateTime dateValue)
		{
			return $"TO_DATE('{dateValue:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')";
		}

		[NotNull]
		private static string GetFloatingPointLiteral(object value)
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}", value);
		}

		/// <summary>
		/// Gets a value formatted as Guid literal.
		/// </summary>
		/// <param name="value">The value. This may be a Guid or a string containing a valid Guid</param>
		/// <returns></returns>
		[NotNull]
		private static string GetGuidLiteral([NotNull] object value)
		{
			var stringValue = value as string;

			return stringValue != null
				       ? GetStringLiteral(value)
				       : GetGuidLiteral((Guid) value);
		}

		[NotNull]
		private static string GetGuidLiteral(Guid guid)
		{
			return string.Format("'{0}'", guid.ToString("B").ToUpper());
		}

		[NotNull]
		private static string GetStringLiteral([CanBeNull] object value)
		{
			return $"'{value}'";
		}
	}
}
