using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.QA.Container
{
	public static class ExpressionUtils
	{
		private const string _caseSensitiveHint = "##CASESENSITIVE";
		private const string _ignoreCaseHint = "##IGNORECASE";

		// TODO what about +, - , *, / etc.?
		private static readonly char[] _expressionTokenSeparators =
		{
			'=',
			',',
			'(',
			')',
			'>',
			'<',
			' ',
			'[',
			']',
			'`'
		};

		[CanBeNull] private static Regex _tableNameMatchRegex;

		[NotNull]
		public static string CaseSensitivityHint => _caseSensitiveHint;

		[NotNull]
		public static string IgnoreCaseHint => _ignoreCaseHint;

		[NotNull]
		public static string ParseCaseSensitivityHint([NotNull] string expression,
		                                              out bool? caseSensitive)
		{
			Assert.ArgumentNotNull(expression, nameof(expression));

			const StringComparison comparison = StringComparison.OrdinalIgnoreCase;

			string trimmed = expression.TrimEnd();

			if (trimmed.EndsWith(_caseSensitiveHint, comparison))
			{
				int index = expression.LastIndexOf(_caseSensitiveHint, comparison);
				caseSensitive = true;
				return expression.Substring(0, index);
			}

			if (trimmed.EndsWith(_ignoreCaseHint, comparison))
			{
				int index = expression.LastIndexOf(_ignoreCaseHint, comparison);
				caseSensitive = false;
				return expression.Substring(0, index);
			}

			caseSensitive = null;
			return expression;
		}

		[NotNull]
		public static IEnumerable<string> GetExpressionFieldNames(
			[NotNull] ITable table,
			[NotNull] string expression,
			bool toUpper = false)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(expression, nameof(expression));

			foreach (string token in GetExpressionTokens(expression, toUpper))
			{
				if (table.FindField(token) >= 0)
				{
					yield return token;
				}
			}
		}

		[NotNull]
		public static IEnumerable<KeyValuePair<IField, int>> GetExpressionFields(
			[NotNull] ITable table,
			[NotNull] string expression)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(expression, nameof(expression));

			IFields fields = table.Fields;
			foreach (string token in GetExpressionTokens(expression))
			{
				int fieldIndex = table.FindField(token);
				if (fieldIndex >= 0)
				{
					yield return
						new KeyValuePair<IField, int>(fields.Field[fieldIndex], fieldIndex);
				}
			}
		}

		[NotNull]
		public static IEnumerable<string> GetExpressionFieldNames(
			[NotNull] string expression,
			[NotNull] ITable table,
			[NotNull] string alias)
		{
			var tableFieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (IField field in DatasetUtils.GetFields(table))
			{
				tableFieldNames.Add(field.Name);
			}

			var result = new HashSet<string>();

			foreach (string token in GetExpressionTokens(expression))
			{
				string[] tokenParts = token.Split('.');

				if (tokenParts.Length != 2)
				{
					continue;
				}

				if (! string.Equals(tokenParts[0], alias, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				string fieldName = tokenParts[1];
				if (! tableFieldNames.Contains(fieldName))
				{
					continue;
				}

				result.Add(fieldName);
			}

			return result;
		}

		public static string ReplaceTableNames(
			[NotNull] string expression,
			[NotNull] IDictionary<string, string> replacements)
		{
			return TableNameMatchRegex.Replace(
				expression,
				match =>
				{
					foreach (KeyValuePair<string, string> pair in replacements)
					{
						string searchedDatasetName = pair.Key;
						string replacedDatasetName = pair.Value;

						string fieldName;
						if (IsFieldReferenceBasedOn(match.Value, searchedDatasetName, out fieldName)
						)
						{
							return $"{replacedDatasetName}.{fieldName}";
						}
					}

					// no match found, return as is
					return match.Value;
				});
		}

		[ContractAnnotation("=>true, fieldName:notnull; =>false, fieldName:canbenull")]
		private static bool IsFieldReferenceBasedOn([NotNull] string token,
		                                            [NotNull] string datasetName,
		                                            [CanBeNull] out string fieldName)
		{
			string fieldPrefix = $"{datasetName}.";

			if (! token.StartsWith(fieldPrefix, StringComparison.OrdinalIgnoreCase))
			{
				fieldName = null;
				return false;
			}

			if (token.Trim().Length <= fieldPrefix.Length)
			{
				fieldName = null;
				return false;
			}

			string remainder = token.Substring(fieldPrefix.Length);

			if (! IsValidUnqualifiedFieldName(remainder))
			{
				fieldName = null;
				return false;
			}

			fieldName = remainder;
			return true;
		}

		private static bool IsValidUnqualifiedFieldName([NotNull] string value)
		{
			// See http://support.esri.com/en/technical-article/000005588: 
			// FAQ: What characters should not be used in ArcGIS for field names and table names?

			if (value.IndexOf('.') >= 0)
			{
				return false;
			}

			if (! char.IsLetter(value[0]))
			{
				return false;
			}

			for (var index = 0; index < value.Length; index++)
			{
				char c = value[index];

				if (index == 0 && ! char.IsLetter(c))
				{
					return false;
				}

				if (! char.IsLetterOrDigit(c) && c != '_')
				{
					return false;
				}
			}

			return true;
		}

		[NotNull]
		public static IEnumerable<string> GetExpressionTokens([NotNull] string expression,
		                                                      bool toUpper = false)
		{
			Assert.ArgumentNotNull(expression, nameof(expression));

			string[] tokens = expression.Split();

			foreach (string token in tokens)
			{
				foreach (string subToken in token.Split(_expressionTokenSeparators,
				                                        StringSplitOptions.RemoveEmptyEntries))
				{
					yield return toUpper
						             ? subToken.ToUpper()
						             : subToken;
				}
			}
		}

		[NotNull]
		private static Regex TableNameMatchRegex => _tableNameMatchRegex ??
		                                            (_tableNameMatchRegex =
			                                             CompileTableNameMatchRegex());

		[NotNull]
		private static Regex CompileTableNameMatchRegex()
		{
			string escapedSeparators = StringUtils.Concatenate(_expressionTokenSeparators,
			                                                   c => $@"\{c}", string.Empty);

			string pattern = $@"(?<=[{escapedSeparators}]|\b)[\w\.]+";

			return new Regex(pattern, RegexOptions.Compiled);
		}
	}
}
