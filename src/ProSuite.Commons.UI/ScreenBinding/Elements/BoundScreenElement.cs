using System;
using System.Collections.Generic;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.ScreenBinding.Settings;
using ProSuite.Commons.Validation;

namespace ProSuite.Commons.UI.ScreenBinding.Elements
{
	public abstract class BoundScreenElement<CONTROLTYPE, PROPERTYTYPE>
		: ScreenElement<CONTROLTYPE>, IBoundScreenElement
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly List<Action> _changeActions = new List<Action>();
		private readonly List<Action> _lostFocusActions = new List<Action>();
		private IScreenBinder _binder = new NulloBinder();
		private object _lastValue;
		private bool _latched; // todo: use Latch
		private PROPERTYTYPE _originalValue;
		private object _target;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="BoundScreenElement&lt;CONTROLTYPE, PROPERTYTYPE&gt;"/> class.
		/// </summary>
		/// <param name="accessor">The accessor.</param>
		/// <param name="control">The control.</param>
		protected BoundScreenElement([NotNull] IPropertyAccessor accessor,
		                             [NotNull] CONTROLTYPE control)
			: base(control)
		{
			Assert.ArgumentNotNull(accessor, nameof(accessor));

			Accessor = accessor;
			Driver.AddLostFocusHandler(HandleLostFocus);
		}

		#endregion

		protected IPropertyAccessor Accessor { get; }

		protected bool IsBound => _target != null;

		#region IBoundScreenElement Members

		public IScreenBinder Binder
		{
			set { _binder = value; }
		}

		public virtual void Bind(object target)
		{
			Assert.ArgumentNotNull(target, nameof(target));

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("Bind() {0}", Accessor.Name);
			}

