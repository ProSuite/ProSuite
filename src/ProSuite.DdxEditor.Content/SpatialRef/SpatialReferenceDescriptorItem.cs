using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.Commons.UI.Finder;
using ProSuite.Commons.UI.WinForms;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.Commons.Validation;
using ProSuite.DdxEditor.Content.Datasets;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Dependencies;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.Geodatabase;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;
using ProSuite.DomainModel.Core.Geodatabase;
using ProSuite.UI.DataModel.ResourceLookup;

namespace ProSuite.DdxEditor.Content.SpatialRef
{
	public class SpatialReferenceDescriptorItem :
		SimpleEntityItem<SpatialReferenceDescriptor, SpatialReferenceDescriptor>
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly CoreDomainModelItemModelBuilder _modelBuilder;

		/// <summary>
		/// Initializes a new instance of the <see cref="SpatialReferenceDescriptorItem"/> class.
		/// </summary>
		/// <param name="modelBuilder">The model builder.</param>
		/// <param name="descriptor">The descriptor.</param>
		/// <param name="repository">The repository.</param>
		public SpatialReferenceDescriptorItem(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
			[NotNull] SpatialReferenceDescriptor descriptor,
			[NotNull] IRepository<SpatialReferenceDescriptor> repository)
			: base(descriptor, repository)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
		}

		public override Image Image => Resources.SpatialReferenceItem;

		protected override IWrappedEntityControl<SpatialReferenceDescriptor>
			CreateEntityControl(IItemNavigation itemNavigation)
		{
			var control = new SpatialReferenceDescriptorControl();
			new SpatialReferenceDescriptorPresenter(this, control);
			return control;
		}

		public override IList<DependingItem> GetDependingItems()
		{
			return _modelBuilder.GetDependingItems(GetEntity());
		}

		[CanBeNull]
		public string GetXmlStringFromDataset([NotNull] IWin32Window owner,
		                                      [CanBeNull] out ISpatialReference
			                                      spatialReference)
		{
			Assert.ArgumentNotNull(owner, nameof(owner));

			var tableRows = new List<DatasetTableRow>();

			_modelBuilder.ReadOnlyTransaction(
				delegate
				{
					IDatasetRepository repository = _modelBuilder.Datasets;

					foreach (VectorDataset dataset in repository.GetAll<VectorDataset>())
					{
						if (dataset.Deleted)
						{
							continue;
						}

						tableRows.Add(new DatasetTableRow(dataset) {Selectable = true});
					}
				});

			IFinder<DatasetTableRow> finder = new Finder<DatasetTableRow>();
			DatasetTableRow selectedTableRow = finder.ShowDialog(owner, tableRows);

			if (selectedTableRow == null)
			{
				spatialReference = null;
				return null;
			}

			var vectorDataset = (VectorDataset) selectedTableRow.Entity;

			const bool allowAlways = true;
			IFeatureClass featureClass =
				ModelElementUtils.TryOpenFromMasterDatabase(vectorDataset, allowAlways);

			Assert.NotNull(featureClass,
			               "Unable to open feature class from model master database");

			spatialReference = DatasetUtils.GetSpatialReference(featureClass);
			Assert.NotNull(spatialReference,
			               "Unable to determine spatial reference for feature class");

			return SpatialReferenceUtils.ToXmlString(spatialReference);
		}

		[CanBeNull]
		public string GetXmlStringFromFeatureClass([NotNull] IWin32Window owner,
		                                           [CanBeNull]
		                                           out ISpatialReference spatialReference)
		{
			Assert.ArgumentNotNull(owner, nameof(owner));

			IList<ConnectionProvider> connectionProviders = GetConnectionProviders();

			if (connectionProviders.Count == 0)
			{
				Dialog.Info("Get From Workspace Dataset",
				            "Please define a Connection Provider first to access the datasets of its workspace.");

				spatialReference = null;
				return null;
			}

			ConnectionProvider selectedProvider =
				FindConnectionProvider(owner, connectionProviders);

			if (selectedProvider == null)
			{
				spatialReference = null;
				return null;
			}

			IList<FeatureClassItem> items = GetFeatureClassItems(selectedProvider).ToList();

			FeatureClassItem selectedItem = FindFeatureClassItem(owner, items);

			if (selectedItem == null)
			{
				spatialReference = null;
				return null;
			}

			spatialReference = DatasetUtils.GetSpatialReference(selectedItem.FeatureClass);

			return spatialReference == null
				       ? null
				       : SpatialReferenceUtils.ToXmlString(spatialReference);
		}

		public void SetXmlString([NotNull] string xmlString)
		{
			Assert.NotNull(GetEntity()).XmlString = xmlString;

			NotifyChanged();
		}

		protected override void IsValidForPersistenceCore(
			SpatialReferenceDescriptor entity,
			Notification notification)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));
			Assert.ArgumentNotNull(notification, nameof(notification));

			if (entity.Name == null)
			{
				return; // already reported by entity
			}

			// check if another entity with the same name exists
			SpatialReferenceDescriptor existing =
				_modelBuilder.SpatialReferenceDescriptors.Get(entity.Name);

			if (existing != null && existing.Id != entity.Id)
			{
				notification.RegisterMessage("Name",
				                             "A spatial reference with the same name already exists",
				                             Severity.Error);
			}
		}

		protected override bool AllowDelete => true;

		#region Non-public

		[NotNull]
		private IList<ConnectionProvider> GetConnectionProviders()
		{
			return _modelBuilder.ReadOnlyTransaction(
				() => _modelBuilder.ConnectionProviders.GetAll());
		}

		private static ConnectionProvider FindConnectionProvider(
			[NotNull] IWin32Window owner, [NotNull] IList<ConnectionProvider> connectionProviders)
		{
			IFinder<ConnectionProvider> finder = new Finder<ConnectionProvider>();

			ConnectionProvider selectedProvider = finder.ShowDialog(
				owner, connectionProviders, false,
				new ColumnDescriptor(nameof(ConnectionProvider.Name), "Connection Provider"),
				new ColumnDescriptor(nameof(ConnectionProvider.TypeDescription), "Type"),
				new ColumnDescriptor(nameof(ConnectionProvider.Description)))?.FirstOrDefault();
			return selectedProvider;
		}

		[NotNull]
		private static IEnumerable<FeatureClassItem> GetFeatureClassItems(
			[NotNull] ConnectionProvider connectionProvider)
		{
			Assert.ArgumentNotNull(connectionProvider, nameof(connectionProvider));

			using (new WaitCursor())
			{
				_msg.Info($"Opening workspace for Connection Provider {connectionProvider.Name}");

				var workspace = connectionProvider.OpenWorkspace();
				Assert.NotNull(workspace, "Workspace could not be opened.");

				using (_msg.IncrementIndentation("Reading feature classes..."))
				{
					foreach (IFeatureClass featureClass in DatasetUtils
					                                       .GetObjectClasses((IWorkspace) workspace)
					                                       .OfType<IFeatureClass>())
					{
						var item = new FeatureClassItem(featureClass);
						_msg.InfoFormat("Reading feature class {0}", item.Name);

						yield return item;
					}
				}
			}
		}

		private static FeatureClassItem FindFeatureClassItem(
			[NotNull] IWin32Window owner, [NotNull] IList<FeatureClassItem> items)
		{
			IFinder<FeatureClassItem> finder2 = new Finder<FeatureClassItem>();

			FeatureClassItem selectedFeatureClass = finder2.ShowDialog(
				owner, items, false,
				new ColumnDescriptor(nameof(FeatureClassItem.Image), string.Empty),
				new ColumnDescriptor(nameof(FeatureClassItem.Name)),
				new ColumnDescriptor(nameof(FeatureClassItem.Dataset)),
				new ColumnDescriptor(nameof(FeatureClassItem.CSName)),
				new ColumnDescriptor(nameof(FeatureClassItem.FactoryCode)))?.FirstOrDefault();
			return selectedFeatureClass;
		}

		#endregion

		private class FeatureClassItem
		{
			public FeatureClassItem([NotNull] IFeatureClass featureClass)
			{
				Assert.ArgumentNotNull(featureClass, nameof(featureClass));

				FeatureClass = featureClass;

				Image = DatasetTypeImageLookup.GetImage(
					(ProSuiteGeometryType) featureClass.ShapeType);
				Image.Tag = DatasetTypeImageLookup.GetDefaultSortIndex(Image);

				Name = DatasetUtils.GetUnqualifiedName(featureClass);

				IFeatureDataset fds = featureClass.FeatureDataset;
				Dataset = fds != null ? DatasetUtils.GetUnqualifiedName(fds) : string.Empty;

				var sref = DatasetUtils.GetSpatialReference(featureClass);
				CSName = sref != null ? sref.Name : string.Empty;
				FactoryCode = sref?.FactoryCode ?? -1;
			}

			[UsedImplicitly]
			public IFeatureClass FeatureClass { get; private set; }

			[NotNull]
			[UsedImplicitly]
			public Image Image { get; private set; }

			[UsedImplicitly]
			[DisplayName(@"Feature Class")]
			public string Name { get; private set; }

			[UsedImplicitly]
			[DisplayName(@"Feature Dataset")]
			public string Dataset { get; private set; }

			[UsedImplicitly]
			[DisplayName(@"Coordinate System")]
			public string CSName { get; private set; }

			[UsedImplicitly]
			[DisplayName(@"ID")]
			public int FactoryCode { get; private set; }
		}
	}
}
