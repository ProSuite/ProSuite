using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Framework.Help
{
	public class PdfFileHelpProvider : FilebasedHelpProviderBase
	{
		public PdfFileHelpProvider([NotNull] string name, [CanBeNull] string filePath)
			: base(name, ".pdf", filePath) { }
	}
}
