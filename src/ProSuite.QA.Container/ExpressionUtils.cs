using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.DomainModels;
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
			'`',
			'+',
			'-',
			'*',
			'/',
			'%'
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
			[NotNull] IReadOnlyTable table,
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
			[NotNull] IReadOnlyTable table,
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
			[NotNull] IReadOnlyTable table,
			[NotNull] string alias)
		{
			var tableFieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (IField field in DatasetUtils.GetFields(table.Fields))
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

						if (IsFieldReferenceBasedOn(match.Value, searchedDatasetName,
						                            out string fieldName))
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

			if (! ModelElementNameUtils.IsValidUnqualifiedFieldName(remainder, out _))
			{
				fieldName = null;
				return false;
			}

			fieldName = remainder;
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
		public static Dictionary<string, string> GetFieldDict(
			[CanBeNull] IList<string> expressions)
		{
			Dictionary<string, string> expressionDict =
				new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

			if (expressions == null)
			{
				return expressionDict;
			}

			foreach (string expression in expressions)
			{
				string expr = GetExpression(expression, out string fieldName);

				expressionDict.Add(fieldName, expr);
			}

			return expressionDict;
		}

		[NotNull]
		public static string GetExpression([NotNull] string expression,
		                                   out string fieldName)
		{
			string trimmedExpr = expression.Trim();
			string expr;
			IList<string> parts = trimmedExpr.Split();
			if (parts.Count >= 3 &&
			    parts[parts.Count - 2]
				    .Equals("AS", StringComparison.InvariantCultureIgnoreCase))
			{
				fieldName = parts[parts.Count - 1];
				trimmedExpr = trimmedExpr.Substring(0, trimmedExpr.Length - fieldName.Length)
				                         .Trim();
				expr = trimmedExpr.Substring(0, trimmedExpr.Length - 2).Trim();
			}
			else
			{
				fieldName = trimmedExpr;
				expr = trimmedExpr;
			}

			return expr;
		}

		[NotNull]
		public static Dictionary<string, string> CreateAliases(
			[CanBeNull] Dictionary<string, string> expressionDict)
		{
			Dictionary<string, string> aliasFieldDict =
				new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
			if (expressionDict == null)
			{
				return aliasFieldDict;
			}

			Dictionary<string, string> fieldAliasDict = new Dictionary<string, string>();

			Dictionary<string, string> changedDict = new Dictionary<string, string>();

			foreach (var pair in expressionDict)
			{
				string expression = pair.Value;
				if (! expression.Contains("."))
				{
					continue;
				}

				string aliasedExpression = expression;
				bool completed = false;
				while (! completed)
				{
					completed = true;
					foreach (string expressionToken in
					         GetExpressionTokens(aliasedExpression, toUpper: true))
					{
						if (! expressionToken.Contains("."))
						{
							continue;
						}

						if (! fieldAliasDict.TryGetValue(expressionToken, out string alias))
						{
							alias = GetAlias(expressionToken, expressionDict.Values,
							                 changedDict.Values);
							aliasFieldDict.Add(alias, expressionToken);
							fieldAliasDict.Add(expressionToken, alias);
						}

						aliasedExpression = Replace(aliasedExpression, expressionToken, alias);

						completed = false;
						break;
					}
				}

				changedDict[pair.Key] = aliasedExpression;
			}

			foreach (var pair in changedDict)
			{
				expressionDict[pair.Key] = pair.Value;
			}

			return aliasFieldDict;
		}

		private static string GetAlias([NotNull] string token,
		                               [NotNull] ICollection<string> expressions,
		                               [NotNull] ICollection<string> aliasedExpressions)
		{
			string candidate = $"_{token.Substring(token.LastIndexOf('.') + 1)}";
			bool success = false;
			while (! success)
			{
				const StringComparison ic = StringComparison.InvariantCultureIgnoreCase;
				success =
					expressions.FirstOrDefault(x => x.IndexOf(candidate, ic) >= 0) == null &&
					aliasedExpressions.FirstOrDefault(x => x.IndexOf(candidate, ic) >= 0) == null;

				if (! success)
				{
					candidate += "_";
				}
			}

			return candidate;
		}

		[NotNull]
		private static string Replace([NotNull] string expression, [NotNull] string search,
		                              [NotNull] string replace)
		{
			if (string.IsNullOrEmpty(search))
			{
				return expression;
			}

			int iStart = 0;
			string replaced = expression;
			while (true)
			{
				int iFound = replaced.Substring(iStart)
				                     .IndexOf(search, StringComparison.InvariantCultureIgnoreCase);

				if (iFound < 0)
				{
					break;
				}

				// verify that it is no subpart of another expression
				string extended = replaced.Substring(Math.Max(0, iFound - 1));
				extended = extended.Substring(0, Math.Min(extended.Length, search.Length + 2));

				foreach (string expressionToken in GetExpressionTokens(extended, toUpper: true))
				{
					if (expressionToken == search)
					{
						replaced =
							$"{replaced.Substring(0, iFound)}{replace}{replaced.Substring(iFound + search.Length)}";
						iStart = 0;
					}
					else
					{
						iStart = iFound + 1;
					}

					break;
				}
			}

			return replaced;
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
