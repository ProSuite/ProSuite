using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.DdxEditor.Framework.ItemViews
{
	public partial class SegmentedEntityControl<T> :
		UserControl, ICompositeEntityControl<T, IViewObserver>
		where T : Entity
	{
		private readonly ScreenBinder<T> _binder;
		private IViewObserver _observer;
		private int _panelCount;
		private readonly List<IEntityPanel<T>> _entityPanels = new List<IEntityPanel<T>>();

		#region Constructors

		public SegmentedEntityControl()
		{
			InitializeComponent();

			_binder = CreateBinder();
		}

		#endregion

		#region ICompositeEntityControl<T,IViewObserver> Members

		public void AddPanel(IEntityPanel<T> panel)
		{
			Assert.ArgumentNotNull(panel, nameof(panel));
			var panelControl = panel as Control;
			Assert.NotNull(panelControl, "panels must derive from Control to be added");

			_entityPanels.Add(panel);

			panel.SetBinder(_binder);

			SuspendLayout();

			try
			{
				Control expander = CreateExpanderControl(panelControl, panel.Title);

				AddControl(expander);
			}
			finally
			{
				ResumeLayout(true);
			}
		}

		public IViewObserver Observer
		{
			get { return _observer; }
			set { _observer = value; }
		}

		public void BindTo(T target)
		{
			Assert.ArgumentNotNull(target, nameof(target));

			_entityPanels.ForEach(
				delegate(IEntityPanel<T> panel) { panel.OnBindingTo(target); });

			_binder.BindToModel(target);

			_entityPanels.ForEach(
				delegate(IEntityPanel<T> panel) { panel.OnBoundTo(target); });
		}

		#endregion

		private static Control CreateExpanderControl(Control contentControl, string title)
		{
			Assert.ArgumentNotNull(contentControl, nameof(contentControl));
			// title can be null or empty

			var expander = new ExpanderControl();

			expander.Title = title;
			expander.PanelHeight = contentControl.Height;
			contentControl.Dock = DockStyle.Top;
			expander.Content = contentControl;
			//expander.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

			return expander;
		}

		private void AddControl(Control expander)
		{
			_panelCount++;
			int row = _panelCount - 1;

			_tableLayoutPanel.RowCount = _panelCount;

			_tableLayoutPanel.Controls.Add(expander, 0, row);

			if (_tableLayoutPanel.RowStyles.Count < _panelCount)
			{
				_tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			}
			else
			{
				_tableLayoutPanel.RowStyles[_panelCount - 1].SizeType = SizeType.AutoSize;
			}

			expander.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		}

		private ScreenBinder<T> CreateBinder()
		{
			var binder = new ScreenBinder<T>(
				new ErrorProviderValidationMonitor(_errorProvider));
			binder.OnChange = BinderChanged;
			return binder;
		}

		private void BinderChanged()
		{
			if (_observer != null)
			{
				_observer.NotifyChanged(_binder.IsDirty());
			}
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				// clear the list of entity panels
				_entityPanels.Clear();

				if (components != null)
				{
					components.Dispose();
				}
			}

			base.Dispose(disposing);
		}
	}
}
