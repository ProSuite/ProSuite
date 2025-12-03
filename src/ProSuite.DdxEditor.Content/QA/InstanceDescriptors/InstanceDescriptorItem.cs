using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Finder;
using ProSuite.Commons.Validation;
using ProSuite.DdxEditor.Content.QA.Categories;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Dependencies;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.UI.Core.QA.ResourceLookup;

namespace ProSuite.DdxEditor.Content.QA.InstanceDescriptors
{
	public class InstanceDescriptorItem : EntityItem<InstanceDescriptor, InstanceDescriptor>
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;
		[CanBeNull] private Image _image;
		[CanBeNull] private string _imageKey;
		[NotNull] private readonly TableState _tableState = new TableState();

		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceDescriptorItem"/> class.
		/// </summary>
		/// <param name="modelBuilder">The model builder.</param>
		/// <param name="descriptor">The test descriptor.</param>
		/// <param name="repository">The repository.</param>
		public InstanceDescriptorItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder,
		                              [NotNull] InstanceDescriptor descriptor,
		                              [NotNull] IInstanceDescriptorRepository repository)
			: base(descriptor, repository)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
			UpdateImage(descriptor);
		}

		public override Image Image => _image;

		public override string ImageKey => _imageKey;

		public string TypeName
		{
			get
			{
				if (GetEntity() is TransformerDescriptor)
				{
					return "Transformer";
				}

				if (GetEntity() is IssueFilterDescriptor)
				{
					return "Issue Filter";
				}

				throw new InvalidOperationException("Unknown descriptor type");
			}
		}

		private void UpdateImage([CanBeNull] InstanceDescriptor instanceDescriptor)
		{
			_image = TestTypeImageLookup.GetImage(instanceDescriptor);

			if (instanceDescriptor != null)
			{
				_image.Tag = TestTypeImageLookup.GetDefaultSortIndex(instanceDescriptor);
			}

			_imageKey = $"{base.ImageKey}#{TestTypeImageLookup.GetImageKey(instanceDescriptor)}";
		}

		protected override void UpdateItemStateCore(InstanceDescriptor entity)
		{
			base.UpdateItemStateCore(entity);

			UpdateImage(entity);
		}

		protected override void IsValidForPersistenceCore(InstanceDescriptor entity,
		                                                  Notification notification)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));
			Assert.ArgumentNotNull(notification, nameof(notification));

			// check if another instance descriptor with the same name exists
			if (entity.Name != null)
			{
				var descriptorWithSameName =
					_modelBuilder.InstanceDescriptors.Get(entity.Name);

				InstanceDescriptorItemUtils.ValidateDescriptorAgainstDuplicateName(
					entity, descriptorWithSameName, notification);
			}

			// check if another instance descriptor with the implementation exists
			InstanceDescriptor descriptorWithSameImplementation =
				_modelBuilder.InstanceDescriptors.GetWithSameImplementation(entity);

			InstanceDescriptorItemUtils.ValidateDescriptorAgainstDuplicateImplementation(
				entity, descriptorWithSameImplementation, notification);
		}

		protected override Control CreateControlCore(IItemNavigation itemNavigation)
		{
			var control = new InstanceDescriptorControl(_tableState);

			new InstanceDescriptorPresenter(this, control,
			                                FindTestClassProvider, itemNavigation);
			return control;
		}

		[CanBeNull]
		private ClassDescriptor FindTestClassProvider(
			[NotNull] IWin32Window owner,
			[CanBeNull] ClassDescriptor orig)
		{
			InstanceDescriptor descriptor = Assert.NotNull(GetEntity());

			return descriptor is TransformerDescriptor
				       ? FindClassDescriptor<ITableTransformer>(owner, orig)
				       : FindClassDescriptor<IIssueFilter>(owner, orig);
		}

		[CanBeNull]
		private static ClassDescriptor FindClassDescriptor<T>(
			[NotNull] IWin32Window owner,
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
					_msg.Debug(e.Message);
				}
			}

			const bool allowMultiSelection = false;
			IList<Type> types = TypeFinder.ShowDialog<T>(
				owner, allowMultiSelection, origType, TypePredicate);

			if (types == null || types.Count == 0)
			{
				return null;
			}

			Assert.True(types.Count == 1, "Cannot handle a list of {0} types",
			            types.Count);

			return new ClassDescriptor(types[0]);
		}

		public bool CanFindCategory => _modelBuilder.DataQualityCategories != null;

		public bool FindCategory(IWin32Window window,
		                         [CanBeNull] out DataQualityCategory category)
		{
			IDataQualityCategoryRepository repository =
				Assert.NotNull(_modelBuilder.DataQualityCategories, "repository is null");

			IList<DataQualityCategory> categories =
				_modelBuilder.ReadOnlyTransaction(() => repository.GetAll());

			DataQualityCategoryTableRow tableRow =
				DataQualityCategoryUtils.SelectCategory(categories,
				                                        window,
				                                        allowNoCategorySelection: false);

			if (tableRow == null)
			{
				category = null;
				return false;
			}

			category = tableRow.DataQualityCategory;
			return true;
		}

		private static bool TypePredicate([NotNull] Type type)
		{
			return type.IsPublic &&
			       ! type.IsAbstract &&
			       ! InstanceUtils.IsInternallyUsed(type);
		}

		[CanBeNull]
		private IInstanceInfo GetInstanceInfo()
		{
			return InstanceDescriptorUtils.GetInstanceInfo(Assert.NotNull(GetEntity()));
		}

		[NotNull]
		public IList<TestParameter> GetTestParameters([CanBeNull] out string description,
		                                              [NotNull] out string[] categories)
		{
			IInstanceInfo instanceInfo = GetInstanceInfo();

			if (instanceInfo == null)
			{
				description = string.Empty;
				categories = Array.Empty<string>();
				return new List<TestParameter>();
			}

			description = instanceInfo.TestDescription;
			categories = instanceInfo.TestCategories;

			IList<TestParameter> testParameters = instanceInfo.Parameters;

			foreach (TestParameter testParameter in testParameters)
			{
				// TODO revise handling of parameter descriptions
				testParameter.Description =
					instanceInfo.GetParameterDescription(testParameter.Name);
			}

			return testParameters;
		}

		public override IList<DependingItem> GetDependingItems()
		{
			return _modelBuilder.GetDependingItems(GetEntity());
		}

		protected override void CollectCommands(
			List<ICommand> commands,
			IApplicationController applicationController)
		{
			base.CollectCommands(commands, applicationController);

			commands.Add(new CreateInstanceConfigurationCommand(this, applicationController));
		}

		protected override bool AllowDelete => true;

		public InstanceConfiguration CreateConfiguration()
		{
			InstanceDescriptor descriptor = Assert.NotNull(GetEntity());

			InstanceConfiguration configuration = descriptor.CreateConfiguration();

			_modelBuilder.ReadOnlyTransaction(
				delegate { configuration.InstanceDescriptor = GetEntity(); });

			return configuration;
		}

		public IEnumerable<InstanceConfiguration> GetInstanceConfigurations()
		{
			InstanceDescriptor instanceDescriptor = Assert.NotNull(GetEntity());

			IInstanceConfigurationRepository repository = _modelBuilder.InstanceConfigurations;

			return repository.Get(instanceDescriptor);
		}
	}
}
