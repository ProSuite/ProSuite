using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Globalization;

namespace ProSuite.QA.Tests.Documentation
{
	/// <summary>
	/// Attribute for documenting transformer classes, constructor parameters and optional parameters. Supports localization by
	/// looking up string resources in <see cref="DocTrStrings"></see>
	/// </summary>
	public class DocTrAttribute : LocalizedDescriptionAttribute
	{
		public DocTrAttribute([NotNull] string resourceName)
			: base(DocTrStrings.ResourceManager, resourceName) { }
	}
}
