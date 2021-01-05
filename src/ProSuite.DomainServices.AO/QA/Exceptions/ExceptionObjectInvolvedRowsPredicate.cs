using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public class ExceptionObjectInvolvedRowsPredicate : ExceptionObjectPredicate
	{
		private readonly Predicate<string> _excludeTableFromKey;

		[NotNull] private readonly IDictionary<ExceptionObject, string> _exceptionObjectKeys
			=
			new Dictionary<ExceptionObject, string>();

		public ExceptionObjectInvolvedRowsPredicate(
			[CanBeNull] Predicate<string> excludeTableFromKey)
		{
			_excludeTableFromKey = excludeTableFromKey;
		}

		protected override bool MatchesCore(ExceptionObject exceptionObject,
		                                    ExceptionObject searchExceptionObject)
		{
			return Matches(GetExceptionObjectKey(exceptionObject),
			               GetExceptionObjectKey(searchExceptionObject));
		}

		protected override bool MatchesCore(ExceptionObject exceptionObject,
		                                    QaError qaError)
		{
			string exceptionObjectKey = GetExceptionObjectKey(exceptionObject);

			string searchKey = ExceptionObjectUtils.GetKey(qaError.InvolvedRows,
			                                               _excludeTableFromKey);

			return Matches(exceptionObjectKey, searchKey);
		}

		private static bool Matches([NotNull] string exceptionObjectKey,
		                            [NotNull] string searchKey)
		{
			return string.Equals(exceptionObjectKey, searchKey,
			                     StringComparison.OrdinalIgnoreCase);
		}

		[NotNull]
		private string GetExceptionObjectKey([NotNull] ExceptionObject exceptionObject)
		{
			string key;
			if (! _exceptionObjectKeys.TryGetValue(exceptionObject, out key))
			{
				key = ExceptionObjectUtils.GetKey(exceptionObject.InvolvedTables,
				                                  _excludeTableFromKey);
				_exceptionObjectKeys.Add(exceptionObject, key);
			}

			return key;
		}
	}
}
