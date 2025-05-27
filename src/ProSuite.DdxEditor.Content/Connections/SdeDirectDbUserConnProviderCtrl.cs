using System.Windows.Forms;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.Geodatabase;

namespace ProSuite.DdxEditor.Content.Connections
{
	// renamed because resource file path on build server was too long
	public partial class SdeDirectDbUserConnProviderCtrl<T> : UserControl,
	                                                          IEntityPanel<T>
		where T : SdeDirectDbUserConnectionProvider
	{
		public SdeDirectDbUserConnProviderCtrl()
		{
			InitializeComponent();
		}

		#region IEntityPanel<T> Members

		public string Title => "Sde Direct Db User Connection Provider Properties";

		public void OnBindingTo(T entity) { }

		public void SetBinder(ScreenBinder<T> binder)
		{
			binder.Bind(m => m.UserName)
			      .To(_textBoxUserName)
			      .WithLabel(_labelUserName);

			binder.Bind(m => m.PlainTextPassword)
			      .To(_textBoxPlainTextPassword)
			      .WithLabel(_labelPlainTextPassword);
		}

		public void OnBoundTo(T entity) { }

		#endregion
	}
}
