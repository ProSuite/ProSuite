using System.Windows.Forms;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.Geodatabase;

namespace ProSuite.DdxEditor.Content.Connections
{
	// renamed because resource file path on build server was too long
	public partial class SdeConnProviderCtrl<T> : UserControl, IEntityPanel<T>
		where T : SdeConnectionProvider
	{
		public SdeConnProviderCtrl()
		{
			InitializeComponent();
		}

		#region IEntityPanel<T> Members

		public string Title => "Sde Connection Provider Properties";

		public void OnBindingTo(T entity) { }

		public void SetBinder(ScreenBinder<T> binder)
		{
			binder.Bind(m => m.RepositoryName)
			      .To(_textBoxRepositoryName)
			      .WithLabel(_labelRepositoryName);
			binder.Bind(m => m.VersionName)
			      .To(_textBoxVersionName)
			      .WithLabel(_labelVersionName);
		}

		public void OnBoundTo(T entity) { }

		#endregion
	}
}
