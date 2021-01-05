using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainServices.AO.QA
{
	public class StopInfo
	{
		public StopInfo([NotNull] QualityCondition qualityCondition,
		                [NotNull] string errorDescription)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

			QualityCondition = qualityCondition;
			ErrorDescription = errorDescription;
		}

		[NotNull]
		public QualityCondition QualityCondition { get; private set; }

		public bool Reported { get; set; }

		[NotNull]
		public string ErrorDescription { get; private set; }
	}
}
