using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.ScreenBinding.ScreenStates;

namespace ProSuite.Commons.UI.ScreenBinding.StateMachine
{
	public class ScreenStateMachine<ENUM>
	{
		private readonly ControlSet _all = new ControlSet();
		private readonly IScreenBinder _binder;

		private readonly Dictionary<ENUM, IScreenState> _states =
			new Dictionary<ENUM, IScreenState>();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ScreenStateMachine&lt;ENUM&gt;"/> class.
		/// </summary>
		/// <param name="binder">The binder.</param>
		public ScreenStateMachine(IScreenBinder binder)
		{
			Assert.ArgumentNotNull(binder, nameof(binder));

			_binder = binder;
		}

		#endregion

		public IScreenState CurrentState { get; private set; } = new NulloScreenState();

		public ScreenStateExpression OnStateChangeTo(ENUM stateName)
		{
			return new ScreenStateExpression(this, stateName);
		}

		public IScreenState GetState(ENUM stateName)
		{
			return _states[stateName];
		}

		public void ChangeStateTo(ENUM stateName)
		{
			IScreenState state = GetState(stateName);
			_binder.EnableControls(state);
			CurrentState = state;
		}

		public void MarkElementAsReadOnly(Control control)
		{
			IScreenElement element = Assert.NotNull(_binder.FindElementForControl(control),
			                                        $"element not found for {control}");
			element.ActivationMode = ActivationMode.ReadOnly;
			element.EnableControl(CurrentState);
		}

		public void MarkElementForNormalActivation(Control control)
		{
			IScreenElement element = Assert.NotNull(_binder.FindElementForControl(control),
			                                        $"element not found for {control}");
			element.ActivationMode = ActivationMode.Normal;
			element.EnableControl(CurrentState);
		}

		#region Nested type: ScreenStateExpression

		public class ScreenStateExpression
		{
			[NotNull] private readonly ScreenStateMachine<ENUM> _parent;
			private readonly ENUM _stateName;

			public ScreenStateExpression([NotNull] ScreenStateMachine<ENUM> parent,
			                             ENUM stateName)
			{
				_parent = parent;
				_stateName = stateName;
			}

			public void Enable(params object[] controls)
			{
				_parent._all.AddRange(controls);
				var state = new NormalScreenState(_parent._all);
				state.Enable(controls);
				_parent._states.Add(_stateName, state);
			}

			public void DisableAll()
			{
				var state = new DisableAllScreenState();
				_parent._states.Add(_stateName, state);
			}

			public void DisableAllBut(params object[] controls)
			{
				var state = new DisableAllButScreenState();
				state.Enable(controls);
				_parent._states.Add(_stateName, state);
			}

			public void EnableAll()
			{
				var state = new EnableAllScreenState();
				_parent._states.Add(_stateName, state);
			}

			public void EnableAllBut(params object[] controls)
			{
				var state = new EnableAllButScreenState();
				state.Disable(controls);
				_parent._states.Add(_stateName, state);
			}
		}

		#endregion
	}
}