			try
			{
				_target = target;

				_originalValue = GetValueFromModel();
				UpdateControl(_originalValue);

				_lastValue = _originalValue;

				// immediately validate
				if (_binder != null)
				{
					_binder.Validate(this);
				}
			}
			catch (Exception e)
			{
				throw new InvalidOperationException(
					string.Format("Unable to bind property " + Accessor.FieldName), e);
			}
		}

		public virtual bool ApplyChanges()
		{
			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("ApplyChanges() {0}", Accessor.Name);
			}

			if (! IsBound)
			{
				return true;
			}

			if (! IsReadOnly)
			{
				Accessor.SetValue(_target, GetValueFromControl());
			}

			return true;
		}

		public virtual void Reset()
		{
			UpdateControl(_originalValue);
			Accessor.SetValue(_target, _originalValue);
		}

		public virtual void Update()
		{
			PROPERTYTYPE newValue = GetValueFromModel();
			UpdateControl(newValue);
		}

		public void RegisterChangeHandler(Action action)
		{
			_changeActions.Add(action);
		}

		public void RegisterLostFocusHandler(Action action)
		{
			_lostFocusActions.Add(action);
		}

		public NotificationMessage[] Validate()
		{
			return _target == null
				       ? Array.Empty<NotificationMessage>()
				       : Accessor.Validate(_target);
		}

		public string FieldName => Accessor.FieldName;

		public override void Focus()
		{
			bool oldLatched = _latched;
			try
			{
				_latched = true;
				Driver.Focus();
				PostFocus();
			}
			finally
			{
				_latched = oldLatched;
			}
		}

		public void StopBinding()
		{
			_target = null;
			Driver.RemoveLostFocusHandler(HandleLostFocus);
			TearDown();
		}

		public virtual void SetDefaults()
		{
			// The latching and explicit call to ElementValueChanged() is to make sure
			// elements like TextBox fire change events correctly on this method

			var changed = false;

			WithinLatch(
				delegate
				{
					object lastValue = LastChosenValues.Retrieve(Driver);
					if (lastValue != null)
					{
						changed = true;
						ResetControl((PROPERTYTYPE) lastValue);
					}
				});

			if (changed && IsBound)
			{
				ElementValueChanged();
			}
		}

		public void RememberLastChoice()
		{
			_changeActions.Add(StoreValue);
		}

		public void RebindAllOnChange()
		{
			_changeActions.Insert(0, () => _binder.UpdateScreen());
		}

		public bool IsDirty()
		{
			if (_target == null)
			{
				return false;
			}

			object propertyValue = GetValueFromModel();

			return ! Equals(_originalValue, propertyValue);

			//if (propertyValue == null)
			//{
			//    if (_originalValue != null)
			//    {
			//        return true;
			//    }
			//}
			//else if (!propertyValue.Equals(_originalValue))
			//{
			//    return true;
			//}

			//return false;
		}

		public object GetValue()
		{
			return GetValueFromModel();
		}

		#endregion

		protected override void PostEnable()
		{
			UpdateDisplayState(_target);
		}

		protected abstract PROPERTYTYPE GetValueFromControl();

		protected abstract void ResetControl(PROPERTYTYPE originalValue);

		protected virtual bool ValidatesOnLostFocus()
		{
			return true;
		}

		protected void NullOutTheElement()
		{
			Accessor.SetValue(_target, null);
			Update();
		}

		protected void UpdateControl([NotNull] PROPERTYTYPE newValue)
		{
			WithinLatch(() => ResetControl(newValue));
		}

		protected virtual void PostFocus() { }

		protected void RunIfNotLatched([NotNull] Action action)
		{
			if (IsLatched())
			{
				return;
			}

			action();
		}

		protected void ElementValueChanged()
		{
			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("ElementValueChanged() {0}", Accessor.Name);
			}

			if (IsLatched())
			{
				return;
			}

			if (IsReadOnly)
			{
				return;
			}

			ApplyChanges();

			foreach (Action handler in _changeActions)
			{
				handler();
			}

			if (! IsBound)
			{
				return;
			}

			Notify();

			//if (IsDirty())
			//{
			//    Notify();
			//}
			//else
			//{
			//    if (_msg.IsVerboseDebugEnabled)
			//    {
			//        _msg.DebugFormat("{0} is not dirty", _accessor.Name);
			//    }
			//}
		}

		protected void Notify()
		{
			object currentControlValue = GetValueFromControl();

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat(
					"Notify() {0}: _lastValue={1} GetValueFromControl()={2}",
					Accessor.Name, _lastValue, currentControlValue);
			}

			if (Equals(_lastValue, currentControlValue))
			{
				return;
			}

			WithinLatch(
				delegate
				{
					_binder.Validate(this);

					try
					{
						_lastValue = GetValueFromModel();

						// ORIGINAL >>
						//if (_lastValue != null)
						//{
						//    _binder.OnChange();
						//}
						// << ORIGINAL

						_binder.OnChange();
					}
					catch (Exception e)
					{
						ShowErrorMessages(e.Message);

						_msg.Warn("Exception in Notify() ignored", e);
					}
				});
		}

		protected void ShowErrorMessages(params string[] messages)
		{
			_binder.ShowErrorMessages(this, messages);
		}

		protected abstract void TearDown();

		private PROPERTYTYPE GetValueFromModel()
		{
			object rawValue = Accessor.GetValue(_target);

			return rawValue == null ? default : (PROPERTYTYPE) rawValue;
		}

		private void StoreValue()
		{
			object currentValue = GetValueFromControl();
			LastChosenValues.Store(Driver, currentValue);
		}

		private void HandleLostFocus()
		{
			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("HandleLostFocus() for accessor {0}", Accessor.Name);
			}

			_lostFocusActions.ForEach(RunIfNotLatched);
			//action => RunIfNotLatched(action));

			if (ValidatesOnLostFocus())
			{
				RunIfNotLatched(Notify);
			}
		}

		#region Latch

		protected void WithinLatch(Action action)
		{
			Assert.ArgumentNotNull(action, nameof(action));

			if (_latched)
			{
				return;
			}

			try
			{
				_latched = true;
				action();
			}
			finally
			{
				_latched = false;
			}
		}

		private bool IsLatched()
		{
			return _latched || _binder.IsLatched;
		}

		#endregion
	}
}
