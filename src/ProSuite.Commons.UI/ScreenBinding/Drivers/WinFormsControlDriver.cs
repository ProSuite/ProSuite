using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.ScreenBinding.Drivers
{
	public class WinFormsControlDriver : IControlDriver
	{
		[NotNull] private readonly Control _control;
		private readonly ToolTip _toolTip = new ToolTip();
		private readonly List<Action> _lostFocusHandlers = new List<Action>();

		public static void RaiseEvent(object control, string eventName,
		                              params object[] args)
		{
			MethodInfo minfo =
				control.GetType().GetMethod("On" + eventName,
				                            BindingFlags.Instance | BindingFlags.Public |
				                            BindingFlags.NonPublic);

			if (minfo == null)
			{
				throw new InvalidOperationException($"No such method: On{eventName}");
			}

			ParameterInfo[] param = minfo.GetParameters();

			Type parameterType = param[0].ParameterType;

			minfo.Invoke(control, new[] {Activator.CreateInstance(parameterType)});
		}

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="WinFormsControlDriver"/> class.
		/// </summary>
		/// <param name="control">The control.</param>
		public WinFormsControlDriver([NotNull] Control control)
		{
			Assert.ArgumentNotNull(control, nameof(control));

			_control = control;
		}

		#endregion

		public bool Visible
		{
			get { return _control.Visible; }
			set { _control.Visible = value; }
		}

		public bool Enabled
		{
			get { return _control.Enabled; }
			set { _control.Enabled = value; }
		}

		public void Focus()
		{
			_control.Focus();
		}

		public Color BackColor
		{
			get { return _control.BackColor; }
			set { _control.BackColor = value; }
		}

		public string GetKey()
		{
			return _control.Parent != null
				       ? _control.Parent.GetType().FullName + "/" + _control.GetType().FullName +
				         "/" +
				         _control.Name
				       : _control.GetType().FullName + "/" + _control.Name;
		}

		public void AddLostFocusHandler(Action action)
		{
			_lostFocusHandlers.Add(action);

			_control.LostFocus -= _control_LostFocus;
			_control.LostFocus += _control_LostFocus;

			// _control.LostFocus += delegate { action(); };
		}

		public void RemoveLostFocusHandler(Action action)
		{
			_lostFocusHandlers.Remove(action);

			if (_lostFocusHandlers.Count == 0)
			{
				_control.LostFocus -= _control_LostFocus;
			}
		}

		public void Click()
		{
			RaiseEvent(_control, "Click");
		}

		public void RaiseLostFocus()
		{
			RaiseEvent(_control, "LostFocus");
		}

		public bool IsFocused => _control.Focused;

		public object Control => _control;

		public string Text => _control.Text;

		public string ToolTipText
		{
			get { return _toolTip.GetToolTip(_control); }
			set { _toolTip.SetToolTip(_control, value); }
		}

		public void OnHover(Action action)
		{
			throw new NotImplementedException();
		}

		public void Click(object control)
		{
			RaiseEvent(control, "Click");
		}

		private void _control_LostFocus(object sender, EventArgs e)
		{
			foreach (Action lostFocusHandler in _lostFocusHandlers)
			{
				lostFocusHandler();
			}
		}
	}
}
