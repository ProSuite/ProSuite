using System;
using System.Globalization;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.Geodatabase
{
	public static class GdbSqlUtils
	{
		/// <summary>
		/// Gets the query literal for a given value, given a field type and target workspace.
		/// </summary>
		/// <param name="value">The field value.</param>
		/// <param name="fieldType">Type of the field.</param>
		/// <param name="datastore">The workspace for which the query literal should be valid.</param>
		/// <returns></returns>
		[NotNull]
		public static string GetLiteral([NotNull] object value,
		                                FieldType fieldType,
		                                [NotNull] Datastore datastore)
		{
			switch (fieldType)
			{
				case FieldType.OID:
				case FieldType.Integer:
				case FieldType.SmallInteger:
				case FieldType.BigInteger:
					return string.Format("{0}", value);

				case FieldType.Double:
				case FieldType.Single:
					return GetFloatingPointLiteral(value);

				case FieldType.String:
					return GetStringLiteral(value);

				case FieldType.Date:
					return GetDateLiteral((DateTime) value, datastore);

				case FieldType.GUID:
				case FieldType.GlobalID:
					return GetGuidLiteral(value);

				case FieldType.Geometry:
				case FieldType.Blob:
				case FieldType.Raster:
				case FieldType.XML:
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
		/// <param name="datastore">The workspace for which the date literal should be valid.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException">Unsupported workspace type</exception>
		[NotNull]
		public static string GetDateLiteral(DateTime dateTime,
		                                    [NotNull] Datastore datastore)
		{
			Assert.ArgumentNotNull(datastore, nameof(datastore));

			if (datastore is ArcGIS.Core.Data.Geodatabase geodatabase)
			{
				switch (geodatabase.GetGeodatabaseType())
				{
					case GeodatabaseType.LocalDatabase:

						return GetFGDBDateLiteral(dateTime);

					case GeodatabaseType.RemoteDatabase:

						Connector connector = datastore.GetConnector();

						if (connector is DatabaseConnectionProperties databaseConnectionFile)
						{
							return GetDateLiteral(dateTime, databaseConnectionFile.DBMS);
						}

						break;
				}
			}

			throw new ArgumentOutOfRangeException(
				string.Format("Unsupported workspace type: {0}",
				              WorkspaceUtils.GetDatastoreDisplayText(datastore)));
		}

		/// <summary>
		/// Gets the date literal that can be used in queries, given a date time value and a dbms type.
		/// </summary>
		/// <param name="dateTime">The date time.</param>
		/// <param name="dbmsType">Type of the DBMS.</param>
		/// <returns></returns>
		/// <exception cref="NotSupportedException">Unsupported dbms type</exception>
		[NotNull]
		public static string GetDateLiteral(DateTime dateTime, EnterpriseDatabaseType dbmsType)
		{
			switch (dbmsType)
			{
				case EnterpriseDatabaseType.Oracle:
					return GetOracleDateLiteral(dateTime);

				case EnterpriseDatabaseType.SQLServer:
					return GetSqlServerDateLiteral(dateTime);

				case EnterpriseDatabaseType.PostgreSQL:
					return GetPostgreSQLDateLiteral(dateTime);

				case EnterpriseDatabaseType.SQLite:
					return GetSqliteDateLiteral(dateTime);
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
		public static string GetSqliteDateLiteral(DateTime dateTime)
		{
			return $"DATETIME('{dateTime:yyyy-MM-dd HH:mm:ss}')";
		}

		[NotNull]
		public static string GetOracleDateLiteral(DateTime dateTime)
		{
			return $"TO_DATE('{dateTime:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')";
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
