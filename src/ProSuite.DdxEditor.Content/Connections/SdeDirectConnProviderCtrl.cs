using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.Geodatabase;

namespace ProSuite.DdxEditor.Content.Connections
{
	// renamed because resource file path on build server was too long
	public partial class SdeDirectConnProviderCtrl<T> : UserControl,
	                                                    IEntityPanel<T>
		where T : SdeDirectConnectionProvider
	{
		public SdeDirectConnProviderCtrl()
		{
			InitializeComponent();
		}

		#region IEntityPanel<T> Members

		public string Title => "Sde Direct Connection Provider Properties";

		public void OnBindingTo(T entity) { }

		public void SetBinder(ScreenBinder<T> binder)
		{
			binder.Bind(m => m.DatabaseName)
			      .To(_textBoxDatabaseName)
			      .WithLabel(_labelDatabaseName);

			binder.Bind(m => m.DatabaseType)
			      .To(_comboBoxDatabaseType)
			      .FillWith(GetDatabaseTypePickList())
			      .WithLabel(_labelDatabaseType);
		}

		public void OnBoundTo(T entity) { }

		#endregion

		[NotNull]
		private static Picklist<DatabaseTypeItem> GetDatabaseTypePickList()
		{
			return new Picklist<DatabaseTypeItem>(
				       new[]
				       {
					       new DatabaseTypeItem
					       {
						       Value = DatabaseType.SqlServer,
						       DisplayName = "Microsoft SQL Server or SQL Server Express"
					       },
					       new DatabaseTypeItem
					       {
						       Value = DatabaseType.PostgreSQL,
						       DisplayName = "PostgreSQL"
					       },
					       new DatabaseTypeItem
					       {
						       Value = DatabaseType.Oracle11,
						       DisplayName = "Oracle"
					       }
				       }, noSort: true)
			       {
				       DisplayMember = "DisplayName",
				       ValueMember = "Value"
			       };
		}

		private class DatabaseTypeItem
		{
			[UsedImplicitly]
			public DatabaseType Value { get; set; }

			[UsedImplicitly]
			public string DisplayName { get; set; }
		}
	}
}
