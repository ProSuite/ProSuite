using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.Properties;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainModel.AO.QA.SpecificationReport
{
	public class HtmlQualitySpecificationElement
	{
		[NotNull] private readonly QualitySpecificationElement _element;
		[NotNull] private readonly HtmlQualityCondition _htmlQualityCondition;

		internal HtmlQualitySpecificationElement(
			[NotNull] HtmlQualityCondition htmlQualityCondition,
			[NotNull] QualitySpecificationElement element)
		{
			Assert.ArgumentNotNull(htmlQualityCondition, nameof(htmlQualityCondition));
			Assert.ArgumentNotNull(element, nameof(element));

			_htmlQualityCondition = htmlQualityCondition;
			_element = element;

			IssueType = _element.AllowErrors
				            ? LocalizableStrings.IssueType_Warning
				            : LocalizableStrings.IssueType_Error;
		}

		[NotNull]
		[UsedImplicitly]
		public HtmlQualityCondition QualityCondition
		{
			get { return _htmlQualityCondition; }
		}

		[UsedImplicitly]
		public bool IsAllowable
		{
			get { return _element.AllowErrors; }
		}

		[UsedImplicitly]
		public bool IsStopCondition
		{
			get { return _element.StopOnError; }
		}

		[UsedImplicitly]
		[NotNull]
		public string IssueType { get; private set; }
	}
}
