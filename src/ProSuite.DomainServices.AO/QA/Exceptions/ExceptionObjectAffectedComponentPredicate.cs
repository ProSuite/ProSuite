using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public class ExceptionObjectAffectedComponentPredicate : ExceptionObjectPredicate
	{
		private readonly char[] _separators = {' ', ';', ','};

		protected override bool MatchesCore(ExceptionObject exceptionObject,
		                                    ExceptionObject searchExceptionObject)
		{
			return Matches(exceptionObject.AffectedComponent,
			               searchExceptionObject.AffectedComponent);
		}

		protected override bool MatchesCore(ExceptionObject exceptionObject,
		                                    QaError qaError)
		{
			return Matches(exceptionObject.AffectedComponent, qaError.AffectedComponent);
		}

		private bool Matches([CanBeNull] string exceptionValue,
		                     [CanBeNull] string searchValue)
		{
			if (exceptionValue == null)
			{
				return true;
			}

			exceptionValue = exceptionValue.Trim();

			if (string.IsNullOrEmpty(exceptionValue))
			{
				return true;
			}

			searchValue = string.IsNullOrEmpty(searchValue)
				              ? null
				              : searchValue.Trim();

			if (exceptionValue.IndexOfAny(_separators) < 0)
			{
				// exception uses single value
				return string.Equals(exceptionValue, searchValue,
				                     StringComparison.OrdinalIgnoreCase);
			}

			// exception has multiple values
			if (string.IsNullOrEmpty(searchValue))
			{
				return false;
			}

			var exceptionSet = new HashSet<string>(
				exceptionValue.Split(_separators,
				                     StringSplitOptions.RemoveEmptyEntries),
				StringComparer.OrdinalIgnoreCase);

			var qaErrorSet = new HashSet<string>(
				searchValue.Split(_separators,
				                  StringSplitOptions.RemoveEmptyEntries),
				StringComparer.OrdinalIgnoreCase);

			return exceptionSet.SetEquals(qaErrorSet);
		}
	}
}
