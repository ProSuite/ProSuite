using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.Commons.UI.Finder;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Content.Datasets;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.LinearNetworks
{
	public class LinearNetworkItem : SimpleEntityItem<LinearNetwork, LinearNetwork>
	{
		private readonly CoreDomainModelItemModelBuilder _modelBuilder;

		public LinearNetworkItem(CoreDomainModelItemModelBuilder modelBuilder,
		                         LinearNetwork entity,
		                         IRepository<LinearNetwork> repository)
			: base(entity, repository)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
		}

		protected override IWrappedEntityControl<LinearNetwork> CreateEntityControl(
			IItemNavigation itemNavigation)
		{
			var control = new LinearNetworkControl();
			new LinearNetworkPresenter(this, control, GetNetworkDatasetsToAdd);
			// attach presenter
			return control;
		}

		protected override string GetText(LinearNetwork entity)
		{
			return string.IsNullOrEmpty(entity.Name) ? "<untitled>" : entity.Name;
		}

		protected override bool AllowDelete => true;
		public override Image Image => Resources.DatasetTypeLinearNetwork;

		private IList<DatasetTableRow> GetNetworkDatasetsToAdd(
			IWin32Window owner, params ColumnDescriptor[] columns)
		{
			Assert.NotNull(owner, "owner");

			IList<DatasetTableRow> datasetTableRows = new List<DatasetTableRow>();

			foreach (VectorDataset vectorDataset in _modelBuilder.ReadOnlyTransaction(
				         () => _modelBuilder.Datasets.GetAll<VectorDataset>()))
			{
				if (vectorDataset.Deleted)
				{
					continue;
				}

				GeometryTypeShape shapeType = vectorDataset.GeometryType as GeometryTypeShape;

				if (shapeType == null)
				{
					continue;
				}

				if (shapeType.ShapeType != ProSuiteGeometryType.Point &&
				    shapeType.ShapeType != ProSuiteGeometryType.Polyline)
				{
					continue;
				}

				if (! _modelBuilder.CanParticipateInLinearNetwork(vectorDataset))
				{
					continue;
				}

				datasetTableRows.Add(new DatasetTableRow(vectorDataset));
			}

			IFinder<DatasetTableRow> finder = new Finder<DatasetTableRow>();

			return finder.ShowDialog(owner, datasetTableRows, true, columns);
		}
	}
}
