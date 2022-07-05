using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.Commons.UI.Finder;
using ProSuite.Commons.Validation;
using ProSuite.DdxEditor.Content.QA.Categories;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DdxEditor.Content.QA.QSpec;
using ProSuite.DdxEditor.Content.QA.TestDescriptors;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Dependencies;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;
using ProSuite.UI.QA;
using ProSuite.UI.QA.ResourceLookup;

namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
{
	public class InstanceConfigurationItem : EntityItem<InstanceConfiguration, InstanceConfiguration>
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		// TODO: Separate interface
		[CanBeNull] private readonly IQualityConditionContainerItem _containerItem;
		[NotNull] private readonly TableState _tableState = new TableState();

		[CanBeNull] private Image _image;
		[CanBeNull] private string _imageKey;

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
			[CanBeNull] IQualityConditionContainerItem containerItem,
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
		protected CoreDomainModelItemModelBuilder ModelBuilder { get; }

		public override Image Image => _image;

		public override string ImageKey => _imageKey;

		public override IList<DependingItem> GetDependingItems()
		{
			return ModelBuilder.GetDependingItems(GetEntity());
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

		public void ExportQualityCondition([NotNull] string exportFileName)
		{
			ModelBuilder.ReadOnlyTransaction(
				() => ExportQualityConditionCore(exportFileName));
		}

		public void ImportQualityCondition([NotNull] string importFileName)
		{
			// TODO only if there are no pending changes!!
			ModelBuilder.UseTransaction(
				() => ImportQualityConditionCore(importFileName));
		}

		[NotNull]
		public IList<InstanceDescriptorTableRow> GetInstanceDescriptorTableRows()
		{
			var list = new List<InstanceDescriptorTableRow>();

			ModelBuilder.ReadOnlyTransaction(delegate { PopulateInstanceDescriptorList(list); });

			return list;
		}

		protected void PopulateInstanceDescriptorList(List<InstanceDescriptorTableRow> list)
		{
			// TODO: Subclasses!

			IDictionary<int, int> refCountMap =
				ModelBuilder.InstanceDescriptors
							.GetReferencingConfigurationCount<TransformerConfiguration>();

			foreach (InstanceDescriptor descriptor in ModelBuilder
													  .InstanceDescriptors.GetTransformerDescriptors())
			{
				int refCount;
				if (!refCountMap.TryGetValue(descriptor.Id, out refCount))
				{
					refCount = 0;
				}

				list.Add(new InstanceDescriptorTableRow(descriptor, refCount));
			}
		}

		public bool CanCreateCopy => _containerItem != null;

		public void CreateCopy()
		{
			//Assert.NotNull(_containerItem, "no container, cannot create copy")
			//      .CreateCopy(this);
		}

		[NotNull]
		public IEnumerable<KeyValuePair<QualitySpecification, QualitySpecificationElement>>
			GetQualitySpecificationReferences()
		{
			yield break;

			//var qualityCondition = Assert.NotNull(GetEntity());

			//IList<QualitySpecification> qualitySpecifications =
			//	_modelBuilder.Resolve<IQualitySpecificationRepository>().Get(
			//		qualityCondition);

			//foreach (QualitySpecification specification in qualitySpecifications)
			//{
			//	QualitySpecificationElement element = specification.GetElement(qualityCondition);

			//	Assert.NotNull(element,
			//	               "Element for {0} not found in referencing quality specification {1}",
			//	               qualityCondition.Name, specification.Name);

			//	yield return
			//		new KeyValuePair<QualitySpecification, QualitySpecificationElement>(
			//			specification, element);
			//}
		}

		[CanBeNull]
		public IList<QualitySpecificationTableRow> GetQualitySpecificationsToReference(
			[NotNull] InstanceConfiguration instanceConfig,
			[NotNull] IWin32Window owner)
		{
			Assert.ArgumentNotNull(instanceConfig, nameof(instanceConfig));

			return new List<QualitySpecificationTableRow>();

			//DdxModel model = DataQualityCategoryUtils.GetDefaultModel(
			//	instanceConfig.Category);

			//var queries = new List<FinderQuery<QualitySpecificationTableRow>>
			//              {
			//	              new FinderQuery<QualitySpecificationTableRow>(
			//		              "<All>", "[all]",
			//		              () =>
			//			              QSpec.TableRows.GetQualitySpecifications(
			//				              ModelBuilder,
			//				              instanceConfig))
			//              };

			//if (model != null)
			//{
			//	queries.Add(new FinderQuery<QualitySpecificationTableRow>(
			//		            $"Quality specifications involving datasets in {model.Name}",
			//		            $"model{model.Id}",
			//		            () => QSpec.TableRows.GetQualitySpecifications(
			//			            ModelBuilder,
			//			            instanceConfig,
			//			            model)));
			//}

			//var finder = new Finder<QualitySpecificationTableRow>();

			//return finder.ShowDialog(
			//	owner, queries,
			//	filterSettingsContext: FinderContextIds.GetId(instanceConfig.Category),
			//	allowMultiSelection: true);
		}

		public void OpenUrl()
		{
			InstanceConfiguration configuration = ModelBuilder.ReadOnlyTransaction(GetEntity);

			if (configuration == null)
			{
				return;
			}

			//string url = configuration.Url;
			string url = null;

			if (StringUtils.IsNullOrEmptyOrBlank(url))
			{
				_msg.Info("No Url defined");

				return;
			}

			_msg.InfoFormat("Opening url {0}...", url);

			Process.Start(url);
		}

		public void AssignNewVersionUuid()
		{
			//QualityCondition qualityCondition =
			//	_modelBuilder.ReadOnlyTransaction(GetEntity);

			//Assert.NotNull(qualityCondition, "Quality condition no longer exists");

			//qualityCondition.AssignNewVersionUuid();

			//NotifyChanged();
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

		/// <summary>
		/// Exports the quality condition to a specified file name
		/// </summary>
		/// <param name="exportFileName">Name of the export file.</param>
		/// <remarks>Expects to be called within a transaction</remarks>
		private void ExportQualityConditionCore([NotNull] string exportFileName)
		{
			//Assert.ArgumentNotNullOrEmpty(exportFileName, nameof(exportFileName));

			//InstanceConfiguration qualityCondition = Assert.NotNull(GetEntity());

			//using (TextWriter file = new StreamWriter(exportFileName))
			//{
			//	try
			//	{
			//		TestFactory factory = Assert.NotNull(
			//			TestFactoryUtils.CreateTestFactory(qualityCondition),
			//			"Cannot create test factory");

			//		string data = factory.Export(qualityCondition);

			//		file.Write(data);
			//	}
			//	finally
			//	{
			//		file.Close();
			//	}
			//}

			//_msg.Info(string.Format(
			//	          "Exported parameters of quality condition '{0}' to {1}",
			//	          qualityCondition.Name, exportFileName));
		}

		/// <summary>
		/// Imports the quality condition from a given file.
		/// </summary>
		/// <param name="importFileName">Name of the import file.</param>
		/// <remarks>Expects to be called within a transaction</remarks>
		private void ImportQualityConditionCore([NotNull] string importFileName)
		{
			//Assert.ArgumentNotNullOrEmpty(importFileName, nameof(importFileName));

			//InstanceConfiguration qualityCondition = Assert.NotNull(GetEntity());

			//QualityCondition paramValuesQa;
			//using (var file = new StreamReader(importFileName))
			//{
			//	try
			//	{
			//		IList<Dataset> datasets = _modelBuilder.Datasets.GetAll();

			//		TestFactory factory = Assert.NotNull(
			//			TestFactoryUtils.CreateTestFactory(qualityCondition),
			//			"Cannot create test factory");

			//		paramValuesQa = factory.CreateQualityCondition(
			//			file, datasets, qualityCondition.ParameterValues);
			//	}
			//	finally
			//	{
			//		file.Close();
			//	}
			//}

			//Assert.NotNull(paramValuesQa,
			//               "Unable to import quality condition parameters");

			//qualityCondition.ClearParameterValues();

			//foreach (TestParameterValue value in paramValuesQa.ParameterValues)
			//{
			//	qualityCondition.AddParameterValue(value);
			//}

			//_msg.Info(string.Format(
			//	          "Imported parameters of quality condition {0} from {1}",
			//	          qualityCondition.Name, importFileName));
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
				//commands.Add(
				//	new CopyQualityConditionCommand(this, applicationController));
				//commands.Add(new AssignQualityConditionsToCategoryCommand(new[] {this},
				//	             _containerItem,
				//	             applicationController));
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

			// check if another quality condition with the same name exists
			QualityCondition existing = ModelBuilder.QualityConditions.Get(entity.Name);

			if (existing != null && existing.Id != entity.Id)
			{
				notification.RegisterMessage("Name",
											 "A quality condition with the same name already exists",
											 Severity.Error);
			}
		}

		private bool IsParameterOptional(
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
					return !testParameter.IsConstructorParameter;
				}
			}

			return false;
		}

		protected virtual InstanceFactory CreateInstanceFactory(InstanceConfiguration entity)
		{
			// TODO: Subclass
			var transformerConfig = (TransformerConfiguration)entity;
			InstanceFactory factory =
				InstanceFactoryUtils.CreateTransformerFactory(transformerConfig);
			return factory;
		}
	}
}
