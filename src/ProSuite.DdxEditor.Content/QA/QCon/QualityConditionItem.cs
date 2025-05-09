using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Essentials.System;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Text;
using ProSuite.Commons.UI.Finder;
using ProSuite.Commons.Validation;
using ProSuite.DdxEditor.Content.QA.Categories;
using ProSuite.DdxEditor.Content.QA.InstanceConfig;
using ProSuite.DdxEditor.Content.QA.QSpec;
using ProSuite.DdxEditor.Content.QA.TestDescriptors;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Dependencies;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.AO.QA.TestReport;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;
using ProSuite.UI.QA;
using ProSuite.UI.QA.PropertyEditors;
using ProSuite.UI.QA.ResourceLookup;
using Notification = ProSuite.Commons.Validation.Notification;

namespace ProSuite.DdxEditor.Content.QA.QCon
{
	public class QualityConditionItem : EntityItem<QualityCondition, QualityCondition>
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;
		[CanBeNull] private readonly IInstanceConfigurationContainerItem _containerItem;
		[NotNull] private readonly TableState _tableStateQSpec = new TableState();
		[NotNull] private readonly TableState _tableStateIssueFilter = new TableState();

		[CanBeNull] private Image _image;
		[CanBeNull] private string _imageKey;

		private ICommand _webHelpCommand;
		private bool _newCreated;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="QualityConditionItem"/> class.
		/// </summary>
		/// <param name="modelBuilder">The model builder.</param>
		/// <param name="qualityCondition">The qualityCondition.</param>
		/// <param name="containerItem">The container item</param>
		/// <param name="repository">The repository.</param>
		public QualityConditionItem(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
			[NotNull] QualityCondition qualityCondition,
			[CanBeNull] IInstanceConfigurationContainerItem containerItem,
			[NotNull] IRepository<QualityCondition> repository)
			: base(qualityCondition, repository)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
			_containerItem = containerItem;

			UpdateImage(qualityCondition);

