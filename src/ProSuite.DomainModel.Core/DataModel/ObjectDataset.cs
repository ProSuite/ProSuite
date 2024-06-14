using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.DomainModel.Core.DataModel
{
	public abstract class ObjectDataset : Dataset, IObjectDataset, IAttributes, ITableSchemaDef
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[UsedImplicitly] private readonly IList<AssociationEnd> _associationEnds =
			new List<AssociationEnd>();

		[UsedImplicitly] private readonly IList<ObjectAttribute> _attributes =
			new List<ObjectAttribute>();

		[UsedImplicitly] private readonly IList<ObjectType> _objectTypes =
			new List<ObjectType>();

		[UsedImplicitly] private string _displayFormat;

		private IDictionary<string, ObjectAttribute> _attributesByName;
		private IDictionary<AttributeRole, ObjectAttribute> _attributesByRole;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectDataset"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected ObjectDataset() { }

		protected ObjectDataset([NotNull] string name) : base(name) { }

		protected ObjectDataset([NotNull] string name,
		                        [CanBeNull] string abbreviation)
			: base(name, abbreviation) { }

		protected ObjectDataset([NotNull] string name,
		                        [CanBeNull] string abbreviation,
		                        [CanBeNull] string aliasName)
			: base(name, abbreviation, aliasName) { }

		#endregion

		#region IObjectDataset implementation

		public override string TypeDescription => "Object Dataset";

		/// <summary>
		/// Gets a value indicating whether the object class has geometry.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if the object dataset has geometry; otherwise, <c>false</c>.
		/// </value>
		public abstract bool HasGeometry { get; }

		public string DisplayFormat
		{
			get { return _displayFormat; }
			set { _displayFormat = value; }
		}

		public IList<ObjectAttribute> Attributes => new ReadOnlyList<ObjectAttribute>(_attributes);

		public IEnumerable<ObjectAttribute> GetAttributes(bool includeDeleted = false)
		{
			foreach (ObjectAttribute attribute in _attributes)
			{
				if (includeDeleted || ! attribute.Deleted)
				{
					yield return attribute;
				}
			}
		}

		/// <summary>
		/// Gets the attribute that has a given special role in the dataset.
		/// </summary>
		/// <param name="role">The attribute role. A non-standard role is expected.</param>
		/// <returns>
		/// Attribute instance, or null if no attribute in the dataset
		/// has the specified role.
		/// </returns>
		/// <exception cref="ArgumentException">Invalid role specified.</exception>
		public ObjectAttribute GetAttribute(AttributeRole role)
		{
			Assert.ArgumentNotNull(role, nameof(role));

			ObjectAttribute attribute;
			return AttributesByRole.TryGetValue(role, out attribute)
				       ? attribute
				       : null;
		}

		public ObjectAttribute GetAttribute(string name, bool includeDeleted = false)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			ObjectAttribute attribute;
			if (AttributesByName.TryGetValue(name, out attribute))
			{
				if (includeDeleted || ! attribute.Deleted)
				{
					return attribute;
				}
			}

			return null;
		}

		public Association GetAssociation(ObjectDataset associatedDataset,
		                                  bool includeDeleted = false)
		{
			// TODO: consider using dictionary
			foreach (AssociationEnd associationEnd in _associationEnds)
			{
				if (associationEnd.OppositeEnd.ObjectDataset.Equals(associatedDataset) &&
				    (includeDeleted || ! associationEnd.Deleted))
				{
					return associationEnd.Association;
				}
			}

			return null;
		}

		public Association GetAssociation(string associationName)
		{
			// TODO: consider using dictionary
			foreach (AssociationEnd associationEnd in _associationEnds)
			{
				if (associationEnd.Association.Name.Equals(
					    associationName, StringComparison.OrdinalIgnoreCase))
				{
					return associationEnd.Association;
				}
			}

			return null;
		}

		public AssociationEnd GetAssociationEnd(ObjectDataset associatedDataset,
		                                        bool includeDeleted = false)
		{
			// TODO: consider using hashtable
			foreach (AssociationEnd associationEnd in _associationEnds)
			{
				if (associationEnd.OppositeEnd.ObjectDataset.Equals(associatedDataset) &&
				    (includeDeleted || ! associationEnd.Deleted))
				{
					return associationEnd;
				}
			}

			return null;
		}

		public AssociationEnd GetAssociationEnd(string associationName)
		{
			// TODO: consider using hashtable
			foreach (AssociationEnd associationEnd in _associationEnds)
			{
				if (associationEnd.Association.Name.Equals(
					    associationName, StringComparison.OrdinalIgnoreCase))
				{
					return associationEnd;
				}
			}

			return null;
		}

		public IEnumerable<AssociationEnd> GetAssociationEnds(bool includeDeleted = false)
		{
			foreach (AssociationEnd end in _associationEnds)
			{
				if (includeDeleted || ! end.Deleted)
				{
					yield return end;
				}
			}
		}

		public IList<AssociationEnd> AssociationEnds =>
			new ReadOnlyList<AssociationEnd>(_associationEnds);

		public ObjectType GetObjectType(int subtypeCode)
		{
			return
				_objectTypes.FirstOrDefault(objectType => objectType.SubtypeCode == subtypeCode);
		}

		[CanBeNull]
		public ObjectType GetObjectType([NotNull] string name)
		{
			return _objectTypes.FirstOrDefault(
				candidate => string.Equals(candidate.Name, name));
		}

		public IEnumerable<ObjectType> GetObjectTypes(bool includeDeleted = false)
		{
			foreach (ObjectType objectType in _objectTypes)
			{
				if (includeDeleted || ! objectType.Deleted)
				{
					yield return objectType;
				}
			}
		}

		public IList<ObjectType> ObjectTypes => new ReadOnlyList<ObjectType>(_objectTypes);

		#endregion

		#region IAttributes implementation

		IList<Attribute> IAttributes.Attributes
		{
			get
			{
				// need to cast up item by item
				var result = new List<Attribute>(_attributes.Count);

				foreach (ObjectAttribute attribute in _attributes)
				{
					result.Add(attribute);
				}

				return result;
			}
		}

		Attribute IAttributes.GetAttribute(string name, bool includeDeleted)
		{
			return GetAttribute(name, includeDeleted);
		}

		#endregion

		#region Attributes

		[NotNull]
		public ObjectAttribute AddAttribute([NotNull] ObjectAttribute attribute)
		{
			Assert.ArgumentNotNull(attribute, nameof(attribute));

			// validate attribute
			if (string.IsNullOrEmpty(attribute.Name))
			{
				throw new ArgumentException("Attribute has no name");
			}

			if (attribute.Dataset != null)
			{
				throw new ArgumentException("Attribute already assigned to dataset");
			}

			foreach (ObjectAttribute existingAttribute in _attributes)
			{
				if (string.Compare(existingAttribute.Name, attribute.Name,
				                   StringComparison.OrdinalIgnoreCase) == 0)
				{
					throw new ArgumentException(
						string.Format("Attribute with same name already exists: {0}",
						              attribute.Name), nameof(attribute));
				}

				if (attribute.Role != null && existingAttribute.Role == attribute.Role)
				{
					throw new ArgumentException(
						string.Format(
							"Attribute with same role already exists: {0} role: {1}",
							existingAttribute.Name,
							Enum.GetName(typeof(AttributeRole), attribute.Role)),
						nameof(attribute));
				}
			}

			ClearAttributeMaps();

			attribute.Dataset = this;

			_attributes.Add(attribute);

			_msg.VerboseDebug(() => $"Added attribute {attribute.Name}");

			return attribute;
		}

		public void RemoveAttribute([NotNull] ObjectAttribute attribute)
		{
			ClearAttributeMaps();

			_attributes.Remove(attribute);
			attribute.Dataset = null;
		}

		public void ClearAttributeMaps()
		{
			_attributesByName = null;
			_attributesByRole = null;
		}

		#endregion

		#region Association ends

		[NotNull]
		internal AssociationEnd AddAssociationEnd(AssociationEnd associationEnd)
		{
			if (_associationEnds.Contains(associationEnd))
			{
				throw new ArgumentException("Association end already exists in collection");
			}

			_associationEnds.Add(associationEnd);

			return associationEnd;
		}

		internal void RemoveAssociationEnd([NotNull] AssociationEnd associationEnd)
		{
			Assert.ArgumentNotNull(associationEnd, nameof(associationEnd));

			_associationEnds.Remove(associationEnd);
		}

		#endregion

		#region Object Types

		[NotNull]
		public ObjectType AddObjectType(int subtypeCode, [NotNull] string name)
		{
			int index = _objectTypes.Count;
			return AddObjectType(subtypeCode, name, index);
		}

		[NotNull]
		public ObjectType AddObjectType(int subtypeCode, [NotNull] string name, int index)
		{
			// check if subtype code already exists
			if (GetObjectType(subtypeCode) != null)
			{
				throw new ArgumentException(
					string.Format(
						"Subtype code already present in collection: {0}",
						subtypeCode));
			}

			// check if subtype name already exists
			if (GetObjectType(name) != null)
			{
				throw new ArgumentException(
					string.Format(
						"Subtype name already present in collection: {0}",
						name));
			}

			var objectType = new ObjectType(this, subtypeCode, name);

			_objectTypes.Insert(index, objectType);

			return objectType;
		}

		public void RemoveObjectType([NotNull] ObjectType objectType)
		{
			_objectTypes.Remove(objectType);
		}

		#endregion

		#region Non-public members

		/// <summary>
		/// Creates the object attribute. Template method to allow instantiation of
		/// dataset-specific ObjectAttribute subclass.
		/// </summary>
		/// <returns></returns>
		[NotNull]
		[PublicAPI]
		// ReSharper disable once VirtualMemberNeverOverridden.Global
		public virtual ObjectAttribute CreateObjectAttribute(
			[NotNull] string name, FieldType fieldType)
		{
			return new ObjectAttribute(name, fieldType);
		}

		[NotNull]
		private IDictionary<string, ObjectAttribute> AttributesByName
		{
			get
			{
				if (_attributesByName == null)
				{
					_attributesByName = new Dictionary<string, ObjectAttribute>(
						StringComparer.OrdinalIgnoreCase);
					foreach (ObjectAttribute attribute in _attributes)
					{
						_attributesByName.Add(attribute.Name, attribute);
					}
				}

				return _attributesByName;
			}
		}

		[NotNull]
		private IDictionary<AttributeRole, ObjectAttribute> AttributesByRole
		{
			get
			{
				if (_attributesByRole == null)
				{
					_attributesByRole = new Dictionary<AttributeRole, ObjectAttribute>();

					foreach (ObjectAttribute attribute in _attributes)
					{
						// only include non-deleted attributes
						if (attribute.Role != null && ! attribute.Deleted)
						{
							_attributesByRole.Add(attribute.Role, attribute);
						}
					}
				}

				return _attributesByRole;
			}
		}

		internal void AttributeChanged([NotNull] ObjectAttribute attribute)
		{
			ClearAttributeMaps();
		}

		#endregion

		#region Implementation of IDbTableSchema

		public IReadOnlyList<ITableField> TableFields
			=> (IReadOnlyList<ITableField>) GetAttributes();

		public bool HasOID => _attributes.Any(a => a.FieldType == FieldType.ObjectID);

		public string OIDFieldName =>
			_attributes.FirstOrDefault(a => a.FieldType == FieldType.ObjectID)?.Name;

		public int FindField(string fieldName)
		{
			for (int i = 0; i < _attributes.Count; i++)
			{
				ObjectAttribute field = _attributes[i];

				if (field.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
				{
					return i;
				}
			}

			return -1;
		}

		#endregion
	}
}
