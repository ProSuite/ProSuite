using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Framework.Help
{
	public interface IHelpProvider
	{
		[NotNull]
		[PublicAPI]
		string Name { get; }

		/// <summary>
		/// Update the value for <see cref="CanShowHelp"/>. This is useful if the source for the help can be defined in the ddx editor itself
		/// </summary>
		[PublicAPI]
		void Refresh();

		[PublicAPI]
		bool CanShowHelp { get; }

		[PublicAPI]
		void ShowHelp([NotNull] IWin32Window owner);
	}
}
