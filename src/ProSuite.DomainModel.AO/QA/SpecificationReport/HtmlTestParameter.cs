using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.AO.QA.SpecificationReport
{
	public class HtmlTestParameter
	{
		internal HtmlTestParameter([NotNull] TestParameter testParameter)
		{
			Name = testParameter.Name;
			Description = StringUtils.IsNotEmpty(testParameter.Description)
				              ? testParameter.Description
				              : null;
			Type = TestImplementationUtils.GetParameterTypeString(testParameter);
		}

		[UsedImplicitly]
		public string Name { get; private set; }

		[UsedImplicitly]
		public string Type { get; private set; }

		[UsedImplicitly]
		public string Description { get; private set; }
	}
}
