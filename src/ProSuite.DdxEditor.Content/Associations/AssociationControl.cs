using System.Windows.Forms;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Associations
{
	public partial class AssociationControl : UserControl,
	                                          IWrappedEntityControl<Association>
	{
		public AssociationControl()
		{
			InitializeComponent();
		}

		public string Title => "Association Properties";

		public void OnBindingTo(Association entity) { }

		public void SetBinder(ScreenBinder<Association> binder)
		{
			binder.Bind(m => m.UnqualifiedName)
			      .To(_textBoxName)
			      .AsReadOnly()
			      .WithLabel(_labelName);

			binder.Bind(m => m.Cardinality)
			      .To(_textBoxCardinality)
			      .AsReadOnly()
			      .WithLabel(_labelCardinality);

			binder.Bind(m => m.Description)
			      .To(_textBoxDescription)
			      .WithLabel(_labelDescription);

			binder.Bind(m => m.NotUsedForDerivedTableGeometry)
			      .To(_booleanComboboxNotUsedForDerivedGeometry);
		}

		public void OnBoundTo(Association entity)
		{
			_textBoxOriginDataset.Text = entity.OriginEnd.ObjectDataset.Name;
			_textBoxDestinationDataset.Text =
				entity.DestinationEnd.ObjectDataset.Name;

			if (entity.IsAttributed)
			{
				_textBoxUsesAssociationTable.Text = "Yes";
				_textBoxAssociationTableName.Text = entity.Name;
				_textBoxAssociationTableName.Visible = true;
				_labelAssociationTableName.Visible = true;
			}
			else
			{
				_textBoxUsesAssociationTable.Text = "No";
				_textBoxAssociationTableName.Text = string.Empty;
				_textBoxAssociationTableName.Visible = false;
				_labelAssociationTableName.Visible = false;
			}
		}
	}
}
