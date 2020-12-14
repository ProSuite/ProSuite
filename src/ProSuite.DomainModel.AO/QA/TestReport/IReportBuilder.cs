using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace EsriDE.ProSuite.DomainModel.QA.TestReport
{
	public interface IReportBuilder
	{
		void WriteReport();

		void IncludeTest([NotNull] Type testType, int constructorIndex);

		void IncludeTestFactory([NotNull] Type testFactoryType);

		bool IncludeAssemblyInfo { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether obsolete tests or factories are
		/// included.
		/// </summary>
		/// <value><c>true</c> if obsolete tests or factories should be included; otherwise, <c>false</c>.</value>
		bool IncludeObsolete { get; set; }

		void AddHeaderItem([NotNull] string name, [CanBeNull] string value);
	}
}