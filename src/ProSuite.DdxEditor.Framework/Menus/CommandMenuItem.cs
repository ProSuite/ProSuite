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
	/// MenuItem to act as the view for an Admin client <see cref="ICommand"/> instance.
	/// </summary>
	public class CommandMenuItem : ToolStripMenuItem
	{
		private readonly ICommand _command;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Initializes a new instance of the <see cref="CommandMenuItem"/> class.
		/// </summary>
		/// <param name="command">The command.</param>
		public CommandMenuItem([NotNull] ICommand command)
			: base(command.Text, command.Image)
		{
			Assert.ArgumentNotNull(command, nameof(command));

			_command = command;

			Enabled = command.Enabled;
			ToolTipText = command.ToolTip;

			TextImageRelation = TextImageRelation.ImageBeforeText;
			ImageScaling = ToolStripItemImageScaling.None;

			AutoSize = true;
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
	}
}
