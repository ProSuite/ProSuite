using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.Commons.UI.Finder;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.Commons.Validation;
using ProSuite.DdxEditor.Content.Datasets;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;

namespace ProSuite.DdxEditor.Content.SimpleTerrains
{
	public class SimpleTerrainDatasetItem
		: SimpleEntityItem<SimpleTerrainDataset, SimpleTerrainDataset>
	{
		private readonly CoreDomainModelItemModelBuilder _modelBuilder;

		public SimpleTerrainDatasetItem(CoreDomainModelItemModelBuilder modelBuilder,
		                                SimpleTerrainDataset entity,
		                                IRepository<SimpleTerrainDataset> repository)
			: base(entity, repository)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
		}

		protected override IWrappedEntityControl<SimpleTerrainDataset> CreateEntityControl(
			IItemNavigation itemNavigation)
		{
			var control = new SimpleTerrainDatasetControl();
			new SimpleTerrainDatasetPresenter(this, control, FindDatasetCategory,
			                                  GetSourceDatasetsToAdd);
			// attach presenter
			return control;
		}

		protected override string GetText(SimpleTerrainDataset entity)
		{
			return string.IsNullOrEmpty(entity.Name) ? "<untitled>" : entity.Name;
		}

		protected override bool AllowDelete => true;
		public override Image Image => Resources.DatasetTypeSurface;

		protected override void IsValidForPersistenceCore(SimpleTerrainDataset entity,
		                                                  Notification notification)
		{
			string terrainName = entity.Name;

			if (terrainName == null)
			{
				notification.RegisterMessage("Name", "Required field", Severity.Error);
				return;
			}

			if (entity.Sources.Count == 0)
			{
				notification.RegisterMessage("Terrain Source Datasets",
				                             "At least one source must be specified",
				                             Severity.Error);
				return;
			}

			bool nameIsQualified = ModelElementNameUtils.IsQualifiedName(terrainName);
			string schemaOwner = entity.Model.DefaultDatabaseSchemaOwner + ".";

			if (entity.Model.ElementNamesAreQualified)
			{
				string message =
					$"The model uses qualified dataset names. The name must be qualified with the model's schema owner ({schemaOwner})";

				if (! nameIsQualified)
				{
					notification.RegisterMessage("Name", message, Severity.Error);
				}
				else
				{
					if (! terrainName.StartsWith(schemaOwner))
					{
						notification.RegisterMessage("Name", message, Severity.Error);
					}
				}
			}
			else
			{
				if (nameIsQualified)
				{
					notification.RegisterMessage(
						"Name",
						"The model uses un-qualified dataset names. Name must not be qualified",
						Severity.Error);
				}
			}

			string unqualifiedName =
				nameIsQualified ? terrainName.Remove(0, schemaOwner.Length) : terrainName;

			if (! ModelElementNameUtils.IsValidUnqualifiedTableName(
				    unqualifiedName, out string nameMessage))
			{
				notification.RegisterMessage("Name", nameMessage, Severity.Error);
			}
		}

		private IList<DatasetTableRow> GetSourceDatasetsToAdd(
			IWin32Window owner, params ColumnDescriptor[] columns)
		{
			Assert.ArgumentNotNull(owner, nameof(owner));

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
				    shapeType.ShapeType != ProSuiteGeometryType.Polyline &&
				    shapeType.ShapeType != ProSuiteGeometryType.Multipoint)
				{
					continue;
				}

				if (! _modelBuilder.CanParticipateInSimpleTerrain(vectorDataset))
				{
					continue;
				}

				datasetTableRows.Add(new DatasetTableRow(vectorDataset));
			}

			IFinder<DatasetTableRow> finder = new Finder<DatasetTableRow>();

			return finder.ShowDialog(owner, datasetTableRows, true, columns);
		}

		private DatasetCategory FindDatasetCategory(
			IWin32Window owner, params ColumnDescriptor[] columns)
		{
			Assert.ArgumentNotNull(owner, nameof(owner));

			IDatasetCategoryRepository repository = _modelBuilder.DatasetCategories;
			if (repository == null)
			{
				return null;
			}

			IList<DatasetCategory> all = _modelBuilder.ReadOnlyTransaction(
				() => repository.GetAll());

			var finder = new Finder<DatasetCategory>();
			return finder.ShowDialog(owner, all, columns);
		}
	}
}
