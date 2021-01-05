using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public class ExceptionObjectIssueCodePredicate : ExceptionObjectPredicate
	{
		protected override bool MatchesCore(ExceptionObject exceptionObject,
		                                    ExceptionObject searchExceptionObject)
		{
			return Matches(exceptionObject.IssueCode,
			               searchExceptionObject.IssueCode);
		}

		protected override bool MatchesCore(ExceptionObject exceptionObject,
		                                    QaError qaError)
		{
			string searchIssueCodeId = qaError.IssueCode?.ID.Trim();

			return Matches(exceptionObject.IssueCode, searchIssueCodeId);
		}

		private static bool Matches([CanBeNull] string exceptionObjectIssueCode,
		                            [CanBeNull] string searchIssueCode)
		{
			if (string.IsNullOrEmpty(exceptionObjectIssueCode))
			{
				return true;
			}

			exceptionObjectIssueCode = exceptionObjectIssueCode.Trim();

			if (string.IsNullOrEmpty(exceptionObjectIssueCode))
			{
				return true;
			}

			if (searchIssueCode == null)
			{
				return false;
			}

			const StringComparison comparison = StringComparison.OrdinalIgnoreCase;
			if (exceptionObjectIssueCode.Equals(searchIssueCode, comparison))
			{
				return true;
			}

			const char hierarchySeparator = '.';

			return searchIssueCode.Length > exceptionObjectIssueCode.Length &&
			       searchIssueCode.StartsWith(exceptionObjectIssueCode, comparison) &&
			       searchIssueCode[exceptionObjectIssueCode.Length] == hierarchySeparator;
		}
	}
}
