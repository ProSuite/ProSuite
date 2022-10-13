using System.Drawing;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Logging;

namespace ProSuite.DdxEditor.Framework.Commands
{
	public abstract class CommandBase : ICommand
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Template method for indicating if the command is enabled or not.
		/// </summary>
		/// <value><c>true</c> if the command is enabled; otherwise, <c>false</c>.</value>
		/// <remarks>Is not called within a domain transaction. If the subclass requires a domain 
		/// transaction eg. to access the entity it must create one itself.</remarks>
		protected virtual bool EnabledCore => true;

		#region ICommand Members

		public virtual Image Image => null;

		public abstract string Text { get; }

		public virtual string ShortText => Text;

		public virtual string ToolTip => Text;

		public bool Enabled => EnabledCore;

		public void Execute()
		{
			Assert.True(Enabled, "Command is disabled, cannot execute");

			_msg.DebugFormat("Executing command '{0}'", Text);

			ExecuteCore();
		}

		#endregion

		protected abstract void ExecuteCore();
	}
}
