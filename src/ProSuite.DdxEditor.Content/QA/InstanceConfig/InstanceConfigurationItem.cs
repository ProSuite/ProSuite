using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Essentials.System;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.Commons.Validation;
using ProSuite.DdxEditor.Content.QA.InstanceDescriptors;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Dependencies;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;
using ProSuite.UI.QA.ResourceLookup;

namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
{
	public class InstanceConfigurationItem
		: EntityItem<InstanceConfiguration, InstanceConfiguration>
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[CanBeNull] private readonly IInstanceConfigurationContainerItem _containerItem;
		[NotNull] private readonly TableState _tableState = new TableState();

		[CanBeNull] private Image _image;
		[CanBeNull] private string _imageKey;

		private ICommand _webHelpCommand;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceConfigurationItem"/> class.
		/// </summary>
		/// <param name="modelBuilder">The model builder.</param>
		/// <param name="instanceConfiguration">The instance configuration.</param>
		/// <param name="containerItem">The container item</param>
		/// <param name="repository">The repository.</param>
		public InstanceConfigurationItem(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
			[NotNull] InstanceConfiguration instanceConfiguration,
			[CanBeNull] IInstanceConfigurationContainerItem containerItem,
			[NotNull] IRepository<InstanceConfiguration> repository)
			: base(instanceConfiguration, repository)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			ModelBuilder = modelBuilder;
			_containerItem = containerItem;

			UpdateImage(instanceConfiguration);
		}

		#endregion

		[NotNull]
		private CoreDomainModelItemModelBuilder ModelBuilder { get; }

		public override Image Image => _image;

		public override string ImageKey => _imageKey;

		public override IList<DependingItem> GetDependingItems()
		{
			InstanceConfiguration rootEntity = GetEntity();

			IEnumerable<InstanceConfiguration> dependent = GetReferencingConfigurations(rootEntity);

			return ModelBuilder.CreateDependingItems(dependent).ToList();
		}

		[CanBeNull]
		public InstanceDescriptor GetInstanceDescriptor()
		{
			return Assert.NotNull(GetEntity()).InstanceDescriptor;
		}

		[CanBeNull]
		public string GetInstanceDescription()
		{
			InstanceConfiguration instanceConfiguration = Assert.NotNull(GetEntity());
			var factory = CreateInstanceFactory(instanceConfiguration);

			return factory?.TestDescription;
		}

		[CanBeNull]
		public IList<TestParameter> GetParameterDescription()
		{
			InstanceConfiguration instanceConfiguration = Assert.NotNull(GetEntity());
			InstanceFactory factory = CreateInstanceFactory(instanceConfiguration);

			if (factory == null)
			{
				return null;
			}

			IList<TestParameter> paramList = factory.Parameters;

			foreach (TestParameter param in paramList)
			{
				param.Description = factory.GetParameterDescription(param.Name);
			}

			return paramList;
		}

		[NotNull]
		public IList<InstanceDescriptorTableRow> GetInstanceDescriptorTableRows()
		{
			var list = new List<InstanceDescriptorTableRow>();

			ModelBuilder.ReadOnlyTransaction(delegate { PopulateInstanceDescriptorList(list); });

			return list;
		}

		public bool CanCreateCopy => _containerItem != null;

		public void CreateCopy()
		{
			Assert.NotNull(_containerItem, "no container, cannot create copy")
			      .CreateCopy(this);
		}

		[NotNull]
		public IEnumerable<InstanceConfiguration> GetReferencingInstances()
		{
			InstanceConfiguration instanceConfig = Assert.NotNull(GetEntity());

			return GetReferencingConfigurations(instanceConfig);
		}

		private IEnumerable<InstanceConfiguration> GetReferencingConfigurations(
			InstanceConfiguration instanceConfig)
		{
			if (instanceConfig is TransformerConfiguration transformerConfiguration)
			{
				// Transformer configs can be referenced by any dataset parameter  (through ValueSource):
				return ModelBuilder.InstanceConfigurations.GetReferencingConfigurations(
					transformerConfiguration);
			}

			if (instanceConfig is IssueFilterConfiguration issueFilterConfiguration)
			{
				// Issue filters are only referenced by conditions:
				return ModelBuilder.QualityConditions.GetReferencingConditions(
					issueFilterConfiguration);
			}

			throw new NotImplementedException();
		}

		public void OpenUrl()
		{
			InstanceConfiguration configuration = ModelBuilder.ReadOnlyTransaction(GetEntity);

			if (configuration == null)
			{
				return;
			}

			string url = configuration.Url;

			if (StringUtils.IsNullOrEmptyOrBlank(url))
			{
				_msg.Info("No Url defined");

				return;
			}

			_msg.InfoFormat("Opening url {0}...", url);

			ProcessUtils.StartProcess(url);
		}

		private void PopulateInstanceDescriptorList(List<InstanceDescriptorTableRow> list)
		{
			InstanceConfigurationsItem configurationsItem = (InstanceConfigurationsItem) Parent;

			Assert.NotNull(configurationsItem);

			list.AddRange(configurationsItem.GetInstanceDescriptorTableRows());
		}

		private void UpdateImage([CanBeNull] InstanceConfiguration instanceConfiguration)
		{
			if (instanceConfiguration == null)
			{
				// don't change
			}
			else
			{
				_image = TestTypeImageLookup.GetImage(instanceConfiguration);
				_image.Tag = TestTypeImageLookup.GetDefaultSortIndex(instanceConfiguration);

				_imageKey = string.Format("{0}#{1}", base.ImageKey,
				                          TestTypeImageLookup.GetImageKey(
					                          instanceConfiguration));
			}
		}

		protected override string GetText(InstanceConfiguration entity)
		{
			string suffix = entity.InstanceDescriptor == null
				                ? string.Empty
				                : string.Format(" ({0})", entity.InstanceDescriptor.Name);

			return $"{entity.Name}{suffix}";
		}

		protected override void CollectCommands(List<ICommand> commands,
		                                        IApplicationController
			                                        applicationController)
		{
			base.CollectCommands(commands, applicationController);

			if (_containerItem != null)
			{
				commands.Add(new CopyInstanceConfigurationCommand(this, applicationController));
				commands.Add(new AssignInstanceConfigurationToCategoryCommand(new[] {this},
					             _containerItem, applicationController));

				_webHelpCommand = new ShowInstanceWebHelpCommand<InstanceConfigurationItem>(
					this, applicationController);

				commands.Add(_webHelpCommand);
			}
		}

		protected override bool AllowDelete => _containerItem != null;

		protected override void UpdateItemStateCore(InstanceConfiguration entity)
		{
			base.UpdateItemStateCore(entity);

			UpdateImage(entity);
		}

		protected override Control CreateControlCore(IItemNavigation itemNavigation)
		{
			return InstanceConfigurationControlFactory.CreateControl(
				this, itemNavigation, ModelBuilder, _tableState);
		}

		protected override void IsValidForPersistenceCore(InstanceConfiguration entity,
		                                                  Notification notification)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));
			Assert.ArgumentNotNull(notification, nameof(notification));

			if (entity.Name == null)
			{
				return; // already reported by entity
			}

			foreach (TestParameterValue value in entity.ParameterValues)
			{
				var dsValue = value as DatasetTestParameterValue;
				if (dsValue == null)
				{
					continue;
				}

				if (dsValue.DatasetValue == null && dsValue.ValueSource == null)
				{
					if (IsParameterOptional(entity, dsValue))
					{
						continue;
					}

					notification.RegisterMessage(dsValue.TestParameterName,
					                             "Dataset not set",
					                             Severity.Error);
				}
			}

			// check if another entity with the same name exists
			// TODO: Also allow duplicate names across types in mapping!
			Type targetType = entity.GetType();
			InstanceConfiguration existing =
				ModelBuilder.InstanceConfigurations.Get(entity.Name, targetType);

			if (existing != null && existing.Id != entity.Id)
			{
				notification.RegisterMessage("Name",
				                             $"A {targetType.Name} with the same name already exists",
				                             Severity.Error);
			}
		}

		private static bool IsParameterOptional(
			[NotNull] InstanceConfiguration entity,
			[NotNull] TestParameterValue parameterValue)
		{
			InstanceFactory factory = CreateInstanceFactory(entity);

			if (factory == null)
			{
				return false;
			}

			foreach (TestParameter testParameter in factory.Parameters)
			{
				if (testParameter.Name == parameterValue.TestParameterName)
				{
					// parameter found; it is optional if not a constructor parameter
					return ! testParameter.IsConstructorParameter;
				}
			}

			return false;
		}

		private static InstanceFactory CreateInstanceFactory(InstanceConfiguration entity)
		{
			InstanceFactory factory = InstanceFactoryUtils.CreateFactory(entity);

			return factory;
		}

		public void ExecuteWebHelpCommand()
		{
			_webHelpCommand?.Execute();
		}
	}
}
