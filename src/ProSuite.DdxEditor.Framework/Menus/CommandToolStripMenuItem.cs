using System;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Framework.Menus
{
	/// <summary>
	/// ToolStripMenuItem to act as the view for an Admin client <see cref="ICommand"/> instance.
	/// </summary>
	public class CommandToolStripMenuItem : ToolStripMenuItem
	{
		private readonly ICommand _command;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Initializes a new instance of the <see cref="CommandToolStripButton"/> class.
		/// </summary>
		/// <param name="command">The command.</param>
		public CommandToolStripMenuItem([NotNull] ICommand command)
			: base(command.ShortText, command.Image)
		{
			Assert.ArgumentNotNull(command, nameof(command));

			_command = command;

			TextImageRelation = TextImageRelation.ImageBeforeText;
			ImageScaling = ToolStripItemImageScaling.None;

			AutoSize = true;

			UpdateAppearance();
		}

		protected override void OnClick(EventArgs e)
		{
			try
			{
				_command.Execute();
			}
			catch (Exception ex)
			{
				ErrorHandler.HandleError(ex, _msg);
			}
		}

		public void UpdateAppearance()
		{
			Enabled = _command.Enabled;
			ToolTipText = _command.ToolTip;
		}
	}
}
