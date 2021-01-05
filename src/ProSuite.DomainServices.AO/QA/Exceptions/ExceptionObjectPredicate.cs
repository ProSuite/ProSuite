using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public abstract class ExceptionObjectPredicate
	{
		public bool Matches([NotNull] ExceptionObject exceptionObject,
		                    [NotNull] QaError qaError)
		{
			Assert.ArgumentNotNull(exceptionObject, nameof(exceptionObject));
			Assert.ArgumentNotNull(qaError, nameof(qaError));

			// TODO record match statistics etc.
			return MatchesCore(exceptionObject, qaError);
		}

		public bool Matches([NotNull] ExceptionObject exceptionObject,
		                    [NotNull] ExceptionObject searchExceptionObject)
		{
			Assert.ArgumentNotNull(exceptionObject, nameof(exceptionObject));
			Assert.ArgumentNotNull(searchExceptionObject, nameof(searchExceptionObject));

			// TODO record match statistics etc.
			return MatchesCore(exceptionObject, searchExceptionObject);
		}

		protected abstract bool MatchesCore([NotNull] ExceptionObject exceptionObject,
		                                    [NotNull] QaError qaError);

		protected abstract bool MatchesCore([NotNull] ExceptionObject exceptionObject,
		                                    [NotNull] ExceptionObject searchExceptionObject);
	}
}
