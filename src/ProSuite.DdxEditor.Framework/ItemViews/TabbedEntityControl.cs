using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.UI.ScreenBinding;

namespace ProSuite.DdxEditor.Framework.ItemViews
{
	public partial class TabbedEntityControl<E> : UserControl,
	                                              ICompositeEntityControl
	                                              <E, IViewObserver>
		where E : Entity
	{
		private readonly ScreenBinder<E> _binder;
		private IViewObserver _observer;

		public TabbedEntityControl()
		{
			InitializeComponent();

			_binder = CreateBinder();
		}

		#region ICompositeEntityControl<T,IViewObserver> Members

		public void AddPanel(IEntityPanel<E> panel)
		{
			Assert.ArgumentNotNull(panel, nameof(panel));
			var control = panel as Control;
			Assert.NotNull(control, "panels must derive from Control to be added");

			panel.SetBinder(_binder);

			var page = new TabPage(panel.Title);
			control.Dock = DockStyle.Fill;
			page.Controls.Add(control);

			_tabControl.TabPages.Add(page);
		}

		public void BindTo(E target)
		{
			Assert.ArgumentNotNull(target, nameof(target));

			_binder.BindToModel(target);
		}

		public IViewObserver Observer
		{
			get { return _observer; }
			set { _observer = value; }
		}

		#endregion

		private ScreenBinder<E> CreateBinder()
		{
			var binder = new ScreenBinder<E>(
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
	}
}
