using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Content.QA.InstanceDescriptors;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
{
	public class TestDescriptorsItem : EntityTypeItem<TestDescriptor>
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;
		[NotNull] private static readonly Image _image;
		[NotNull] private static readonly Image _selectedImage;

		static TestDescriptorsItem()
		{
			_image = ItemUtils.GetGroupItemImage(Resources.TestDescriptorsOverlay);
			_selectedImage = ItemUtils.GetGroupItemSelectedImage(
				Resources.TestDescriptorsOverlay);
		}

		public TestDescriptorsItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder)
			: base("Test Descriptors", "Test implementations")
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
		}

		public override Image Image => _image;

		public override Image SelectedImage => _selectedImage;

		protected override bool AllowDeleteSelectedChildren => true;

		protected override bool SortChildren => true;

		protected override IEnumerable<Item> GetChildren()
		{
			return _modelBuilder.GetChildren(this);
		}

		protected override void CollectCommands(
			List<ICommand> commands,
			IApplicationController applicationController)
		{
			base.CollectCommands(commands, applicationController);

			commands.Add(new AddTestDescriptorCommand(this, applicationController));
			commands.Add(new AddTestDescriptorsFromAssemblyCommand(this, applicationController));

			commands.Add(new DeleteAllChildItemsCommand(this, applicationController));
		}

		[NotNull]
		public TestDescriptorItem AddTestDescriptorItem()
		{
			var testDescriptor = new TestDescriptor();

			var item = new TestDescriptorItem(_modelBuilder, testDescriptor,
			                                  _modelBuilder.TestDescriptors);

			AddChild(item);

			item.NotifyChanged();

			return item;
		}

		public void AddTestDescriptors(string dllFilePath, IItemNavigation itemNavigation)
		{
			using (_msg.IncrementIndentation(
				       "Adding test descriptors from assembly {0}", dllFilePath))
			{
				Assembly assembly = Assembly.LoadFile(dllFilePath);

				IList<TestDescriptor> newDescriptors =
					InstanceDescriptorItemUtils.CreateTestDescriptors(assembly);

				itemNavigation.GoToItem(this);

				InstanceDescriptorItemUtils.RegisterDescriptors(
					_modelBuilder, newDescriptors, _modelBuilder.TestDescriptors,
					"test descriptor");

				RefreshChildren();
			}
		}

		protected override Control CreateControlCore(IItemNavigation itemNavigation)
		{
			return CreateTableControl(GetTableRows, itemNavigation);
		}

		[NotNull]
		protected virtual IEnumerable<TestDescriptorTableRow> GetTableRows()
		{
			ITestDescriptorRepository repository = _modelBuilder.TestDescriptors;

			IList<TestDescriptor> testDescriptors = repository.GetAll()
			                                                  .OrderBy(t => t.Name)
			                                                  .ToList();

			IDictionary<int, int> refCountMap =
				repository.GetReferencingQualityConditionCount();

			foreach (TestDescriptor testDescriptor in testDescriptors)
			{
				int refCount;
				if (! refCountMap.TryGetValue(testDescriptor.Id, out refCount))
				{
					refCount = 0;
				}

				yield return new TestDescriptorTableRow(testDescriptor, refCount);
			}
		}
	}
}
