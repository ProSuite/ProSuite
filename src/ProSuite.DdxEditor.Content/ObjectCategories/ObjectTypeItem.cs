using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.ObjectCategories
{
	public class ObjectTypeItem : ObjectCategoryItem<ObjectType>
	{
		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;

		public ObjectTypeItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder,
		                      [NotNull] ObjectType objectCategory,
		                      [NotNull] IRepository<ObjectCategory> repository)
			: base(modelBuilder, objectCategory, repository)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
		}

		protected override IEnumerable<Item> GetChildren()
		{
			return _modelBuilder.GetChildren(this, Model);
		}

		// has no own control

		protected override void CollectCommands(List<ICommand> commands,
		                                        IApplicationController
			                                        applicationController)
		{
			base.CollectCommands(commands, applicationController);

			commands.Add(new AddObjectSubtypeCommand(this, applicationController));
		}

		public ObjectSubtypeItem AddObjectSubtypeItem()
		{
			ObjectType entity = null;
			_modelBuilder.ReadOnlyTransaction(delegate { entity = GetEntity(); });

			var objectSubtype = new ObjectSubtype(entity, null);

			ObjectSubtypeItem item = _modelBuilder.CreateObjectSubtypeItem(objectSubtype);

			AddChild(item);

			item.NotifyChanged();

			return item;
		}
	}
}
