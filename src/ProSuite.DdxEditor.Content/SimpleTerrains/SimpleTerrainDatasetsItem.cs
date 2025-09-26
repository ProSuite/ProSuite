using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.WinForms;
using ProSuite.DdxEditor.Content.Models;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;

namespace ProSuite.DdxEditor.Content.SimpleTerrains
{
	public class SimpleTerrainDatasetsItem : EntityTypeItem<SimpleTerrainDataset>
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;
		[NotNull] private static readonly Image _image;
		[NotNull] private static readonly Image _selectedImage;

		static SimpleTerrainDatasetsItem()
		{
			_image = ItemUtils.GetGroupItemImage(Resources.DatasetTypeSurfaceOverlay);
			_selectedImage =
				ItemUtils.GetGroupItemSelectedImage(Resources.DatasetTypeSurfaceOverlay);
		}

		public SimpleTerrainDatasetsItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder)
			: base(
				"Simple Terrains",
				"Definition of simple terrains for a data model")
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
		}

		public override Image Image => _image;

		public override Image SelectedImage => _selectedImage;

		protected override bool AllowDeleteSelectedChildren => true;

		protected override IEnumerable<Item> GetChildren()
		{
			return _modelBuilder.GetChildren(this);
		}

		protected override void CollectCommands(List<ICommand> commands,
		                                        IApplicationController applicationController)
		{
			base.CollectCommands(commands, applicationController);

			commands.Add(new AddSimpleTerrainDatasetCommand(this, applicationController));
			commands.Add(new ImportSimpleTerrainDatasetsCommand(this, applicationController));
			commands.Add(new ExportSimpleTerrainDatasetsCommand(
				             this, applicationController, filterByModel: false));
			commands.Add(new ExportSimpleTerrainDatasetsCommand(
				             this, applicationController, filterByModel: true));
		}

		protected override Control CreateControlCore(IItemNavigation itemNavigation)
		{
			return CreateTableControl(GetTableRows, itemNavigation);
		}

		[NotNull]
		protected virtual IEnumerable<SimpleTerrainDatasetTableRow> GetTableRows()
		{
			return _modelBuilder.SimpleTerrainDatasets.GetAll()
			                    .Select(entity => new SimpleTerrainDatasetTableRow(entity));
		}

		public IList<ModelTableRow> GetModelTableRows()
		{
			return
				_modelBuilder.ReadOnlyTransaction(
					() =>
						_modelBuilder.Models.GetAll()
						             .Select(entity => new ModelTableRow(entity))
						             .ToList());
		}

		[NotNull]
		public SimpleTerrainDatasetItem AddSimpleTerrainDatasetItem()
		{
			var simpleTerrainDataset = new ModelSimpleTerrainDataset();
			simpleTerrainDataset.GeometryType =
				_modelBuilder.Resolve<IGeometryTypeRepository>().GetAll()
				             .First(x => x is GeometryTypeTerrain);

			var item = new SimpleTerrainDatasetItem(_modelBuilder, simpleTerrainDataset,
			                                        _modelBuilder.SimpleTerrainDatasets);

			AddChild(item);

			item.NotifyChanged();

			return item;
		}

		public void ImportSimpleTerrainDatasets([NotNull] string fileName)
		{
			Assert.ArgumentNotNullOrEmpty(fileName, nameof(fileName));

			using (new WaitCursor())
			{
				_modelBuilder.NewTransaction(
					() => _modelBuilder.SimpleTerrainsImporter.Import(fileName));
			}

			_msg.InfoFormat("Simple Terrain Datasets imported from {0}", fileName);

			RefreshChildren();
		}

		public void ExportSimpleTerrainDatasets([NotNull] string fileName, [CanBeNull] DdxModel model)
		{
			Assert.ArgumentNotNullOrEmpty(fileName, nameof(fileName));

			using (new WaitCursor())
			{
				_modelBuilder.SimpleTerrainsExporter.Export(fileName, model);
			}

			if (model != null)
			{
				_msg.InfoFormat("Simple Terrain Datasets for {0} exported to {1}", model.Name,
				                fileName);
			}
			else
			{
				_msg.InfoFormat("Simple Terrain Datasets exported to {0}", fileName);
			}
		}
	}
}
