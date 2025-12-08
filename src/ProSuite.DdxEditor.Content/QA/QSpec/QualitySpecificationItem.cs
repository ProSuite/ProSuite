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
using ProSuite.Commons.Text;
using ProSuite.Commons.UI.Finder;
using ProSuite.Commons.UI.WinForms;
using ProSuite.Commons.Validation;
using ProSuite.DdxEditor.Content.QA.Categories;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Dependencies;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.QA.SpecificationReport;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.QA;
using ProSuite.UI.Core.QA.ResourceLookup;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public class QualitySpecificationItem :
		EntityItem<QualitySpecification, QualitySpecification>
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;
		[NotNull] private readonly IQualitySpecificationContainerItem _containerItem;
		[NotNull] private readonly TableState _tableState = new TableState();

		[CanBeNull] private Image _image;
		[CanBeNull] private string _imageKey;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="QualitySpecificationItem"/> class.
		/// </summary>
		/// <param name="modelBuilder">The model builder.</param>
		/// <param name="entity">The entity.</param>
		/// <param name="containerItem"></param>
		/// <param name="repository">The repository.</param>
		public QualitySpecificationItem(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
			[NotNull] QualitySpecification entity,
			[NotNull] IQualitySpecificationContainerItem containerItem,
			[NotNull] IRepository<QualitySpecification> repository)
			: base(entity, repository)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
			_containerItem = containerItem;

			UpdateImage(entity);
		}

		#endregion

		public override Image Image => _image;

		public override string ImageKey => _imageKey;

		[CanBeNull]
		public IList<QualityConditionWithTestParametersTableRow>
			GetQualityConditionsToAdd(
				[NotNull] QualitySpecification qualitySpecification, IWin32Window owner)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));

			DdxModel model = DataQualityCategoryUtils.GetDefaultModel(
				qualitySpecification.Category);

			var queries =
				new List<FinderQuery<QualityConditionWithTestParametersTableRow>>();

			if (model != null)
			{
				queries.Add(new FinderQuery<QualityConditionWithTestParametersTableRow>(
					            $"Quality conditions involving datasets in {model.Name}",
					            $"model{model.Id}",
					            () => QCon.TableRows.GetQualityConditions(_modelBuilder,
						            qualitySpecification,
						            model)));
			}

			queries.Add(new FinderQuery<QualityConditionWithTestParametersTableRow>(
				            "<All>", "[all]",
				            () => QCon.TableRows.GetQualityConditions(_modelBuilder,
					            qualitySpecification)));

			var finder = new Finder<QualityConditionWithTestParametersTableRow>();
			return finder.ShowDialog(
				owner, queries,
				filterSettingsContext: FinderContextIds.GetId(
					qualitySpecification.Category),
				allowMultiSelection: true);
		}

		[CanBeNull]
		public QualitySpecification GetQualitySpecification()
		{
			return _modelBuilder.UseTransaction(GetEntity);
		}

		public void ExportDatasetDependencies(
			ICollection<KeyValuePair<string, ICollection<QualitySpecification>>>
				qualitySpecificationsByFileName,
			IEnumerable<string> deletableFiles,
			ExportDatasetDependenciesOptions options)
		{
			QualitySpecificationsItemUtils.ExportDatasetDependencies(
				qualitySpecificationsByFileName, deletableFiles,
				options, _modelBuilder);
		}

		public void ExportQualitySpecifications(
			[NotNull] IDictionary<string, ICollection<QualitySpecification>>
				qualitySpecificationsByFileName,
			[NotNull] IEnumerable<string> deletableFiles,
			bool exportMetadata,
			bool? exportWorkspaceConnections,
			bool exportConnectionFilePaths,
			bool exportAllDescriptors,
			bool exportAllCategories,
			bool exportNotes)
		{
			QualitySpecificationsItemUtils.ExportQualitySpecifications(
				qualitySpecificationsByFileName,
				deletableFiles,
				exportMetadata,
				exportWorkspaceConnections,
				exportConnectionFilePaths,
				exportAllDescriptors,
				exportAllCategories,
				exportNotes,
				_modelBuilder);
		}

		public void ImportQualitySpecification([NotNull] string fileName,
		                                       bool ignoreConditionsForUnknownDatasets,
		                                       bool updateDescriptorNames,
		                                       bool updateDescriptorProperties)
		{
			Assert.ArgumentNotNullOrEmpty(fileName, nameof(fileName));

			using (new WaitCursor())
			{
				QualitySpecification qualitySpecification =
					_modelBuilder.ReadOnlyTransaction(() => GetEntity());

				using (
					_msg.IncrementIndentation(
						"Importing quality specification '{0}' from {1}",
						qualitySpecification.Name, fileName))
				{
					_modelBuilder.DataQualityImporter.Import(fileName,
					                                         new[] {qualitySpecification},
					                                         ignoreConditionsForUnknownDatasets,
					                                         updateDescriptorNames,
					                                         updateDescriptorProperties);
				}

				// TODO report stats (inserted, updated qcons and testdescs)
				_msg.InfoFormat("Quality specification '{0}' ({1}) imported from {2}",
				                qualitySpecification.Name,
				                QualitySpecificationsItemUtils.GetElementCountMessage(
					                qualitySpecification),
				                fileName);
			}
		}

		public void CreateCopy()
		{
			_containerItem.CreateCopy(this);
		}

		public int RemoveElements(
			[NotNull] IEnumerable<QualitySpecificationElementTableRow> selected)
		{
			Assert.ArgumentNotNull(selected, nameof(selected));

			return _modelBuilder.ReadOnlyTransaction(
				delegate
				{
					QualitySpecification qualitySpecification =
						Assert.NotNull(GetEntity());

					var elementsToRemove =
						new HashSet<QualitySpecificationElement>(
							selected.Select(row => row.Element));

					if (elementsToRemove.SetEquals(qualitySpecification.Elements))
					{
						// NOTE: removing many/all elements turns out to be slow due to binding/data grid drawing
						//       Optimizing the "remove all" case here does not make a big difference

						qualitySpecification.RemoveAllElements();
					}
					else
					{
						// remove them from the entity
						foreach (var element in elementsToRemove)
						{
							qualitySpecification.RemoveElement(element);
						}
					}

					return qualitySpecification.Elements.Count;
				});
		}

		[NotNull]
		public HashSet<int> GetQualityConditionIdsInvolvingDeletedDatasets()
		{
			return _modelBuilder.QualityConditions.GetIdsInvolvingDeletedDatasets();
		}

		protected override void CollectCommands(
			List<ICommand> commands,
			IApplicationController applicationController)
		{
			base.CollectCommands(commands, applicationController);

			commands.Add(new CopyQualitySpecificationCommand(this,
				             applicationController));
			commands.Add(new AssignQualitySpecificationsToCategoryCommand(new[] {this},
				             _containerItem,
				             applicationController));
			commands.Add(new ExportQualitySpecificationCommand(this, _containerItem,
			                                                   applicationController));
			commands.Add(
				new ImportQualitySpecificationCommand(this, applicationController));
			commands.Add(new ExportDatasetDependenciesForQualitySpecificationCommand(
				             this, _containerItem, applicationController));

			string reportTemplate = _modelBuilder.QualitySpecificationReportTemplate;
			if (StringUtils.IsNotEmpty(reportTemplate) && File.Exists(reportTemplate))
			{
				commands.Add(new CreateQualitySpecificationReportCommand(this,
					             applicationController,
					             reportTemplate));
			}
		}

		protected override bool AllowDelete => true;

		protected override Control CreateControlCore(IItemNavigation itemNavigation)
		{
			var control = new QualitySpecificationControl(_tableState);

			new QualitySpecificationPresenter(this, control, itemNavigation,
			                                  qualityConditions =>
				                                  AssignToCategory(qualityConditions,
					                                  itemNavigation,
					                                  control));

			return control;
		}

		public override IList<DependingItem> GetDependingItems()
		{
			return _modelBuilder.GetDependingItems(GetEntity());
		}

		protected override void IsValidForPersistenceCore(QualitySpecification entity,
		                                                  Notification notification)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));
			Assert.ArgumentNotNull(notification, nameof(notification));

			if (entity.Name == null)
			{
				return; // already reported by entity
			}

			// check if another entity with the same name exists
			QualitySpecification existing = _modelBuilder.QualitySpecifications.Get(entity.Name);

			if (existing != null && existing.Id != entity.Id)
			{
				notification.RegisterMessage("Name",
				                             "A quality specification with the same name already exists",
				                             Severity.Error);
			}
		}

		protected override void UpdateItemStateCore(QualitySpecification entity)
		{
			base.UpdateItemStateCore(entity);

			UpdateImage(entity);
		}

		private bool AssignToCategory(
			[NotNull] ICollection<QualityCondition> qualityConditions,
			[NotNull] IItemNavigation itemNavigation,
			[NotNull] IWin32Window owner)
		{
			var categoriesToRefresh = new HashSet<DataQualityCategory>();
			foreach (QualityCondition qualityCondition in qualityConditions)
			{
				if (qualityCondition.Category != null)
				{
					categoriesToRefresh.Add(qualityCondition.Category);
				}
			}

			DataQualityCategory category;
			bool assigned = QualityConditionContainerUtils.AssignToCategory(
				qualityConditions.Cast<InstanceConfiguration>().ToList(), _modelBuilder,
				owner, out category);

			if (! assigned)
			{
				return false;
			}

			categoriesToRefresh.Add(category); // may be null

			foreach (DataQualityCategory categoryToRefresh in categoriesToRefresh)
			{
				QualityConditionContainerUtils.RefreshQualityConditionAssignmentTarget(
					categoryToRefresh,
					itemNavigation);
			}

			return true;
		}

		private void UpdateImage([CanBeNull] QualitySpecification qualitySpecification)
		{
			if (qualitySpecification == null)
			{
				// don't change
			}
			else
			{
				_image = QualitySpecificationImageLookup.GetImage(qualitySpecification);

				_imageKey = string.Format(
					"{0}#{1}",
					base.ImageKey,
					QualitySpecificationImageLookup.GetImageKey(qualitySpecification));
			}
		}

		public void OpenUrl()
		{
			QualitySpecification entity = _modelBuilder.ReadOnlyTransaction(GetEntity);

			if (entity == null)
			{
				return;
			}

			string url = entity.Url;

			if (StringUtils.IsNullOrEmptyOrBlank(url))
			{
				_msg.Info("No Url defined");

				return;
			}

			_msg.InfoFormat("Opening url {0}...", url);

			ProcessUtils.StartProcess(url);
		}

		public void CreateReport([NotNull] string htmlFilePath,
		                         [NotNull] string reportTemplate,
		                         bool overwrite)
		{
			Assert.ArgumentNotNullOrEmpty(htmlFilePath, nameof(htmlFilePath));
			Assert.ArgumentNotNullOrEmpty(reportTemplate, nameof(reportTemplate));

			HtmlQualitySpecification report = _modelBuilder.ReadOnlyTransaction(
				() => SpecificationReportUtils.CreateHtmlQualitySpecification(
					Assert.NotNull(GetEntity()),
					optionsProvider: null));

			if (File.Exists(htmlFilePath))
			{
				if (! overwrite)
				{
					throw new IOException($"File already exists: {htmlFilePath}");
				}

				File.Delete(htmlFilePath);
			}

			string path = SpecificationReportUtils.RenderHtmlQualitySpecification(report,
				reportTemplate,
				htmlFilePath);

			_msg.InfoFormat("Report for quality specification '{0}' created: {1}",
			                report.Name, path);

			_msg.Info("Opening report...");
			ProcessUtils.StartProcess(path);
		}
	}
}
