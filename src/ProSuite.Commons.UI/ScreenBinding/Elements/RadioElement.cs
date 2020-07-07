using System;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.ScreenBinding.ScreenStates;

namespace ProSuite.Commons.UI.ScreenBinding.Elements
{
	public class RadioElement<ENUM> : BoundScreenElement<RadioButton, ENUM>
	{
		private bool _latched;

		private readonly RadioButtonGroup<ENUM> _group;
		private ENUM _boundValue;

		#region Constructors

		public RadioElement([NotNull] IPropertyAccessor accessor,
		                    [NotNull] RadioButton control,
		                    [NotNull] RadioButtonGroup<ENUM> group)
			: base(accessor, control)
		{
			Assert.ArgumentNotNull(group, nameof(@group));

			_group = group;
			_group.Add(this);

			Alias = control.Text;
			control.CheckedChanged += control_CheckedChanged;
		}

		#endregion

		public ENUM BoundValue
		{
			get { return _boundValue; }
			set { _boundValue = value; }
		}

		public void RegisterOnClickHandler(Action handler)
		{
			BoundControl.Click += delegate { RunIfNotLatched(handler); };
		}

		internal bool Latched
		{
			get { return _latched; }
			set { _latched = value; }
		}

		public override void EnableControl(IScreenState state)
		{
			Assert.ArgumentNotNull(state, nameof(state));

			Latched = true;
			base.EnableControl(state);
			Latched = false;
		}

		protected override void TearDown()
		{
			BoundControl.CheckedChanged += control_CheckedChanged;
		}

		public override bool ApplyChanges()
		{
			if (! BoundControl.Checked)
			{
				return true;
			}

			base.ApplyChanges();
			return true;
		}

		protected override ENUM GetValueFromControl()
		{
			return _boundValue;
		}

		protected override void ResetControl(ENUM originalValue)
		{
			_group.LatchPeers(this);
			try
			{
				BoundControl.Checked = originalValue.Equals(_boundValue);
			}
			finally
			{
				_group.UnLatchPeers(this);
			}
		}

		private void control_CheckedChanged(object sender, EventArgs e)
		{
			ElementValueChanged();
		}
	}
}
