using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Finder;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.Commons.Validation;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework.Dependencies;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.Geodatabase;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;

namespace ProSuite.DdxEditor.Content.Models
{
	public abstract class ModelItemBase<E> : SubclassedEntityItem<E, Model>
		where E : Model
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ModelItemBaseBase{E}"/> class.
		/// </summary>
		/// <param name="modelBuilder">The model builder.</param>
		/// <param name="model">The model.</param>
		/// <param name="repository">The repository.</param>
		protected ModelItemBase([NotNull] CoreDomainModelItemModelBuilder modelBuilder,
		                        [NotNull] E model,
		                        [NotNull] IRepository<Model> repository)
			: base(model, repository)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
		}

		#endregion

		public override Image Image => Resources.ModelItem;

		public override IList<DependingItem> GetDependingItems()
		{
			return _modelBuilder.GetDependingItems(GetEntity());
		}

		public ConnectionProvider FindUserConnectionProvider(IWin32Window owner)
		{
			return FindConnectionProviderCore<ConnectionProvider>(
				owner,
				new ColumnDescriptor("Name"),
				new ColumnDescriptor("TypeDescription"),
				new ColumnDescriptor("Description"));
		}

		public SpatialReferenceDescriptor FindSpatialReferenceDescriptor(
			IWin32Window owner)
		{
			Assert.ArgumentNotNull(owner, nameof(owner));

			IList<SpatialReferenceDescriptor> all = null;

			_modelBuilder.ReadOnlyTransaction(
				delegate { all = _modelBuilder.SpatialReferenceDescriptors.GetAll(); });

			var finder = new Finder<SpatialReferenceDescriptor>();

			return finder.ShowDialog(owner, all,
			                         new ColumnDescriptor("Name"),
			                         new ColumnDescriptor("Description"));
		}

		public ConnectionProvider FindSdeRepositoryOwnerConnectionProvider(
			IWin32Window owner)
		{
			return FindConnectionProviderCore<SdeDirectDbUserConnectionProvider>(
				owner,
				new ColumnDescriptor("Name"),
				new ColumnDescriptor("TypeDescription"),
				new ColumnDescriptor("Description"));
		}

		public ConnectionProvider FindSchemaOwnerConnectionProvider(
			IWin32Window owner)
		{
			return FindConnectionProviderCore<ConnectionProvider>(
				owner,
				c => c is IOpenSdeWorkspace,
				new ColumnDescriptor("Name"),
				new ColumnDescriptor("TypeDescription"),
				new ColumnDescriptor("Description"));
		}

		public bool CanRefreshModelContent(out string message)
		{
			E model = null;
			_modelBuilder.ReadOnlyTransaction(delegate { model = GetEntity(); });

			if (model.DatasetListBuilderFactoryClassDescriptor == null)
			{
				message = "Dataset List Builder not assigned";
				return false;
			}

			message = string.Empty;
			return true;
		}

		public void RefreshModelContent()
		{
			E model = Assert.NotNull(GetEntity());

			RefreshCore(model,
			            _modelBuilder.AttributeTypes,
			            _modelBuilder.Resolve<IGeometryTypeRepository>());

			RefreshChildren();
		}

		public object FindAttributeConfiguratorFactory(IWin32Window owner)
		{
			return _modelBuilder.ReadOnlyTransaction(
				delegate
				{
					ClassDescriptor currentValue =
						Assert.NotNull(GetEntity()).AttributeConfiguratorFactoryClassDescriptor;

					return FindClassDescriptor<IAttributeConfiguratorFactory>(owner, currentValue);
				});
		}

		public object FindDatasetListBuilderFactory(IWin32Window owner)
		{
			return _modelBuilder.ReadOnlyTransaction(
				delegate
				{
					ClassDescriptor currentValue =
						Assert.NotNull(GetEntity()).DatasetListBuilderFactoryClassDescriptor;

					return FindClassDescriptor<IDatasetListBuilderFactory>(owner, currentValue);
				});
		}

		#region Non-public

		protected override bool AllowDelete => true;

		protected override IEnumerable<Item> GetChildren()
		{
			return _modelBuilder.GetChildren(this);
		}

		protected override void AttachPresenter(
			ICompositeEntityControl<E, IViewObserver> control)
		{
			// if needed, override and use specific subclass
			new ModelPresenter<E>(this, control);
		}

		protected override void AddEntityPanels(
			ICompositeEntityControl<E, IViewObserver> compositeControl)
		{
			var view = new ModelControl<E>();

			new ModelControlPresenter<E>(this, view);

			compositeControl.AddPanel(view);
		}

		protected override void IsValidForPersistenceCore(E entity,
		                                                  Notification notification)
		{
			base.IsValidForPersistenceCore(entity, notification);

			if (entity.Name != null)
			{
				Model other = _modelBuilder.Models.Get(entity.Name);

				if (other != null && other.Id != entity.Id)
				{
					notification.RegisterMessage("Name",
					                             "Another model with the same name already exists",
					                             Severity.Error);
				}
			}
		}

		private T FindConnectionProviderCore<T>(IWin32Window owner,
		                                        params ColumnDescriptor[] columns)
			where T : ConnectionProvider
		{
			return FindConnectionProviderCore<T>(owner, null, columns);
		}

		private T FindConnectionProviderCore<T>(IWin32Window owner,
		                                        [CanBeNull] Predicate<T> match,
		                                        params ColumnDescriptor[] columns)
			where T : ConnectionProvider
		{
			Assert.ArgumentNotNull(owner, nameof(owner));

			IList<T> all = null;

			_modelBuilder.ReadOnlyTransaction(
				delegate
				{
					all = _modelBuilder.ConnectionProviders
					                   .GetAll<T>()
					                   .Where(c => match == null || match(c))
					                   .ToList();
				});

			IFinder<T> finder = new Finder<T>();

			return finder.ShowDialog(owner, all, columns);
		}

		private void RefreshCore(
			[NotNull] E model,
			[NotNull] IRepository<AttributeType> attributeTypeRepository,
			[NotNull] IRepository<GeometryType> geometryTypeRepository)
		{
			Assert.ArgumentNotNull(model, nameof(model));
			Assert.ArgumentNotNull(attributeTypeRepository, nameof(attributeTypeRepository));
			Assert.ArgumentNotNull(geometryTypeRepository, nameof(geometryTypeRepository));

			_modelBuilder.NewTransaction(
				delegate
				{
					IEnumerable<ObjectAttributeType> attributeTypes =
						model.Harvest(geometryTypeRepository.GetAll(),
						              attributeTypeRepository.GetAll());

					foreach (ObjectAttributeType attributeType in attributeTypes)
					{
						if (! attributeType.IsPersistent)
						{
							attributeTypeRepository.Save(attributeType);
						}
					}
				});
		}

		[CanBeNull]
		private static ClassDescriptor FindClassDescriptor<T>(
			[CanBeNull] IWin32Window owner,
			[CanBeNull] ClassDescriptor orig)
		{
			Type origType = null;
			if (orig != null)
			{
				try
				{
					origType = orig.GetInstanceType();
				}
				catch (Exception e)
				{
					_msg.WarnFormat("Unable to access '{0}': {1}", orig, e.Message);
				}
			}

			IList<Type> types = TypeFinder.ShowDialog<T>(owner, false, origType);
			if (types == null || types.Count == 0)
			{
				return null;
			}

			Assert.True(types.Count == 1, "Cannot handle a list of {0} types", types.Count);

			return new ClassDescriptor(types[0]);
		}

		public void AssignLayerFiles([NotNull] string folderPath)
		{
			Assert.ArgumentNotNull(folderPath, nameof(folderPath));
			Assert.ArgumentCondition(Directory.Exists(folderPath),
			                         "Directory does not exist: {0}", folderPath);

			var assignCount = 0;
			var totalCount = 0;
			using (_msg.IncrementIndentation(
				       "Searching for layer files that match datasets with missing layer files"))
			{
				_modelBuilder.UseTransaction(
					delegate
					{
						E model = Assert.NotNull(GetEntity());

						foreach (Dataset dataset in model.GetDatasets())
						{
							totalCount++;

							var spatialDataset = dataset as ISpatialDataset;
							if (spatialDataset == null)
							{
								continue;
							}

							if (spatialDataset.DefaultLayerFile != null &&
							    ! string.IsNullOrEmpty(spatialDataset.DefaultLayerFile.FileName))
							{
								continue;
							}

							string layerFile = GetLayerFile(folderPath, spatialDataset);

							if (layerFile == null)
							{
								continue;
							}

							_msg.InfoFormat("Assigning layer file to dataset {0}: {1}",
							                spatialDataset.Name, layerFile);

							spatialDataset.DefaultLayerFile = new LayerFile(layerFile);
							assignCount++;
						}
					});
			}

			_msg.InfoFormat("Layer files assigned to {0} of {1} dataset{2}",
			                assignCount, totalCount, totalCount == 1
				                                         ? string.Empty
				                                         : "s");
		}

		[CanBeNull]
		private static string GetLayerFile([NotNull] string folderPath,
		                                   [NotNull] ISpatialDataset spatialDataset)
		{
			var candidates = new[]
			                 {
				                 spatialDataset.Name,
				                 spatialDataset.UnqualifiedName
			                 };

			foreach (string candidate in candidates)
			{
				string layerFile = string.Format("{0}.lyr", candidate);
				string layerFileFullPath = Path.Combine(folderPath, layerFile);

				if (File.Exists(layerFileFullPath))
				{
					return layerFileFullPath;
				}
			}

			return null;
		}

		#endregion
	}
}
