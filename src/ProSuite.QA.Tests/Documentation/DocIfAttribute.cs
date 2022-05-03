using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Globalization;

namespace ProSuite.QA.Tests.Documentation
{
	/// <summary>
	/// Attribute for documenting issue filters. Supports localization by
	/// looking up string resources in <see cref="DocIfStrings"></see>
	/// </summary>
	public class DocIfAttribute : LocalizedDescriptionAttribute
	{
		public DocIfAttribute([NotNull] string resourceName)
			: base(DocIfStrings.ResourceManager, resourceName) { }
	}
}
