using ProSuite.Commons.Essentials.Assertions;
using ProSuite.DdxEditor.Framework.Dependencies;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.ObjectCategories
{
	public class ObjectSubtypeToObjectTypeDependingItem : DependingItem
	{
		private readonly ObjectType _objectType;
		private readonly ObjectSubtype _objectSubtype;

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectSubtypeToObjectTypeDependingItem"/> class.
		/// </summary>
		/// <param name="objectType">The object type to which the dependency exists.</param>
		/// <param name="objectSubtype">The object subtype that is dependent on the object type.</param>
		public ObjectSubtypeToObjectTypeDependingItem(ObjectType objectType,
		                                              ObjectSubtype objectSubtype)
			: base(objectType, objectType.Name)
		{
			Assert.ArgumentNotNull(objectType, nameof(objectType));
			Assert.ArgumentNotNull(objectSubtype, nameof(objectSubtype));

			_objectType = objectType;
			_objectSubtype = objectSubtype;
		}

		protected override void RemoveDependencyCore()
		{
			_objectType.RemoveObjectSubtype(_objectSubtype);
		}

		public override bool RequiresConfirmation => false;

		public override bool RemovedByCascadingDeletion => false;
	}
}
