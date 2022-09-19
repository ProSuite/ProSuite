using System.Drawing;

namespace ProSuite.DdxEditor.Framework.Commands
{
	public interface ICommand
	{
		string Text { get; }

		string ShortText { get; }

		Image Image { get; }

		bool Enabled { get; }

		string ToolTip { get; }

		void Execute();
	}
}
