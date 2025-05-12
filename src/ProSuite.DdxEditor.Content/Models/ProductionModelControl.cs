using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Models
{
	public partial class ProductionModelControl<T> : UserControl, IEntityPanel<T>
		where T : ProductionModel
	{
		public ProductionModelControl()
		{
			InitializeComponent();
		}

		#region IEntityPanel<T> Members

		public string Title => "Production Model Properties";

		public void OnBindingTo(T entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			_textBoxMultipointIssueDataset.Text = GetName(entity.ErrorMultipointDataset);
			_textBoxLineIssueDataset.Text = GetName(entity.ErrorLineDataset);
			_textBoxPolygonIssueDataset.Text = GetName(entity.ErrorPolygonDataset);
			_textBoxMultiPatchIssueDataset.Text = GetName(entity.ErrorMultiPatchDataset);
			_textBoxTableIssueDataset.Text = GetName(entity.ErrorTableDataset);
		}

		public void SetBinder(ScreenBinder<T> binder) { }

		public void OnBoundTo(T entity) { }

		#endregion

		[NotNull]
		private static string GetName([CanBeNull] IModelElement modelElement)
		{
			return modelElement == null
				       ? string.Empty
				       : modelElement.Name;
		}
	}
}
