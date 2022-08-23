using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.QA.TestReport
{
	public interface IReportBuilder
	{
		void WriteReport();

		void IncludeTest([NotNull] Type testType, int constructorIndex);

		void IncludeTestFactory([NotNull] Type testFactoryType);

		bool IncludeAssemblyInfo { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether obsolete instance descriptors are
		/// included.
		/// </summary>
		/// <value><c>true</c> if obsolete tests, factories, transformers or filters should be
		/// included; otherwise, <c>false</c>.</value>
		bool IncludeObsolete { get; set; }

		void AddHeaderItem([NotNull] string name, [CanBeNull] string value);

		void IncludeTransformer([NotNull] Type transformerType, int ctorIndex);

		void IncludeIssueFilter(Type issueFilterType, int ctorIndex);
	}
}
