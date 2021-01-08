using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.QA.SpecificationReport
{
	public class HtmlDatasetReference
	{
		[NotNull] private readonly HtmlQualitySpecificationElement _element;
		[NotNull] private readonly HtmlTestParameterValue _parameterValue;

		internal HtmlDatasetReference([NotNull] HtmlQualitySpecificationElement element,
		                              [NotNull] HtmlTestParameterValue parameterValue)
		{
			Assert.ArgumentNotNull(element, nameof(element));
			Assert.ArgumentNotNull(parameterValue, nameof(parameterValue));

			_element = element;
			_parameterValue = parameterValue;
		}

		[NotNull]
		[UsedImplicitly]
		public HtmlQualitySpecificationElement Element
		{
			get { return _element; }
		}

		[NotNull]
		[UsedImplicitly]
		public HtmlTestParameterValue ParameterValue
		{
			get { return _parameterValue; }
		}
	}
}
