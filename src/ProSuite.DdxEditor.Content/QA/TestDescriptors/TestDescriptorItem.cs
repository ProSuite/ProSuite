using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Finder;
using ProSuite.Commons.UI.WinForms;
using ProSuite.Commons.Validation;
using ProSuite.DdxEditor.Content.Datasets;
using ProSuite.DdxEditor.Content.QA.Categories;
using ProSuite.DdxEditor.Content.QA.InstanceDescriptors;
using ProSuite.DdxEditor.Content.QA.QSpec;
using ProSuite.DdxEditor.Content.QA.TestDescriptors.CreateQualityConditions;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Dependencies;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.UI.QA;
using ProSuite.UI.QA.ResourceLookup;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
{
	public class TestDescriptorItem : EntityItem<TestDescriptor, TestDescriptor>
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;
		[CanBeNull] private Image _image;
		[CanBeNull] private string _imageKey;
		[NotNull] private readonly TableState _tableState = new TableState();

		/// <summary>
		/// Initializes a new instance of the <see cref="TestDescriptorItem"/> class.
		/// </summary>
		/// <param name="modelBuilder">The model builder.</param>
		/// <param name="testDescriptor">The test descriptor.</param>
		/// <param name="repository">The repository.</param>
		public TestDescriptorItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder,
		                          [NotNull] TestDescriptor testDescriptor,
		                          [NotNull] IRepository<TestDescriptor> repository)
			: base(testDescriptor, repository)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
			UpdateImage(testDescriptor);
		}

		public override Image Image => _image;

		public override string ImageKey => _imageKey;

		private void UpdateImage([CanBeNull] TestDescriptor testDescriptor)
		{
			if (testDescriptor == null)
			{
				// don't change
			}
			else
			{
				_image = TestTypeImageLookup.GetImage(testDescriptor);
				_image.Tag = TestTypeImageLookup.GetDefaultSortIndex(testDescriptor);

				_imageKey = string.Format("{0}#{1}", base.ImageKey,
				                          TestTypeImageLookup
					                          .GetImageKey(testDescriptor));
			}
		}

		protected override void UpdateItemStateCore(TestDescriptor entity)
		{
			base.UpdateItemStateCore(entity);

			UpdateImage(entity);
		}

		protected override void IsValidForPersistenceCore(TestDescriptor entity,
		                                                  Notification notification)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));
			Assert.ArgumentNotNull(notification, nameof(notification));

			// check if another test descriptor with the same name exists
			if (entity.Name != null)
			{
				TestDescriptor descriptorWithSameName =
					_modelBuilder.TestDescriptors.Get(entity.Name);

				InstanceDescriptorItemUtils.ValidateDescriptorAgainstDuplicateName(
					entity, descriptorWithSameName, notification);
			}

			// check if another test descriptor with the implementation exists
			TestDescriptor descriptorWithSameImplementation =
				_modelBuilder.TestDescriptors.GetWithSameImplementation(entity);

			InstanceDescriptorItemUtils.ValidateDescriptorAgainstDuplicateImplementation(
				entity, descriptorWithSameImplementation, notification);
		}

		protected override Control CreateControlCore(IItemNavigation itemNavigation)
		{
			var control = new TestDescriptorControl(_tableState);

			new TestDescriptorPresenter(this, control, FindTestFactoryProvider,
			                            FindTestClassProvider,
			                            FindTestConfiguratorProvider, itemNavigation);
			return control;
		}

		[CanBeNull]
		private static ClassDescriptor FindTestFactoryProvider(
			[NotNull] IWin32Window owner,
			[CanBeNull] ClassDescriptor orig)
		{
			return FindClassDescriptor<TestFactory>(owner, orig);
		}

		[CanBeNull]
		private static ClassDescriptor FindTestClassProvider(
			[NotNull] IWin32Window owner,
			[CanBeNull] ClassDescriptor orig)
		{
			return FindClassDescriptor<ITest>(owner, orig);
		}

		[CanBeNull]
		private static ClassDescriptor FindTestConfiguratorProvider(
			[NotNull] IWin32Window owner,
			[CanBeNull] ClassDescriptor orig)
		{
			return FindClassDescriptor<ITestConfigurator>(owner, orig);
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

			commands.Add(new CreateQualityConditionCommand(this, applicationController));
			commands.Add(
				new BatchCreateQualityConditionsCommand(this, applicationController));
		}

		protected override bool AllowDelete => true;

		public QualityCondition CreateQualityCondition()
		{
			var condition = new QualityCondition(assignUuids: true);

			_modelBuilder.ReadOnlyTransaction(
				delegate { condition.TestDescriptor = GetEntity(); });

			return condition;
		}

		public IEnumerable<QualityCondition> GetQualityConditions()
		{
			return _modelBuilder.QualityConditions.Get(Assert.NotNull(GetEntity()));
		}

		[NotNull]
		public ICollection<string> GetQualityConditionNames()
		{
			var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			_modelBuilder.ReadOnlyTransaction(
				delegate
				{
					foreach (QualityCondition qualityCondition in GetQualityConditions())
					{
						result.Add(qualityCondition.Name);
					}
				});

			return result;
		}

		[CanBeNull]
		public IList<DatasetTableRow> GetApplicableDatasets(
			[NotNull] IWin32Window owner,
			[NotNull] string parameterName,
			[NotNull] IEnumerable<Dataset> alreadySelectedDatasets,
			bool excludeAlreadyUsedDatasets,
			[CanBeNull] DataQualityCategory category)
		{
			HashSet<Dataset> nonSelectableDatasets =
				excludeAlreadyUsedDatasets
					? GetSet(alreadySelectedDatasets)
					: null;

			DdxModel model = DataQualityCategoryUtils.GetDefaultModel(category);

			var queries = new List<FinderQuery<DatasetTableRow>>();

			if (model != null)
			{
				queries.Add(new FinderQuery<DatasetTableRow>(
					            string.Format("Datasets in {0}", model.Name),
					            string.Format("model{0}", model.Id),
					            () => GetDatasetTableRows(parameterName,
					                                      _modelBuilder,
					                                      nonSelectableDatasets,
					                                      model)));
			}

			queries.Add(new FinderQuery<DatasetTableRow>(
				            "<All>", "[all]",
				            () => GetDatasetTableRows(parameterName,
				                                      _modelBuilder,
				                                      nonSelectableDatasets)));

			var finder = new Finder<DatasetTableRow>();

			return finder.ShowDialog(
				owner, queries,
				filterSettingsContext: FinderContextIds.GetId(category),
				allowMultiSelection: true);
		}

		[NotNull]
		private List<DatasetTableRow> GetDatasetTableRows(
			[NotNull] string parameterName,
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
			[CanBeNull] HashSet<Dataset> nonSelectableDatasets,
			[CanBeNull] DdxModel model = null)
		{
			Stopwatch stopWatch = _msg.DebugStartTiming();

			IDatasetRepository repository = modelBuilder.Datasets;

			return modelBuilder.ReadOnlyTransaction(
				delegate
				{
					if (nonSelectableDatasets != null)
					{
						// Exclude also datasets that are referenced in existing quality conditions based 
						// on the same test descriptor
						foreach (QualityCondition qualityCondition in
						         GetQualityConditions())
						{
							foreach (TestParameterValue testParameterValue in
							         qualityCondition.GetParameterValues(parameterName))
							{
								var value = testParameterValue as DatasetTestParameterValue;

								if (value?.DatasetValue != null &&
								    ! nonSelectableDatasets.Contains(value.DatasetValue))
								{
									nonSelectableDatasets.Add(value.DatasetValue);
								}
							}
						}
					}

					IInstanceInfo instanceInfo = GetInstanceInfo();
					TestParameter testParameter = instanceInfo?.GetParameter(parameterName);

					IList<Dataset> datasets = model?.GetDatasets() ?? repository.GetAll();

					var result = new List<DatasetTableRow>();

					foreach (Dataset dataset in datasets)
					{
						if (dataset.Deleted)
						{
							continue;
						}

						if (! IsApplicableFor(testParameter, dataset))
						{
							continue;
						}

						var tableRow = new DatasetTableRow(dataset);

						if (nonSelectableDatasets != null)
						{
							tableRow.Selectable =
								! nonSelectableDatasets.Contains(dataset);
						}

						result.Add(tableRow);
					}

					_msg.DebugStopTiming(stopWatch, "Read {0} datasets", result.Count);

					return result;
				});
		}

		[NotNull]
		private static HashSet<Dataset> GetSet([NotNull] IEnumerable<Dataset> datasets)
		{
			var result = new HashSet<Dataset>();

			foreach (Dataset dataset in datasets)
			{
				result.Add(dataset);
			}

			return result;
		}

		// TODO consolidate with TestParameterDatasetProviderBase
		private static bool IsApplicableFor(TestParameter testParameter,
		                                    [NotNull] Dataset dataset)
		{
			if (testParameter == null || dataset.Deleted)
			{
				return false;
			}

			// error datasets shouldn't be themselves testable
			if (dataset is IErrorDataset)
			{
				return false;
			}

			DdxModel model = dataset.Model;

			if (model == null)
			{
				return false;
			}

			if (! (model is ProductionModel))
			{
				return false;
			}

			TestParameterType parameterType =
				TestParameterTypeUtils.GetParameterType(testParameter.Type);

			if ((parameterType & TestParameterType.Dataset) != 0)
			{
				return false;
			}

			if ((parameterType & TestParameterType.VectorDataset) ==
			    TestParameterType.VectorDataset &&
			    dataset is VectorDataset)
			{
				return true;
			}

			if ((parameterType & TestParameterType.ObjectDataset) ==
			    TestParameterType.ObjectDataset &&
			    dataset is IObjectDataset)
			{
				return true;
			}

			if ((parameterType & TestParameterType.GeometricNetworkDataset) ==
			    TestParameterType.GeometricNetworkDataset &&
			    dataset.TypeDescription == "Geometric Network")
			{
				return true;
			}

			if ((parameterType & TestParameterType.TopologyDataset) ==
			    TestParameterType.TopologyDataset &&
			    dataset is TopologyDataset)
			{
				return true;
			}

			if ((parameterType & TestParameterType.TerrainDataset) ==
			    TestParameterType.TerrainDataset &&
			    dataset is ISimpleTerrainDataset)
			{
				return true;
			}

			if ((parameterType & TestParameterType.RasterMosaicDataset) ==
			    TestParameterType.RasterMosaicDataset &&
			    dataset is RasterMosaicDataset)
			{
				return true;
			}

			if ((parameterType & TestParameterType.RasterDataset) ==
			    TestParameterType.RasterDataset &&
			    dataset is RasterDataset)
			{
				return true;
			}

			return false;
		}

		public bool CanBatchCreateQualityConditions(
			[NotNull] out string datasetParameterName,
			[NotNull] out string reason)
		{
			datasetParameterName = string.Empty;

			if (IsDirty || IsNew)
			{
				reason = "There are pending changes";
				return false;
			}

			string dsParameterName = null;
			var enabled = false;

			try
			{
				_modelBuilder.ReadOnlyTransaction(
					delegate
					{
						IInstanceInfo instanceInfo = Assert.NotNull(GetInstanceInfo());

						var parameters =
							instanceInfo.Parameters ?? Enumerable.Empty<TestParameter>();

						foreach (TestParameter parameter in parameters)
						{
							if (TestParameterTypeUtils.IsDatasetType(parameter.Type))
							{
								if (dsParameterName != null)
								{
									return;
								}

								dsParameterName = parameter.Name;

								if (parameter.ArrayDimension > 0 &&
								    parameter.IsConstructorParameter)
								{
									return;
								}
							}
							else
							{
								// scalar parameter - no arrays allowed if a required constructor parameter
								if (parameter.ArrayDimension > 0 &&
								    parameter.IsConstructorParameter)
								{
									return;
								}
							}
						}

						enabled = true;
					});
			}
			catch (Exception e)
			{
				reason = string.Format("Error accessing test descriptor: {0}", e.Message);
				return false;
			}

			reason = "Only one non-array dataset parameter and only " +
			         "non-array scalar parameters are currently supported";
			datasetParameterName = dsParameterName;

			return enabled;
		}

		public bool BatchCreateQualityConditions(
			[CanBeNull] IWin32Window window,
			[CanBeNull] out DataQualityCategory targetCategory)
		{
			string datasetParameterName;
			string reason;
			Assert.True(
				CanBatchCreateQualityConditions(out datasetParameterName, out reason),
				"Operation not supported for test descriptor {0}: {1}",
				Text, reason);

			DataQualityCategory category;
			IEnumerable<QualityConditionParameters> qualityConditionParameters =
				GetQualityConditionParameters(window, datasetParameterName,
				                              out category);

			if (qualityConditionParameters == null)
			{
				targetCategory = null;
				return false;
			}

			using (new WaitCursor())
			{
				_modelBuilder.UseTransaction(
					delegate
					{
						TestDescriptor testDescriptor = Assert.NotNull(GetEntity());

						foreach (QualityConditionParameters parameters in
						         qualityConditionParameters)
						{
							QualityCondition qualityCondition = CreateQualityCondition(
								testDescriptor, parameters, datasetParameterName);

							qualityCondition.Category = category;

							_msg.InfoFormat("Creating quality condition {0}",
							                qualityCondition.Name);

							_modelBuilder.QualityConditions.Save(qualityCondition);
						}
					});
			}

			targetCategory = category;
			return true;
		}

		[CanBeNull]
		public IList<QualitySpecificationTableRow> GetQualitySpecificationsToReference(
			[NotNull] IWin32Window owner,
			[NotNull] ICollection<QualitySpecification> selectedQualitySpecifications,
			[CanBeNull] DataQualityCategory category)
		{
			DdxModel model = DataQualityCategoryUtils.GetDefaultModel(category);

			var queries = new List<FinderQuery<QualitySpecificationTableRow>>();

			queries.Add(new FinderQuery<QualitySpecificationTableRow>(
				            "<All>", "[all]",
				            () => TableRows.GetQualitySpecificationTableRows(
					            _modelBuilder, selectedQualitySpecifications)));

			if (model != null)
			{
				queries.Add(new FinderQuery<QualitySpecificationTableRow>(
					            $"Quality specifications involving datasets in {model.Name}",
					            $"model{model.Id}",
					            () => TableRows.GetQualitySpecificationTableRows(
						            _modelBuilder, selectedQualitySpecifications,
						            model)));
			}

			var finder = new Finder<QualitySpecificationTableRow>();
			return finder.ShowDialog(owner, queries, allowMultiSelection: true);
		}

		[NotNull]
		private static QualityCondition CreateQualityCondition(
			[NotNull] TestDescriptor testDescriptor,
			[NotNull] QualityConditionParameters parameters,
			[NotNull] string datasetParameterName)
		{
			Assert.ArgumentNotNull(testDescriptor, nameof(testDescriptor));
			Assert.ArgumentNotNull(parameters, nameof(parameters));
			Assert.ArgumentNotNullOrEmpty(datasetParameterName,
			                              nameof(datasetParameterName));

			var result = new QualityCondition(parameters.Name, testDescriptor);

			AddParameterValue(result, datasetParameterName,
			                  parameters.Dataset,
			                  parameters.FilterExpression);

			foreach (ScalarParameterValue parameter in parameters.ScalarParameters)
			{
				InstanceConfigurationUtils.AddScalarParameterValue(
					result, parameter.Name, parameter.Value);
			}

			foreach (QualitySpecification qualitySpecification in parameters.QualitySpecifications)
			{
				qualitySpecification.AddElement(result);
			}

			return result;
		}

		[NotNull]
		private static TestParameterValue AddParameterValue(
			[NotNull] InstanceConfiguration qualityCondition,
			[NotNull] string parameterName,
			[CanBeNull] Dataset value,
			string filterExpression = null,
			bool usedAsReferenceData = false)
		{
			TestParameterValue result = InstanceConfigurationUtils.AddParameterValue(
				qualityCondition, parameterName, value, filterExpression, usedAsReferenceData);

			TestParameterTypeUtils.AssertValidDataset(Assert.NotNull(result.DataType), value);

			return result;
		}

		[CanBeNull]
		private IEnumerable<QualityConditionParameters> GetQualityConditionParameters(
			[CanBeNull] IWin32Window window,
			[NotNull] string datasetParameterName,
			[CanBeNull] out DataQualityCategory targetCategory)
		{
			IList<TestParameter> testParameters = _modelBuilder.ReadOnlyTransaction(
				() => Assert.NotNull(GetInstanceInfo()).Parameters);

			IList<QualityConditionParameters> result;
			using (var form = new CreateQualityConditionsForm())
			{
				new CreateQualityConditionsPresenter(form, this, datasetParameterName,
				                                     testParameters);

				DialogResult dialogResult = form.ShowDialog(window);

				if (dialogResult != DialogResult.OK)
				{
					targetCategory = null;
					return null;
				}

				targetCategory = form.TargetCategory;
				result = form.QualityConditionParameters;
			}

			return result;
		}
	}
}
