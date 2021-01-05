using System;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public class ExceptionObjectValuesPredicate : ExceptionObjectPredicate
	{
		private readonly double _significantDigits;
		private readonly bool _ignoreLeadingAndTrailingWhitespace;
		private readonly bool _ignoreCase;

		public ExceptionObjectValuesPredicate(double significantDigits = 1E-10,
		                                      bool ignoreLeadingAndTrailingWhitespace =
			                                      true,
		                                      bool ignoreCase = false)
		{
			_significantDigits = significantDigits;
			_ignoreLeadingAndTrailingWhitespace = ignoreLeadingAndTrailingWhitespace;
			_ignoreCase = ignoreCase;
		}

		protected override bool MatchesCore(ExceptionObject exceptionObject,
		                                    ExceptionObject searchExceptionObject)
		{
			const double toleranceFactor = 10;
			double tolerance = exceptionObject.XYTolerance * toleranceFactor ?? 0;

			return Matches(exceptionObject.DoubleValue1,
			               searchExceptionObject.DoubleValue1,
			               tolerance) &&
			       Matches(exceptionObject.DoubleValue2,
			               searchExceptionObject.DoubleValue2,
			               tolerance) &&
			       Matches(exceptionObject.TextValue, searchExceptionObject.TextValue);
		}

		protected override bool MatchesCore(ExceptionObject exceptionObject,
		                                    QaError qaError)
		{
			double? errorDoubleValue1;
			double? errorDoubleValue2;
			string errorTextValue;
			IssueUtils.GetValues(qaError.Values,
			                     out errorDoubleValue1,
			                     out errorDoubleValue2,
			                     out errorTextValue);

			const double toleranceFactor = 10;
			double tolerance = exceptionObject.XYTolerance * toleranceFactor ?? 0;

			return Matches(exceptionObject.DoubleValue1, errorDoubleValue1, tolerance) &&
			       Matches(exceptionObject.DoubleValue2, errorDoubleValue2, tolerance) &&
			       Matches(exceptionObject.TextValue, errorTextValue);
		}

		private bool Matches(double? exceptionValue, double? searchValue, double tolerance)
		{
			if (exceptionValue == null)
			{
				// exception does not specify a value --> match always
				return true;
			}

			if (searchValue == null)
			{
				// exception has value, but search object does not --> no match
				return false;
			}

			if (Math.Abs(exceptionValue.Value - searchValue.Value) <= tolerance)
			{
				return true;
			}

			// in case the specified tolerance is too small for the field values, or 0: compare based on significant digits

			// This uses a fixed number of significant digits (not decimal places)
			// If unwanted mismatches are observed: consider using fixed number decimal places instead
			// (not sure what is more appropriate for double fields in dbf tables...)
			return MathUtils.AreDigitsEqual(exceptionValue.Value, searchValue.Value,
			                                _significantDigits);
		}

		private bool Matches([CanBeNull] string exceptionValue,
		                     [CanBeNull] string errorValue)
		{
			string normalizedExceptionValue = GetNormalized(exceptionValue);

			if (normalizedExceptionValue == null)
			{
				// exception does not specify a value --> match always
				return true;
			}

			string normalizedIssueValue = GetNormalized(errorValue);

			if (normalizedIssueValue == null)
			{
				// exception has value, but issue does not --> no match
				return false;
			}

			return string.Equals(normalizedExceptionValue,
			                     normalizedIssueValue,
			                     _ignoreCase
				                     ? StringComparison.InvariantCultureIgnoreCase
				                     : StringComparison.InvariantCulture);
		}

		[CanBeNull]
		private string GetNormalized([CanBeNull] string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}

			return _ignoreLeadingAndTrailingWhitespace
				       ? text.Trim()
				       : text;
		}
	}
}
