using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.Env
{
	public abstract class UIEnvironmentDecorator : IUIEnvironment
	{
		private IUIEnvironment _target;

		[NotNull]
		protected IUIEnvironment Target
		{
			get
			{
				Assert.NotNull(_target, "target is null");
				return _target;
			}
		}

		protected internal void SetTarget([NotNull] IUIEnvironment target)
		{
			Assert.ArgumentNotNull(target, nameof(target));

			_target = target;
		}

		#region Implementation of IUIEnvironment

		public virtual DialogResult ShowDialog(IModalDialog form, IWin32Window owner,
		                                       Action<DialogResult> procedure)
		{
			return Target.ShowDialog(form, owner, procedure);
		}

		public Task<DialogResult> ShowDialogAsync(IModalDialog modalDialog, IWin32Window owner,
		                                          Action<DialogResult> procedure)
		{
			return Target.ShowDialogAsync(modalDialog, owner, procedure);
		}

		public virtual CursorState ReleaseCursor()
		{
			return Target.ReleaseCursor();
		}

		public virtual Task<CursorState> ReleaseCursorAsync()
		{
			return Target.ReleaseCursorAsync();
		}

		public virtual void WithReleasedCursor(Action procedure)
		{
			Target.WithReleasedCursor(procedure);
		}

		public virtual Task WithReleasedCursorAsync(Func<Task> func)
		{
			return Target.WithReleasedCursorAsync(func);
		}

		public virtual void RestoreCursor(CursorState cursorState)
		{
			Target.RestoreCursor(cursorState);
		}

		public virtual Task RestoreCursorAsync(CursorState cursorState)
		{
			return Target.RestoreCursorAsync(cursorState);
		}

		#endregion
	}
}
