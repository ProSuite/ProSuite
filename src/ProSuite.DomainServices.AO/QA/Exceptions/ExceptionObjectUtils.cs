using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Text;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public static class ExceptionObjectUtils
	{
		private const string _tableDelimiter = ";";
		private const string _oidFormat = ":{0}";
		private const char _originSeparator = '#';
		private const char _originLeftDelimiter = '[';
		private const char _originRightDelimiter = ']';

		[NotNull]
		public static string GetKey(
			[NotNull] IEnumerable<InvolvedTable> involvedTables,
			[CanBeNull] Predicate<string> excludeTableFromKey = null)
		{
			var sb = new StringBuilder();

			foreach (InvolvedTable involvedTable in involvedTables.OrderBy(t => t.TableName))
			{
				string tableName = involvedTable.TableName.Trim();

				if (excludeTableFromKey != null && excludeTableFromKey(tableName))
				{
					continue;
				}

				sb.Append(tableName);

				foreach (int oid in involvedTable.RowReferences
				                                 .OrderBy(r => r.OID)
				                                 .Select(r => r.OID))
				{
					sb.AppendFormat(_oidFormat, oid);
				}

				sb.Append(_tableDelimiter);
			}

			return sb.ToString();
		}

		[NotNull]
		public static string GetKey(
			[NotNull] IEnumerable<InvolvedRow> involvedRows,
			[CanBeNull] Predicate<string> excludeTableFromKey = null)
		{
			var sb = new StringBuilder();

			IDictionary<string, List<InvolvedRow>> rowsByTableName =
				InvolvedRowUtils.GroupByTableName(involvedRows);

			foreach (
				KeyValuePair<string, List<InvolvedRow>> pair in
				rowsByTableName.OrderBy(p => p.Key))
			{
				string tableName = pair.Key.Trim();

				if (excludeTableFromKey != null && excludeTableFromKey(tableName))
				{
					continue;
				}

				sb.Append(tableName);
				foreach (int oid in pair.Value.Where(r => r.OID >= 0)
				                        .OrderBy(r => r.OID)
				                        .Select(r => r.OID))
				{
					sb.AppendFormat(_oidFormat, oid);
				}

				sb.Append(_tableDelimiter);
			}

			return sb.ToString();
		}

		[NotNull]
		public static string GetShapeTypeText(esriGeometryType? shapeType)
		{
			if (shapeType == null)
			{
				return "<None>";
			}

			switch (shapeType.Value)
			{
				case esriGeometryType.esriGeometryPoint:
					return "Point";

				case esriGeometryType.esriGeometryMultipoint:
					return "Multipoint";

				case esriGeometryType.esriGeometryPolyline:
					return "Polyline";

				case esriGeometryType.esriGeometryPolygon:
					return "Polygon";

				case esriGeometryType.esriGeometryMultiPatch:
					return "MultiPatch";

				default:
					throw new ArgumentOutOfRangeException(
						$"Unexpected shape type: {shapeType.Value}");
			}
		}

		public static void AssertValidOriginValue([NotNull] string originValue)
		{
			Assert.ArgumentNotNullOrEmpty(originValue, nameof(originValue));

			var specialCharacters = new[]
			                        {
				                        _originLeftDelimiter,
				                        _originSeparator,
				                        _originRightDelimiter
			                        };

			if (originValue.IndexOfAny(specialCharacters) < 0)
			{
				return;
			}

			throw new AssertionException(
				string.Format(
					"Origin value must not contain any of the following characters: {0}",
					StringUtils.Concatenate(specialCharacters, ", ")));
		}

		public static IEnumerable<string> ParseOrigins(
			[CanBeNull] string managedExceptionOrigin)
		{
			if (string.IsNullOrEmpty(managedExceptionOrigin))
			{
				yield break;
			}

			foreach (string token in managedExceptionOrigin.Split(
				new[] {_originSeparator},
				StringSplitOptions.RemoveEmptyEntries))
			{
				yield return ParseOrigin(token);
			}
		}

		[NotNull]
		public static string FormatOrigins([NotNull] IEnumerable<string> origins)
		{
			return StringUtils.ConcatenateSorted(origins.Select(FormatOrigin),
			                                     _originSeparator.ToString());
		}

		[NotNull]
		public static string FormatGuid(Guid guid)
		{
			return guid.ToString().ToUpper();
		}

		[CanBeNull]
		public static string GetNormalizedStatus([CanBeNull] string rawStatusValue)
		{
			if (rawStatusValue == null)
			{
				return null;
			}

			var status = ParseStatus(rawStatusValue, ExceptionObjectStatus.Inactive);

			switch (status)
			{
				case ExceptionObjectStatus.Active:
					return "Active";

				case ExceptionObjectStatus.Inactive:
					return "Inactive";

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static ExceptionObjectStatus ParseStatus([CanBeNull] string value,
		                                                ExceptionObjectStatus defaultStatus)
		{
			string trimmedValue = value?.Trim();

			if (string.IsNullOrEmpty(trimmedValue))
			{
				return defaultStatus;
			}

			if (string.Equals(trimmedValue, "default", StringComparison.OrdinalIgnoreCase) ||
			    string.Equals(trimmedValue, "d", StringComparison.OrdinalIgnoreCase))
			{
				return defaultStatus;
			}

			if (string.Equals(trimmedValue, "inactive", StringComparison.OrdinalIgnoreCase) ||
			    string.Equals(trimmedValue, "i", StringComparison.OrdinalIgnoreCase))
			{
				return ExceptionObjectStatus.Inactive;
			}

			if (string.Equals(trimmedValue, "active", StringComparison.OrdinalIgnoreCase) ||
			    string.Equals(trimmedValue, "a", StringComparison.OrdinalIgnoreCase))
			{
				return ExceptionObjectStatus.Active;
			}

			throw new InvalidConfigurationException(
				$"Unsupported exception status value: {trimmedValue}");
		}

		[NotNull]
		private static string ParseOrigin([NotNull] string token)
		{
			return token.Trim()
			            .TrimStart(_originLeftDelimiter)
			            .TrimEnd(_originRightDelimiter);
		}

		[NotNull]
		private static string FormatOrigin([NotNull] string o)
		{
			return $"{_originLeftDelimiter}{o.Trim()}{_originRightDelimiter}";
		}
	}
}
