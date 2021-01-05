using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainServices.AO.QA
{
	public class TestVerification
	{
		public TestVerification([NotNull] QualityConditionVerification verification,
		                        int testIndex)
		{
			Assert.ArgumentNotNull(verification, nameof(verification));
			Assert.ArgumentCondition(verification.QualityCondition != null,
			                         "quality condition is undefined");

			QualityConditionVerification = verification;
			QualityCondition = verification.QualityCondition;

			TestIndex = testIndex;
		}

		[NotNull]
		public QualityConditionVerification QualityConditionVerification { get; private set; }

		[NotNull]
		public QualityCondition QualityCondition { get; private set; }

		// TODO what would this have been used for?
		/// <summary>
		/// Gets or the index of the test for the verified quality condition.
		/// </summary>
		/// <value>
		/// The index of the test.
		/// </value>
		/// <remarks>The test factory used by a quality condition can produce more than one test. 
		/// The index refers to the respective test within this result.</remarks>
		public int TestIndex { get; private set; }
	}
}
