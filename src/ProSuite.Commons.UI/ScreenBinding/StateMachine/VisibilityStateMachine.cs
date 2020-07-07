using System.Collections.Generic;
using System.Windows.Forms;

namespace ProSuite.Commons.UI.ScreenBinding.StateMachine
{
	public class VisibilityStateMachine<ENUM>
	{
		private readonly ControlSet _allControls = new ControlSet();

		private readonly Dictionary<ENUM, VisibleState> _states =
			new Dictionary<ENUM, VisibleState>();

		public VisibilityStateExpression OnStateChangeTo(ENUM key)
		{
			var expression =
				new VisibilityStateExpression(_allControls);
			_states.Add(key, expression.State);
			return expression;
		}

		public void ChangeStateTo(ENUM key, IScreenBinder binder)
		{
			_states[key].Process(binder);
		}

		#region Nested type: VisibilityStateExpression

		public class VisibilityStateExpression
		{
			private readonly VisibleState _state;

			internal VisibilityStateExpression(ControlSet allControls)
			{
				_state = new VisibleState(allControls);
			}

			internal VisibleState State => _state;

			public VisibilityStateExpression Show(params Control[] controls)
			{
				_state.Show(controls);
				return this;
			}
		}

		#endregion
	}
}
