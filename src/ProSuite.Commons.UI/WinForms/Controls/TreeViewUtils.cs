using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	public static class TreeViewUtils
	{
		/// <summary>
		/// Gets the text for a tree node, with padding such that the
		/// node font can be changed the bold and the text is still not cut off.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <returns></returns>
		[NotNull]
		public static string GetPaddedNodeText([NotNull] string text)
		{
			Assert.ArgumentNotNull(text, nameof(text));

			int paddingCount = text.Length / 3 + 1;

			return text + new string(' ', paddingCount);
		}
	}
}
