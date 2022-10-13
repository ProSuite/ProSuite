using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	/// <summary>
	/// An object category that represents a GDB subtype
	/// </summary>
	public class ObjectType : ObjectCategory
	{
		private int _subtypeCode;

		private readonly IList<ObjectSubtype> _objectSubtypes =
			new List<ObjectSubtype>();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectType"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected ObjectType() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectType"/> class.
		/// </summary>
		/// <param name="objectDataset">The object dataset.</param>
		/// <param name="subtypeCode">The subtype code.</param>
		/// <param name="subtypeName">Name of the subtype.</param>
		public ObjectType([NotNull] ObjectDataset objectDataset, int subtypeCode,
		                  string subtypeName) : base(objectDataset, subtypeName)
		{
			_subtypeCode = subtypeCode;
		}

		#endregion

		public override bool CanChangeName => false;

		public override int SubtypeCode => _subtypeCode;

		[NotNull]
		public IList<ObjectSubtype> ObjectSubtypes
			=> new ReadOnlyList<ObjectSubtype>(_objectSubtypes);

		[NotNull]
		public ObjectSubtype AddObjectSubType([NotNull] string name,
		                                      [NotNull] string attributeName,
		                                      [CanBeNull] object attributeValue,
		                                      VariantValueType valueType =
			                                      VariantValueType.Null)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));
			Assert.ArgumentNotNullOrEmpty(attributeName, nameof(attributeName));

			ObjectAttribute attribute = ObjectDataset.GetAttribute(attributeName);
			Assert.NotNull(attribute, "attribute '{0}' not found in '{1}' ",
			               attributeName, ObjectDataset.Name);

			return AddObjectSubType(name, attribute, attributeValue, valueType);
		}

		[NotNull]
		public ObjectSubtype AddObjectSubType([NotNull] string name,
		                                      [NotNull] ObjectAttribute attribute,
		                                      [CanBeNull] object attributeValue,
		                                      VariantValueType valueType =
			                                      VariantValueType.Null)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));
			Assert.ArgumentNotNull(attribute, nameof(attribute));

			ObjectSubtype objectSubtype = AddObjectSubType(name);

			objectSubtype.AddCriterion(attribute, attributeValue, valueType);

			return objectSubtype;
		}

		[NotNull]
		public ObjectSubtype AddObjectSubType([CanBeNull] string name = null)
		{
			var objectSubtype = new ObjectSubtype(this, name);

			return AddObjectSubType(objectSubtype);
		}

		[NotNull]
		public ObjectSubtype AddObjectSubType([NotNull] ObjectSubtype objectSubtype)
		{
			Assert.ArgumentNotNull(objectSubtype, nameof(objectSubtype));
			Assert.ArgumentCondition(Equals(objectSubtype.ObjectType),
			                         "ObjectSubtype does not belong to this ObjectType");

			int index = _objectSubtypes.Count;

			_objectSubtypes.Add(objectSubtype);

			objectSubtype.SortOrder = index;

			return objectSubtype;
		}

		// TODO move up / move down methods on _objectSubtypes (modify SortOrder)

		public bool RemoveObjectSubtype([NotNull] ObjectSubtype objectSubtype)
		{
			Assert.ArgumentNotNull(objectSubtype, nameof(objectSubtype));

			return _objectSubtypes.Remove(objectSubtype);
		}

		#region Non-public members

		/// <summary>
		/// Updates the name. To be used during harvesting of the subtype properties
		/// </summary>
		/// <param name="name">The name.</param>
		public void UpdateName(string name)
		{
			SetName(name);
		}

		public void UpdateSubtypeCode(int subtypeCode)
		{
			_subtypeCode = subtypeCode;
		}

		#endregion
	}
}
