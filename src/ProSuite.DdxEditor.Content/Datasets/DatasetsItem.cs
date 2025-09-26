using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Models;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Datasets
{
	public class DatasetsItem<M> : EntityTypeItem<Dataset> where M : DdxModel
	{
		private readonly CoreDomainModelItemModelBuilder _modelBuilder;
		private readonly ModelItemBase<M> _parent;

		/// <summary>
		/// Initializes a new instance of the <see cref="DatasetsItem&lt;M&gt;"/> class.
		/// </summary>
		/// <param name="modelBuilder">The model builder.</param>
		/// <param name="parent">The parent.</param>
		public DatasetsItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder,
		                    [NotNull] ModelItemBase<M> parent)
			: base("Datasets", "Datasets contained in the model")
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));
			Assert.ArgumentNotNull(parent, nameof(parent));

			_modelBuilder = modelBuilder;
			_parent = parent;
		}

		[NotNull]
		public DdxModel Model => Assert.NotNull(_parent.GetEntity());

		protected override IEnumerable<Item> GetChildren()
		{
			return _modelBuilder.GetChildren(this);
		}

		protected override bool SortChildren => true;

		protected override Control CreateControlCore(IItemNavigation itemNavigation)
		{
			return CreateTableControl(GetGridRows, itemNavigation, "ModelName");
		}

		[NotNull]
		protected virtual IEnumerable<DatasetTableRow> GetGridRows()
		{
			return Model.GetDatasets<Dataset>(_modelBuilder.IncludeDeletedModelElements)
			            .Select(GetDatasetGridRow);
		}

		[NotNull]
		protected virtual DatasetTableRow GetDatasetGridRow([NotNull] Dataset dataset)
		{
			return new DatasetTableRow(dataset);
		}
	}
}
