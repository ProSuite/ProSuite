using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.IssueCodes;

namespace ProSuite.QA.Tests.IssueCodes
{
	/// <summary>
	/// Base class for issue code classes used in this assembly. 
	/// Used for simplifying the construction code within each test. 
	/// Uses a standard resource manager for descriptions, and always reads the
	/// issue codes from string constants in the subclass.
	/// </summary>
	internal abstract class LocalTestIssueCodes : TestIssueCodes
	{
		private const bool _includeLocalCodesFromStringConstants = true;

		/// <summary>
		/// Initializes a new instance of the <see cref="LocalTestIssueCodes"/> class.
		/// </summary>
		/// <param name="testId">The test id.</param>
		protected LocalTestIssueCodes([NotNull] string testId)
			: base(testId, IssueCodeDescriptions.ResourceManager,
			       _includeLocalCodesFromStringConstants) { }
	}
}
