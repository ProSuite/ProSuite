using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public sealed class QualitySpecificationsItem : EntityTypeItem<QualitySpecification>,
	                                                IQualitySpecificationContainerItem
	{
		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;
		[NotNull] private readonly IQualitySpecificationContainer _container;
		[NotNull] private static readonly Image _image;
		[NotNull] private static readonly Image _selectedImage;

		static QualitySpecificationsItem()
		{
			_image = ItemUtils.GetGroupItemImage(Resources.QualitySpecificationsOverlay);
			_selectedImage = ItemUtils.GetGroupItemSelectedImage(
				Resources.QualitySpecificationsOverlay);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="QualitySpecificationsItem"/> class.
		/// </summary>
		/// <param name="modelBuilder">The model builder.</param>
		/// <param name="container">The container for the quality specifications</param>
		public QualitySpecificationsItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder,
		                                 [NotNull] IQualitySpecificationContainer container)
			: base("Quality Specifications", "Sets of quality conditions")
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));
			Assert.ArgumentNotNull(container, nameof(container));

			_modelBuilder = modelBuilder;
			_container = container;
		}

		public override Image Image => _image;

		public override Image SelectedImage => _selectedImage;

		protected override bool AllowDeleteSelectedChildren => true;

		protected override IEnumerable<Item> GetChildren()
		{
			return _container.GetQualitySpecificationItems(this);
		}

		protected override Control CreateControlCore(IItemNavigation itemNavigation)
		{
			return CreateTableControl(GetTableRows, itemNavigation);
		}

		protected override void CollectCommands(
			List<ICommand> commands,
			IApplicationController applicationController)
		{
			base.CollectCommands(commands, applicationController);

			commands.Add(new AddQualitySpecificationCommand(this, applicationController, this));
			// TODO re-enable after adding options for controlling category import/assignment (see todo's in XmlDataQualityImporter)
			//commands.Add(new ImportQualitySpecificationsCommand(this, applicationController,
			//													_container));
			commands.Add(new ExportQualitySpecificationsCommand(this, applicationController,
			                                                    _container,
			                                                    includeSubCategories: false));
			commands.Add(new ExportDatasetDependenciesCommand(
				             this, applicationController,
				             _container,
				             includeSubCategories: false));
			commands.Add(new DeleteAllChildItemsCommand(this, applicationController));
		}

		protected override void CollectCommands(List<ICommand> commands,
		                                        IApplicationController applicationController,
		                                        ICollection<Item> selectedChildren)
		{
			base.CollectCommands(commands, applicationController, selectedChildren);

			commands.Add(new AssignQualitySpecificationsToCategoryCommand(
				             selectedChildren.OfType<QualitySpecificationItem>().ToList(),
				             this, applicationController));
		}

		void IQualitySpecificationContainerItem.AddNewQualitySpecificationItem()
		{
			AddQualitySpecificationItem(_container.CreateQualitySpecificationItem(this));
		}

		void IQualitySpecificationContainerItem.CreateCopy(QualitySpecificationItem item)
		{
			AddQualitySpecificationItem(
				QualitySpecificationContainerUtils.CreateCopy(item, _modelBuilder, _container,
				                                              this));
		}

		bool IQualitySpecificationContainerItem.AssignToCategory(
			ICollection<QualitySpecificationItem> items,
			IWin32Window owner,
			out DataQualityCategory category)
		{
			if (! QualitySpecificationContainerUtils.AssignToCategory(items,
				    _modelBuilder,
				    owner,
				    out category))
			{
				return false;
			}

			RefreshChildren();
			return true;
		}

		QualitySpecification IQualitySpecificationContainerItem.GetQualitySpecification(
			QualitySpecificationItem item)
		{
			return _modelBuilder.ReadOnlyTransaction(() => item.GetEntity());
		}

		IEnumerable<QualitySpecification> IQualitySpecificationContainerItem.
			GetQualitySpecifications(bool includeSubCategories)
		{
			return _container.GetQualitySpecifications(includeSubCategories);
		}

		[NotNull]
		private IEnumerable<QualitySpecificationInCategoryTableRow> GetTableRows()
		{
			var comparer = new QualitySpecificationListComparer();

			return _container.GetQualitySpecifications()
			                 .OrderBy(q => q, comparer)
			                 .Select(qspec => new QualitySpecificationInCategoryTableRow(qspec));
		}

		private void AddQualitySpecificationItem([NotNull] QualitySpecificationItem item)
		{
			AddChild(item);

			item.NotifyChanged();
		}
	}
}
