using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.UI.Core.QA.Customize
{
	public interface ICustomizeQASpezificationView
	{
		void RenderConditionsViewSelection(
			[NotNull] ICollection<QualitySpecificationElement> qualitySpecificationElements);

		void RenderViewContent();
	}
}
