using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.QSpec;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.QCon
{
	public class QualityConditionsItem : EntityTypeItem<QualityCondition>,
	                                     IQualityConditionContainerItem
	{
		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;
		[NotNull] private readonly IQualityConditionContainer _container;
		[NotNull] private static readonly Image _image;
		[NotNull] private static readonly Image _selectedImage;

		static QualityConditionsItem()
		{
			_image = ItemUtils.GetGroupItemImage(Resources.QualityConditionsOverlay);
			_selectedImage =
				ItemUtils.GetGroupItemSelectedImage(Resources.QualityConditionsOverlay);
		}

		public QualityConditionsItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder,
		                             [NotNull] IQualityConditionContainer container)
			: base("Quality Conditions",
			       "Configured quality definitions involving one or more datasets")
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
			return _container.GetQualityConditionItems(this);
		}

		protected override Control CreateControlCore(IItemNavigation itemNavigation)
		{
			return _modelBuilder.ListQualityConditionsWithDataset
				       ? (Control)
				       CreateTableControl(_container.GetQualityConditionDatasetTableRows,
				                          itemNavigation)
				       : CreateTableControl(_container.GetQualityConditionTableRows,
				                            itemNavigation);
		}

		protected override void CollectCommands(
			List<ICommand> commands,
			IApplicationController applicationController)
		{
			base.CollectCommands(commands, applicationController);

			commands.Add(new AddQualityConditionCommand(this, applicationController, this));
			commands.Add(new DeleteAllChildItemsCommand(this, applicationController));
		}

		protected override void CollectCommands(List<ICommand> commands,
		                                        IApplicationController applicationController,
		                                        ICollection<Item> selectedChildren)
		{
			base.CollectCommands(commands, applicationController, selectedChildren);

			List<QualityConditionItem> selectedConditionItems =
				selectedChildren.OfType<QualityConditionItem>().ToList();

			if (selectedConditionItems.Count > 0)
			{
				commands.Add(new AssignQualityConditionsToCategoryCommand(
					             selectedConditionItems, this, applicationController));
			}
		}

		[NotNull]
		public QualityConditionItem AddQualityConditionItem(
			[NotNull] QualityCondition qualityCondition)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

			var item = new QualityConditionItem(_modelBuilder, qualityCondition, this,
			                                    _modelBuilder.QualityConditions);

			AddQualityConditionItem(item);

			return item;
		}

		void IQualityConditionContainerItem.AddNewQualityConditionItem()
		{
			AddQualityConditionItem(_container.CreateQualityConditionItem(this));
		}

		void IQualityConditionContainerItem.CreateCopy(QualityConditionItem item)
		{
			QualityCondition copy = _modelBuilder.ReadOnlyTransaction(
				() => Assert.NotNull(item.GetEntity()).CreateCopy());

			copy.Name = string.Format("Copy of {0}", copy.Name);

			AddQualityConditionItem(new QualityConditionItem(_modelBuilder, copy, this,
			                                                 _modelBuilder.QualityConditions));
		}

		bool IQualityConditionContainerItem.AssignToCategory(
			ICollection<QualityConditionItem> items,
			IWin32Window owner,
			out DataQualityCategory category)
		{
			if (! QualityConditionContainerUtils.AssignToCategory(items, _modelBuilder, owner,
			                                                      out category))
			{
				return false;
			}

			RefreshChildren();
			return true;
		}

		public QualityCondition GetQualityCondition(QualityConditionItem item)
		{
			return _modelBuilder.ReadOnlyTransaction(() => item.GetEntity());
		}

		private void AddQualityConditionItem(QualityConditionItem item)
		{
			AddChild(item);

			item.NotifyChanged();
		}
	}
}
