using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.UI.Core.DataModel.ResourceLookup;

namespace ProSuite.DdxEditor.Content.AssociationEnds
{
	public partial class AssociationEndControl : UserControl,
	                                             IAssociationEndView
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AssociationEndControl"/> class.
		/// </summary>
		public AssociationEndControl()
		{
			InitializeComponent();
		}

		public string Title => "Association End Properties";

		public IAssociationEndObserver Observer { get; set; }

		public void OnBindingTo(AssociationEnd entity) { }

		public void SetBinder(ScreenBinder<AssociationEnd> binder)
		{
			binder.Bind(m => m.Name)
			      .To(_textBoxName)
			      .WithLabel(_labelName);

			binder.Bind(m => m.CopyPolicy)
			      .To(_comboBoxCopyPolicy)
			      .FillWithEnum<CopyPolicy>()
			      .WithLabel(_labelCopyPolicy);

			binder.Bind(m => m.DocumentAssociationEdit)
			      .To(_booleanComboboxDocumentAssociationEdit);

			binder.Bind(m => m.CascadeDeletion)
			      .To(_booleanComboboxCascadeDeletion);
		}

		public void OnBoundTo(AssociationEnd entity)
		{
			_textBoxDataset.Text = entity.ObjectDataset.Name;
			_textBoxAssociation.Text = entity.Association.Name;

			_textBoxCardinality.Text = entity.CardinalityText;

			RenderEndKey(entity, _labelKey, _textBoxKey);
			RenderEndKey(entity.OppositeEnd, _labelOppositeEndKey, _textBoxOppositeEndKey);

			_textBoxOppositeEndDataset.Text = entity.OppositeDataset.Name;

			_booleanComboboxDocumentAssociationEdit.Enabled =
				entity.CanChangeDocumentAssociationEdit;

			_booleanComboboxCascadeDeletion.Enabled = entity.CanChangeCascadeDeletion;

			_pictureBoxAssociationCardinality.Image =
				AssociationImageLookup.GetImage(entity.Association);
			_pictureBoxAssociationEndType.Image =
				AssociationEndImageLookup.GetImage(entity);
		}

		private static void RenderEndKey([NotNull] AssociationEnd associationEnd,
		                                 [NotNull] Control label,
		                                 [NotNull] Control textBox)
		{
			Assert.ArgumentNotNull(associationEnd, nameof(associationEnd));
			Assert.ArgumentNotNull(label, nameof(label));
			Assert.ArgumentNotNull(textBox, nameof(textBox));

			if (associationEnd.HasPrimaryKey)
			{
				label.Text = @"Primary Key";
				textBox.Text = associationEnd.PrimaryKey.Name;
			}
			else if (associationEnd.HasForeignKey)
			{
				label.Text = @"Foreign Key";
				textBox.Text = associationEnd.ForeignKey.Name;
			}
		}
	}
}
