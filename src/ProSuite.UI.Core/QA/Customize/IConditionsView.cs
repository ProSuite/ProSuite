using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.UI.Core.QA.Customize
{
	internal interface IConditionsView
	{
		[NotNull]
		Control Control { get; }

		bool MatchCase { get; set; }

		bool FilterRows { get; set; }

		[CanBeNull]
		ICustomizeQASpezificationView CustomizeView { get; set; }

		void SetSpecification([NotNull] QualitySpecification qualitySpecification);

		void RefreshAll();

		[NotNull]
		ICollection<QualitySpecificationElement> GetSelectedElements();

		[NotNull]
		ICollection<QualitySpecificationElement> GetFilteredElements();

		void SetSelectedElements(
			[NotNull] ICollection<QualitySpecificationElement> elements,
			bool forceVisible);

		void PushTreeState();
	}
}
