using System;
using System.Drawing;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.ScreenBinding.Drivers;
using ProSuite.Commons.UI.ScreenBinding.ScreenStates;
using ProSuite.Commons.UI.ScreenBinding.Stylers;

namespace ProSuite.Commons.UI.ScreenBinding.Elements
{
	public class ScreenElement<T> : IScreenElement
	{
		private ActivationMode _activationMode;
		private readonly Color _originalColor;

		private Action<object> _showAction = delegate { }; // (target) => { };
		private Action<object> _enableAction = delegate { }; // (target) => { };

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ScreenElement&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="control">The control.</param>
		public ScreenElement([NotNull] T control)
		{
			Assert.ArgumentNotNull(control, nameof(control));

			Driver = ControlDriverFactory.GetDriver(control);

			Styler.Default.ApplyStyle(control);
			_originalColor = Driver.BackColor;
			BoundControl = control;
		}

		#endregion

		public T BoundControl { get; }

		#region IScreenElement Members

		public virtual void Highlight(Color color)
		{
			Driver.BackColor = color;
		}

		public virtual void RemoveHighlight()
		{
			Driver.BackColor = _originalColor;
		}

		public void Hide()
		{
			Driver.Visible = false;
			if (Label != null)
			{
				Label.Visible = false;
			}

			if (PostLabel != null)
			{
				PostLabel.Visible = false;
			}
		}

		public void Show()
		{
			Driver.Visible = true;
			if (Label != null)
			{
				Label.Visible = true;
			}

			if (PostLabel != null)
			{
				PostLabel.Visible = true;
			}
		}

		public void CopyFrom(IScreenDriver driver)
		{
			IScreenElement peer = driver.FindElementForControl(Driver.Control);
			if (peer != null)
			{
				CopyFrom(peer);
			}
		}

		public object Control => Driver.Control;

		public ActivationMode ActivationMode
		{
			get { return _activationMode; }
			set
			{
				_activationMode = value;
				if (_activationMode == ActivationMode.ReadOnly)
				{
					// Disable();
					SetReadOnly(); // experimental
				}
			}
		}

		public string Alias { get; set; }

		public IControlDriver Label { get; set; }

		public IControlDriver PostLabel { get; set; }

		public virtual void EnableControl(IScreenState state)
		{
			switch (_activationMode)
			{
				case ActivationMode.ReadOnly:
					Disable();
					break;

				case ActivationMode.AlwaysActive:
					Enable();
					break;

				case ActivationMode.Normal:
					if (state.IsControlEnabled(Driver.Control))
					{
						Enable();
						PostEnable();
					}
					else
					{
						Disable();
					}

					break;
			}
		}

		public void Enable()
		{
			IsReadOnly = false;

			EnableCore();
		}

		public void Disable()
		{
			IsReadOnly = true;

			DisableCore();
		}

		public string ToolTipText
		{
			get { return Driver.ToolTipText; }
			set { Driver.ToolTipText = value; }
		}

		public bool Matches(string labelText)
		{
			if (Label != null && Label.Text == labelText)
			{
				return true;
			}

			if (! string.IsNullOrEmpty(Alias))
			{
				return Alias == labelText;
			}

			return false;
		}

		public virtual void Focus()
		{
			Driver.Focus();
		}

		public string LabelText
		{
			get
			{
				if (! string.IsNullOrEmpty(Alias))
				{
					return Alias;
				}

				return Label != null
					       ? Label.Text
					       : string.Empty;
			}
		}

		public void UpdateDisplayState(object target)
		{
			_showAction(target);
			_enableAction(target);
		}

		public void BindVisibilityTo(IPropertyAccessor accessor)
		{
			_showAction = target =>
			{
				var isVisible = (bool) accessor.GetValue(target);
				if (isVisible)
				{
					Show();
				}

				if (! isVisible)
				{
					Hide();
				}
			};
		}

		public void BindEnabledTo(IPropertyAccessor accessor)
		{
			_enableAction = target =>
			{
				var isEnabled = (bool) accessor.GetValue(target);

				if (isEnabled)
				{
					Enable();
				}

				if (! isEnabled)
				{
					Disable();
				}
			};
		}

		#endregion

		public void CopyFrom(IScreenElement element)
		{
			Assert.ArgumentNotNull(element, nameof(element));

			Label = element.Label;
			Alias = element.Alias;
			PostLabel = element.PostLabel;
		}

		[PublicAPI]
		protected virtual void SetReadOnly()
		{
			Disable();
		}

		protected bool IsReadOnly { get; private set; }

		protected virtual void EnableCore()
		{
			Driver.Enabled = true;
		}

		protected virtual void DisableCore()
		{
			Driver.Enabled = false;
		}

		protected IControlDriver Driver { get; }

		protected virtual void PostEnable() { }
	}
}