			if (! modelBuilder.SupportsTransformersAndFilters)
			{
				HideIssueFilters = true;
			}
		}

		#endregion

		public bool HideIssueFilters { get; set; }

		public override Image Image => _image;

		public override string ImageKey => _imageKey;

		public override IList<DependingItem> GetDependingItems()
		{
			return _modelBuilder.GetDependingItems(GetEntity());
		}

		[CanBeNull]
		public TestDescriptor GetTestDescriptor()
		{
			return Assert.NotNull(GetEntity()).TestDescriptor;
		}

		[CanBeNull]
		public string GetTestDescription()
		{
			QualityCondition qualityCondition = Assert.NotNull(GetEntity());
			var instanceInfo = GetInstanceInfo(qualityCondition);

			return instanceInfo?.TestDescription;
		}

		[CanBeNull]
		public ITestConfigurator GetConfigurator()
		{
			QualityCondition qualityCondition = Assert.NotNull(GetEntity());
			TestDescriptor testDescriptor = qualityCondition.TestDescriptor;
			if (testDescriptor == null)
			{
				return null;
			}

			//TODO: get ITestConfigurator from :
			// - qualityCondition.TestDescriptor.TestConfiguratorDescriptor
			// - adapt TestDescriptor
			// - adapt Admin GUI

			ITestConfigurator result = testDescriptor.TestConfigurator
			                                         ?.CreateInstance<ITestConfigurator>();

			if (result == null)
			{
				TestFactory factory = TestFactoryUtils.CreateTestFactory(qualityCondition);

				if (factory == null)
				{
					return null;
				}

				result = DefaultTestConfiguratorFactory.Create(
					factory, testDescriptor.AssemblyName, readOnly: false);
			}

			result.DatasetProvider = _modelBuilder.GetTestParameterDatasetProvider();
			result.QualityCondition = qualityCondition;

			return result;
		}

		[CanBeNull]
		public IList<TestParameter> GetParameterDescription()
		{
			QualityCondition qualityCondition = Assert.NotNull(GetEntity());
			var instanceInfo = GetInstanceInfo(qualityCondition);

			if (instanceInfo == null)
			{
				return null;
			}

			IList<TestParameter> paramList = instanceInfo.Parameters;

			foreach (TestParameter param in paramList)
			{
				param.Description = instanceInfo.GetParameterDescription(param.Name);
			}

			return paramList;
		}

		public void ExportQualityCondition([NotNull] string exportFileName)
		{
			_modelBuilder.ReadOnlyTransaction(
				() => ExportQualityConditionCore(exportFileName));
		}

		public void ImportQualityCondition([NotNull] string importFileName)
		{
			// TODO only if there are no pending changes!!
			_modelBuilder.UseTransaction(
				() => ImportQualityConditionCore(importFileName));
		}

		[NotNull]
		public IList<TestDescriptorTableRow> GetTestDescriptorTableRows()
		{
			var list = new List<TestDescriptorTableRow>();

			_modelBuilder.ReadOnlyTransaction(
				delegate
				{
					IDictionary<int, int> refCountMap =
						_modelBuilder.TestDescriptors.GetReferencingQualityConditionCount();

					foreach (TestDescriptor descriptor in _modelBuilder.TestDescriptors.GetAll())
					{
						if (! refCountMap.TryGetValue(descriptor.Id, out int refCount))
						{
							refCount = 0;
						}

						list.Add(new TestDescriptorTableRow(descriptor, refCount));
					}
				});

			return list;
		}

		public bool CanCreateCopy => _containerItem != null;

		public void CreateCopy()
		{
			Assert.NotNull(_containerItem, "no container, cannot create copy")
			      .CreateCopy(this);
		}

		[NotNull]
		public IDictionary<QualitySpecification, QualitySpecificationElement>
			GetQualitySpecificationReferences()
		{
			QualityCondition qualityCondition = Assert.NotNull(GetEntity());

			var result = new Dictionary<QualitySpecification, QualitySpecificationElement>();

			_modelBuilder.ReadOnlyTransaction(
				delegate
				{
					IList<QualitySpecification> qualitySpecifications =
						_modelBuilder.QualitySpecifications.Get(qualityCondition);

					foreach (QualitySpecification specification in qualitySpecifications)
					{
						QualitySpecificationElement element =
							specification.GetElement(qualityCondition);

						Assert.NotNull(element,
						               "Element for {0} not found in referencing quality specification {1}",
						               qualityCondition.Name, specification.Name);

						result.Add(specification, element);
					}
				}
			);

			return result;
		}

		[CanBeNull]
		public IList<QualitySpecificationTableRow> GetQualitySpecificationsToReference(
			[NotNull] QualityCondition qualityCondition,
			[NotNull] IWin32Window owner)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));
			Assert.ArgumentNotNull(owner, nameof(owner));

			DdxModel model = DataQualityCategoryUtils.GetDefaultModel(qualityCondition.Category);

			IList<QualitySpecification> qualitySpecifications =
				_modelBuilder.ReadOnlyTransaction(
					() => _modelBuilder.QualitySpecifications.Get(qualityCondition));

			var queries = new List<FinderQuery<QualitySpecificationTableRow>>
			              {
				              new FinderQuery<QualitySpecificationTableRow>(
					              "<All>", "[all]",
					              () =>
						              QSpec.TableRows.GetQualitySpecificationTableRows(
							              _modelBuilder, qualitySpecifications))
			              };

			if (model != null)
			{
				queries.Add(new FinderQuery<QualitySpecificationTableRow>(
					            $"Quality specifications involving datasets in {model.Name}",
					            $"model{model.Id}",
					            () => QSpec.TableRows.GetQualitySpecificationTableRows(
						            _modelBuilder, qualitySpecifications, model)));
			}

			var finder = new Finder<QualitySpecificationTableRow>();

			return finder.ShowDialog(
				owner, queries,
				filterSettingsContext: FinderContextIds.GetId(qualityCondition.Category),
				allowMultiSelection: true);
		}

		[CanBeNull]
		public IList<InstanceConfigurationTableRow> GetIssueFiltersToAdd(
			[NotNull] QualityCondition qualityCondition,
			[NotNull] IWin32Window owner)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));
			Assert.ArgumentNotNull(owner, nameof(owner));

			DdxModel model = DataQualityCategoryUtils.GetDefaultModel(qualityCondition.Category);

			IList<IssueFilterConfiguration> issueFilterConfigurations =
				qualityCondition.IssueFilterConfigurations;

			var queries = new List<FinderQuery<InstanceConfigurationTableRow>>();

			if (model != null)
			{
				//TODO
				//queries.Add(
				//	new FinderQuery<InstanceConfigurationTableRow>(
				//		$"Issue filters involving datasets in {model.Name}",
				//		$"model{model.Id}",
				//		() => InstanceConfigTableRows
				//		      .GetInstanceConfigurationTableRows<IssueFilterConfiguration>(
				//			      _modelBuilder, issueFilterConfigurations, model)
				//		      .ToList()));
			}

			queries.Add(
				new FinderQuery<InstanceConfigurationTableRow>(
					"<All>", "[all]",
					() => InstanceConfigTableRows
					      .GetInstanceConfigurationTableRows(
						      _modelBuilder, issueFilterConfigurations)
					      .ToList()));

			var finder = new Finder<InstanceConfigurationTableRow>();

			return finder.ShowDialog(
				owner, queries,
				filterSettingsContext: FinderContextIds.GetId(qualityCondition.Category),
				allowMultiSelection: true);
		}

		public void OpenUrl()
		{
			QualityCondition qualityCondition = _modelBuilder.ReadOnlyTransaction(GetEntity);

			if (qualityCondition == null)
			{
				return;
			}

			string url = qualityCondition.Url;

			if (StringUtils.IsNullOrEmptyOrBlank(url))
			{
				_msg.Info("No Url defined");

				return;
			}

			_msg.InfoFormat("Opening url {0}...", url);

			ProcessUtils.StartProcess(url);
		}

		public void AssignNewVersionUuid()
		{
			QualityCondition qualityCondition = _modelBuilder.ReadOnlyTransaction(GetEntity);

			Assert.NotNull(qualityCondition, "Quality condition no longer exists");

			qualityCondition.AssignNewVersionUuid();

			NotifyChanged();
		}

		public void ExecuteWebHelpCommand()
		{
			_webHelpCommand?.Execute();
		}

		[CanBeNull]
		public string GetWebHelp([CanBeNull] TestDescriptor testDescriptor,
		                         [CanBeNull] out string title)
		{
			if (testDescriptor == null)
			{
				title = null;
				return null;
			}

			title = testDescriptor.TypeDisplayName;
			return TestReportUtils.WriteDescriptorDoc(testDescriptor);
		}

		private void UpdateImage([CanBeNull] QualityCondition qualityCondition)
		{
			if (qualityCondition == null)
			{
				// don't change
			}
			else
			{
				_image = TestTypeImageLookup.GetImage(qualityCondition);
				_image.Tag = TestTypeImageLookup.GetDefaultSortIndex(qualityCondition);

				_imageKey = string.Format("{0}#{1}", base.ImageKey,
				                          TestTypeImageLookup.GetImageKey(qualityCondition));
			}
		}

		/// <summary>
		/// Exports the quality condition to a specified file name
		/// </summary>
		/// <param name="exportFileName">Name of the export file.</param>
		/// <remarks>Expects to be called within a transaction</remarks>
		private void ExportQualityConditionCore([NotNull] string exportFileName)
		{
			Assert.ArgumentNotNullOrEmpty(exportFileName, nameof(exportFileName));

			QualityCondition qualityCondition = Assert.NotNull(GetEntity());

			using (TextWriter file = new StreamWriter(exportFileName))
			{
				try
				{
					TestFactory factory = Assert.NotNull(
						TestFactoryUtils.CreateTestFactory(qualityCondition),
						"Cannot create test factory");

					string data = factory.Export(qualityCondition);

					file.Write(data);
				}
				finally
				{
					file.Close();
				}
			}

			_msg.InfoFormat("Exported parameters of quality condition '{0}' to {1}",
			                qualityCondition.Name, exportFileName);
		}

		/// <summary>
		/// Imports the quality condition from a given file.
		/// </summary>
		/// <param name="importFileName">Name of the import file.</param>
		/// <remarks>Expects to be called within a transaction</remarks>
		private void ImportQualityConditionCore([NotNull] string importFileName)
		{
			Assert.ArgumentNotNullOrEmpty(importFileName, nameof(importFileName));

			QualityCondition qualityCondition = Assert.NotNull(GetEntity());

			QualityCondition paramValuesQa;
			using (var file = new StreamReader(importFileName))
			{
				try
				{
					IList<Dataset> datasets = _modelBuilder.Datasets.GetAll();

					TestFactory factory = Assert.NotNull(
						TestFactoryUtils.CreateTestFactory(qualityCondition),
						"Cannot create test factory");

					paramValuesQa = factory.CreateQualityCondition(
						file, datasets, qualityCondition.ParameterValues);
				}
				finally
				{
					file.Close();
				}
			}

			Assert.NotNull(paramValuesQa, "Unable to import quality condition parameters");

			qualityCondition.ClearParameterValues();

			foreach (TestParameterValue value in paramValuesQa.ParameterValues)
			{
				qualityCondition.AddParameterValue(value);
			}

			_msg.InfoFormat("Imported parameters of quality condition {0} from {1}",
			                qualityCondition.Name, importFileName);
		}

		[CanBeNull]
		private static IInstanceInfo GetInstanceInfo([NotNull] InstanceConfiguration entity)
		{
			InstanceDescriptor descriptor = entity.InstanceDescriptor;

			if (descriptor == null)
			{
				return null;
			}

			IInstanceInfo instanceInfo = InstanceDescriptorUtils.GetInstanceInfo(descriptor);

			return instanceInfo;
		}

		protected override string GetText(QualityCondition entity)
		{
			string suffix = entity.TestDescriptor == null
				                ? string.Empty
				                : string.Format(" ({0})", entity.TestDescriptor.Name);

			return $"{entity.Name}{suffix}";
		}

		protected override void OnSavedChanges(EventArgs e)
		{
			base.OnSavedChanges(e);

			if (_newCreated)
			{
				bool referencedByAnySpec = GetQualitySpecificationReferences().Any();

				if (! referencedByAnySpec)
				{
					_msg.Warn("The saved quality condition has not been assigned to any quality " +
					          "specification. Assign the condition to one or more specifications " +
					          "on the 'Quality Specifications' tab to make sure this condition can be used.");
				}
			}
		}

		protected override void CollectCommands(List<ICommand> commands,
		                                        IApplicationController
			                                        applicationController)
		{
			base.CollectCommands(commands, applicationController);

			if (_containerItem != null)
			{
				commands.Add(new CopyQualityConditionCommand(this, applicationController));
				commands.Add(new AssignQualityConditionsToCategoryCommand(new[] { this },
					             _containerItem, applicationController));

				_webHelpCommand = new ShowInstanceWebHelpCommand<QualityConditionItem>(
					this, applicationController);

				commands.Add(_webHelpCommand);
			}
		}

		protected override bool AllowDelete => _containerItem != null;

		protected override void UpdateItemStateCore(QualityCondition entity)
		{
			base.UpdateItemStateCore(entity);

			UpdateImage(entity);
		}

		protected override Control CreateControlCore(IItemNavigation itemNavigation)
		{
			return InstanceConfigurationControlFactory.CreateControl(
				this, itemNavigation, _modelBuilder, _tableStateQSpec, _tableStateIssueFilter);
		}

		protected override void IsValidForPersistenceCore(QualityCondition entity,
		                                                  Notification notification)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));
			Assert.ArgumentNotNull(notification, nameof(notification));

			if (entity.Name == null)
			{
				return; // already reported by entity
			}

			// validation of test parameters (see #198)
			foreach (var dsValue in entity.ParameterValues.OfType<DatasetTestParameterValue>())
			{
				if (dsValue.DatasetValue == null && dsValue.ValueSource == null)
				{
					if (IsDatasetParameterOptional(entity, dsValue))
					{
						continue;
					}

					notification.RegisterMessage(dsValue.TestParameterName,
					                             "Dataset not set",
					                             Severity.Error);
				}
			}

			// check if another quality condition with the same name exists
			QualityCondition existing = _modelBuilder.QualityConditions.Get(entity.Name);

			if (existing != null && existing.Id != entity.Id)
			{
				notification.RegisterMessage("Name",
				                             "A quality condition with the same name already exists",
				                             Severity.Error);
			}

			if (TestParameterValueUtils.CheckCircularReferencesInGraph(
				    entity, out string testParameterName,
				    out NotificationCollection configurationNames))
			{
				var message =
					$"Not allowed circular {entity.GetType().Name} references {NotificationUtils.Concatenate(configurationNames, " -> ")}";

				notification.RegisterMessage($"{testParameterName}", message, Severity.Error);
			}

			// Remember for OnSavedChanges
			_newCreated = IsNew;
		}

		private static bool IsDatasetParameterOptional(
			[NotNull] QualityCondition entity,
			[NotNull] DatasetTestParameterValue parameterValue)
		{
			IInstanceInfo instanceInfo = GetInstanceInfo(entity);

			if (instanceInfo == null)
			{
				return false;
			}

			TestParameter testParameter =
				instanceInfo.GetParameter(parameterValue.TestParameterName);

			if (testParameter != null)
			{
				// non-constructor parameters are always optional
				if (! testParameter.IsConstructorParameter) return true;

				// a constructor parameter is optional, if it is a list parameter
				if (testParameter.ArrayDimension > 0) return true;
			}

			return false;
		}
	}
}
