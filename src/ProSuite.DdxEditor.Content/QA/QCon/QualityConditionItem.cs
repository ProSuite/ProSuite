using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
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
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;
using ProSuite.QA.Core;
using ProSuite.UI.QA;
using ProSuite.UI.QA.PropertyEditors;
using ProSuite.UI.QA.ResourceLookup;

namespace ProSuite.DdxEditor.Content.QA.QCon
{
	public class QualityConditionItem : EntityItem<QualityCondition, QualityCondition>
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;
		[CanBeNull] private readonly IInstanceConfigurationContainerItem _containerItem;
		[NotNull] private readonly TableState _tableState = new TableState();

		[CanBeNull] private Image _image;
		[CanBeNull] private string _imageKey;

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
		}

		#endregion

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
			TestFactory factory = GetTestFactory(qualityCondition.TestDescriptor);

			return factory?.TestDescription;
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
				TestFactory factory = GetTestFactory(testDescriptor);

				if (factory == null)
				{
					return null;
				}

				result = DefaultTestConfiguratorFactory.Create(
					factory, testDescriptor.TestAssemblyName, false);
			}

			result.DatasetProvider = _modelBuilder.GetTestParameterDatasetProvider();
			result.QualityCondition = qualityCondition;

			return result;
		}

		[CanBeNull]
		public IList<TestParameter> GetParameterDescription()
		{
			QualityCondition qualityCondition = Assert.NotNull(GetEntity());
			TestFactory factory = GetTestFactory(qualityCondition.TestDescriptor);

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
						_modelBuilder.TestDescriptors
						             .GetReferencingQualityConditionCount();

					foreach (TestDescriptor descriptor in _modelBuilder
					                                      .TestDescriptors.GetAll())
					{
						int refCount;
						if (! refCountMap.TryGetValue(descriptor.Id, out refCount))
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
		public IEnumerable<KeyValuePair<QualitySpecification, QualitySpecificationElement>>
			GetQualitySpecificationReferences()
		{
			QualityCondition qualityCondition = Assert.NotNull(GetEntity());

			IList<QualitySpecification> qualitySpecifications =
				_modelBuilder.Resolve<IQualitySpecificationRepository>().Get(
					qualityCondition);

			foreach (QualitySpecification specification in qualitySpecifications)
			{
				QualitySpecificationElement element = specification.GetElement(qualityCondition);

				Assert.NotNull(element,
				               "Element for {0} not found in referencing quality specification {1}",
				               qualityCondition.Name, specification.Name);

				yield return
					new KeyValuePair<QualitySpecification, QualitySpecificationElement>(
						specification, element);
			}
		}

		[CanBeNull]
		public IList<QualitySpecificationTableRow> GetQualitySpecificationsToReference(
			[NotNull] QualityCondition qualityCondition,
			[NotNull] IWin32Window owner)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

			DdxModel model = DataQualityCategoryUtils.GetDefaultModel(
				qualityCondition.Category);

			var queries = new List<FinderQuery<QualitySpecificationTableRow>>
			              {
				              new FinderQuery<QualitySpecificationTableRow>(
					              "<All>", "[all]",
					              () =>
						              QSpec.TableRows.GetQualitySpecifications(
							              _modelBuilder,
							              qualityCondition))
			              };

			if (model != null)
			{
				queries.Add(new FinderQuery<QualitySpecificationTableRow>(
					            $"Quality specifications involving datasets in {model.Name}",
					            $"model{model.Id}",
					            () => QSpec.TableRows.GetQualitySpecifications(
						            _modelBuilder,
						            qualityCondition,
						            model)));
			}

			var finder = new Finder<QualitySpecificationTableRow>();

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

			Process.Start(url);
		}

		public void AssignNewVersionUuid()
		{
			QualityCondition qualityCondition =
				_modelBuilder.ReadOnlyTransaction(GetEntity);

			Assert.NotNull(qualityCondition, "Quality condition no longer exists");

			qualityCondition.AssignNewVersionUuid();

			NotifyChanged();
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
				                          TestTypeImageLookup.GetImageKey(
					                          qualityCondition));
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

			_msg.Info(string.Format(
				          "Exported parameters of quality condition '{0}' to {1}",
				          qualityCondition.Name, exportFileName));
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

			Assert.NotNull(paramValuesQa,
			               "Unable to import quality condition parameters");

			qualityCondition.ClearParameterValues();

			foreach (TestParameterValue value in paramValuesQa.ParameterValues)
			{
				qualityCondition.AddParameterValue(value);
			}

			_msg.Info(string.Format(
				          "Imported parameters of quality condition {0} from {1}",
				          qualityCondition.Name, importFileName));
		}

		[CanBeNull]
		private static TestFactory GetTestFactory([CanBeNull] TestDescriptor testDescriptor)
		{
			return testDescriptor == null
				       ? null
				       : TestFactoryUtils.GetTestFactory(testDescriptor);
		}

		protected override string GetText(QualityCondition entity)
		{
			string suffix = entity.TestDescriptor == null
				                ? string.Empty
				                : string.Format(" ({0})", entity.TestDescriptor.Name);

			return $"{entity.Name}{suffix}";
		}

		protected override void CollectCommands(List<ICommand> commands,
		                                        IApplicationController
			                                        applicationController)
		{
			base.CollectCommands(commands, applicationController);

			if (_containerItem != null)
			{
				commands.Add(
					new CopyQualityConditionCommand(this, applicationController));
				commands.Add(new AssignQualityConditionsToCategoryCommand(new[] {this},
					             _containerItem,
					             applicationController));
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
				this, itemNavigation, _modelBuilder, _tableState);
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
			QualityCondition existing = _modelBuilder.QualityConditions.Get(entity.Name);

			if (existing != null && existing.Id != entity.Id)
			{
				notification.RegisterMessage("Name",
				                             "A quality condition with the same name already exists",
				                             Severity.Error);
			}
		}

		private static bool IsParameterOptional(
			[NotNull] QualityCondition entity,
			[NotNull] TestParameterValue parameterValue)
		{
			TestFactory factory = TestFactoryUtils.CreateTestFactory(entity);

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
	}
}
