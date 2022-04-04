using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Framework.Help
{
	public class HtmlFileHelpProvider : FilebasedHelpProviderBase
	{
		public HtmlFileHelpProvider([NotNull] string name, [CanBeNull] string filePath)
			: base(name, ".html", filePath) { }
	}
}
