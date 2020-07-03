using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.NamedValuesExpressions
{
	public class NamedValuesParser
	{
		private readonly char _separator;
		private readonly string[] _lineSeparators;
		private readonly string[] _valueSeparators;
		private readonly string _conjunctionSeparator;

		public NamedValuesParser(char separator,
		                         [NotNull] IEnumerable<string> lineSeparators,
		                         [NotNull] IEnumerable<string> valueSeparators,
		                         [NotNull] string conjunctionSeparator)
		{
			Assert.ArgumentNotNull(lineSeparators, nameof(lineSeparators));
			Assert.ArgumentNotNull(valueSeparators, nameof(valueSeparators));
			Assert.ArgumentNotNullOrEmpty(conjunctionSeparator, nameof(conjunctionSeparator));

			_separator = separator;
			_conjunctionSeparator = conjunctionSeparator;
			_lineSeparators = new List<string>(lineSeparators).ToArray();
			_valueSeparators = new List<string>(valueSeparators).ToArray();
		}

		public bool TryParse(
			[CanBeNull] string criterionString,
			[NotNull] out IList<NamedValuesExpression> nameValuesExpressions,
			[NotNull] out NotificationCollection notifications)
		{
			nameValuesExpressions = new List<NamedValuesExpression>();
			notifications = new NotificationCollection();

			if (criterionString == null)
			{
				return true;
			}

			string[] lines = criterionString.Split(_lineSeparators,
			                                       StringSplitOptions.RemoveEmptyEntries);
			var anyFailure = false;
			foreach (string line in lines)
			{
				if (StringUtils.IsNullOrEmptyOrBlank(line))
				{
					continue;
				}

				NamedValuesExpression expression;
				if (TryCreateExpression(line, notifications, out expression))
				{
					nameValuesExpressions.Add(expression);
				}
				else
				{
					anyFailure = true;
				}
			}

			return ! anyFailure;
		}

		private bool TryCreateExpression(
			[NotNull] string line,
			[NotNull] NotificationCollection notifications,
			[CanBeNull] out NamedValuesExpression expression)
		{
			if (line.IndexOf(_conjunctionSeparator, StringComparison.Ordinal) < 0)
			{
				NamedValues namedValues;
				if (TryCreateNamedValues(line, notifications, out namedValues))
				{
					expression = new SimpleNamedValuesExpression(namedValues);
					return true;
				}

				expression = null;
				return false;
			}

			string[] parts = line.Split(new[] {_conjunctionSeparator}, StringSplitOptions.None);

			if (parts.Length < 2)
			{
				NotificationUtils.Add(notifications,
				                      "Invalid position of '{0}' on line '{1}'",
				                      _conjunctionSeparator, line);
				expression = null;
				return false;
			}

			var conjunction = new NamedValuesConjunctionExpression();

			var anyFailure = false;
			foreach (string part in parts)
			{
				NamedValues namedValues;
				if (TryCreateNamedValues(part, notifications, out namedValues))
				{
					conjunction.Add(namedValues);
				}
				else
				{
					anyFailure = true;
				}
			}

			if (anyFailure)
			{
				expression = null;
				return false;
			}

			expression = conjunction;
			return true;
		}

		private bool TryCreateNamedValues([NotNull] string line,
		                                  [NotNull] NotificationCollection notifications,
		                                  out NamedValues namedValues)
		{
			int separatorIndex = line.IndexOf(_separator);

			if (separatorIndex < 0)
			{
				NotificationUtils.Add(notifications,
				                      "'{0}' not found on line '{1}'", _separator, line);
				namedValues = null;
				return false;
			}

			if (separatorIndex == 0)
			{
				NotificationUtils.Add(notifications,
				                      "Invalid position of '{0}' on line '{1}'", _separator, line);
				namedValues = null;
				return false;
			}

			string name = line.Substring(0, separatorIndex);

			if (StringUtils.IsNullOrEmptyOrBlank(name))
			{
				NotificationUtils.Add(notifications,
				                      "No valid criterion name found on line '{0}'", line);
				namedValues = null;
				return false;
			}

			name = name.Trim();

			if (separatorIndex >= line.Length - 1)
			{
				namedValues = new NamedValues(name);
				return true;
			}

			namedValues = new NamedValues(name, GetValues(line, separatorIndex));
			return true;
		}

		[NotNull]
		private IEnumerable<string> GetValues([NotNull] string line,
		                                      int separatorIndex)
		{
			string valuesString = line.Substring(separatorIndex + 1);

			string[] rawValues = valuesString.Split(_valueSeparators,
			                                        StringSplitOptions.RemoveEmptyEntries);

			foreach (string rawValue in rawValues)
			{
				string trimmedValue = rawValue.Trim();
				if (trimmedValue.Length == 0)
				{
					continue;
				}

				yield return trimmedValue;
			}
		}
	}
}