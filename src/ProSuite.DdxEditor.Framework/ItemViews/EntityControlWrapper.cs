using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.ScreenBinding;

namespace ProSuite.DdxEditor.Framework.ItemViews
{
	public partial class EntityControlWrapper<E> : UserControl,
	                                               IEntityControlWrapper<E>
		where E : Entity
	{
		[NotNull] private readonly ScreenBinder<E> _binder;
		private IWrappedEntityControl<E> _wrappedEntityControl;

		public EntityControlWrapper()
		{
			InitializeComponent();

			_binder = CreateBinder();
		}

		#region IEntityControlWrapper<E> Members

		public IViewObserver Observer { get; set; }

		public void BindTo(E target)
		{
			Assert.ArgumentNotNull(target, nameof(target));

			_wrappedEntityControl?.OnBindingTo(target);

			_binder.BindToModel(target);
		}

		public void SetControl(IWrappedEntityControl<E> wrappedEntityControl)
		{
			Assert.ArgumentNotNull(wrappedEntityControl, nameof(wrappedEntityControl));
			var control = wrappedEntityControl as Control;
			Assert.NotNull(control, "entity controls must derive from Control to be added");
			Assert.Null(_wrappedEntityControl, "Entity control already assigned");

			_wrappedEntityControl = wrappedEntityControl;
			_wrappedEntityControl.SetBinder(_binder);

			SuspendLayout();

			try
			{
				control.Dock = DockStyle.Fill;
				Controls.Add(control);
			}
			finally
			{
				ResumeLayout(true);
			}
		}

		public void OnBoundTo(E entity)
		{
			_wrappedEntityControl.OnBoundTo(entity);
		}

		#endregion

		[NotNull]
		private ScreenBinder<E> CreateBinder()
		{
			return new ScreenBinder<E>(
				       new ErrorProviderValidationMonitor(_errorProvider))
			       {
				       OnChange = BinderChanged
			       };
		}

		private void BinderChanged()
		{
			Observer?.NotifyChanged(_binder.IsDirty());
		}
	}
}
