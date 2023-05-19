using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Db;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class AttributedAssociation : Association, IAttributes
	{
		private readonly IList<AssociationAttribute> _attributes =
			new List<AssociationAttribute>();

		private IDictionary<string, AssociationAttribute> _attributesByName;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="AttributedAssociation"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected AttributedAssociation() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="AttributedAssociation"/> class.
		/// </summary>
		/// <param name="name">The association name.</param>
		/// <param name="cardinality">The cardinality of the association. Note this may also be 1:1 or 1:n, 
		/// as these might also be defined as 'attributed'.</param>
		/// <param name="destinationForeignKeyName">Name of the destination foreign key.</param>
		/// <param name="destinationForeignKeyType">Type of the destination foreign key.</param>
		/// <param name="destinationPrimaryKey">The destination primary key.</param>
		/// <param name="originForeignKeyName">Name of the origin foreign key.</param>
		/// <param name="originForeignKeyType">Type of the origin foreign key.</param>
		/// <param name="originPrimaryKey">The origin primary key.</param>
		public AttributedAssociation([NotNull] string name,
		                             AssociationCardinality cardinality,
		                             [NotNull] string destinationForeignKeyName,
		                             FieldType destinationForeignKeyType,
		                             [NotNull] ObjectAttribute destinationPrimaryKey,
		                             [NotNull] string originForeignKeyName,
		                             FieldType originForeignKeyType,
		                             [NotNull] ObjectAttribute originPrimaryKey)
			: base(name, cardinality)
		{
			Assert.ArgumentNotNullOrEmpty(destinationForeignKeyName,
			                              nameof(destinationForeignKeyName));
			Assert.ArgumentNotNullOrEmpty(originForeignKeyName, nameof(originForeignKeyName));
			Assert.NotNullOrEmpty(destinationPrimaryKey.Name);
			Assert.NotNullOrEmpty(originPrimaryKey.Name);

			AssociationAttribute destinationForeignKey = AddAttribute(
				destinationForeignKeyName, destinationForeignKeyType);
			AssociationAttribute originForeignKey = AddAttribute(
				originForeignKeyName, originForeignKeyType);

			// TODO should be renamed to AttributedAssociationEnd! cardinality may also be 1:1 or 1:n
			DestinationEnd = new ManyToManyAssociationEnd(this, destinationForeignKey,
			                                              destinationPrimaryKey);
			OriginEnd = new ManyToManyAssociationEnd(this, originForeignKey,
			                                         originPrimaryKey);
		}

		#endregion

		public override bool IsAttributed => true;

		[NotNull]
		public IEnumerable<AssociationAttribute> GetAttributes(bool includeDeleted = false)
		{
			foreach (AssociationAttribute attribute in _attributes)
			{
				if (includeDeleted || ! attribute.Deleted)
				{
					yield return attribute;
				}
			}
		}

		[NotNull]
		public IList<AssociationAttribute> Attributes =>
			new ReadOnlyList<AssociationAttribute>(_attributes);

		[NotNull]
		public AssociationAttribute AddAttribute([NotNull] string name,
		                                         FieldType fieldType)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			foreach (AssociationAttribute existingAttribute in _attributes)
			{
				if (string.Equals(existingAttribute.Name, name,
				                  StringComparison.OrdinalIgnoreCase))
				{
					throw new ArgumentException(
						string.Format("Attribute with same name already exists: {0}",
						              name), nameof(name));
				}
			}

			ClearAttributeMaps();

			var attribute = new AssociationAttribute(name, fieldType)
			                {Association = this};

			_attributes.Add(attribute);

			_msg.DebugFormat("Added attribute {0} to attributed association {1}", name, Name);

			return attribute;
		}

		[CanBeNull]
		public AssociationAttribute GetAttribute([NotNull] string name,
		                                         bool includeDeleted = false)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			AssociationAttribute attribute;
			if (AttributesByName.TryGetValue(name, out attribute))
			{
				if (includeDeleted || ! attribute.Deleted)
				{
					return attribute;
				}
			}

			return null;
		}

		public void RemoveAttribute([NotNull] AssociationAttribute attribute)
		{
			Assert.ArgumentNotNull(attribute, nameof(attribute));

			ClearAttributeMaps();

			_attributes.Remove(attribute);
			attribute.Association = null;
		}

		public void ClearAttributeMaps()
		{
			_attributesByName = null;
		}

		#region IAttributes Members

		IList<Attribute> IAttributes.Attributes => _attributes.Cast<Attribute>().ToList();

		Attribute IAttributes.GetAttribute(string name, bool includeDeleted)
		{
			return GetAttribute(name, includeDeleted);
		}

		#endregion

		#region Non-public members

		protected override bool IsValidCardinality(AssociationCardinality cardinality)
		{
			switch (cardinality)
			{
				case AssociationCardinality.Unknown:
					return false;

				case AssociationCardinality.OneToOne:
				case AssociationCardinality.OneToMany:
				case AssociationCardinality.ManyToMany:
					return true;

				default:
					throw new ArgumentOutOfRangeException(nameof(cardinality), cardinality,
					                                      @"Unexpected cardinality");
			}
		}

		[NotNull]
		private IDictionary<string, AssociationAttribute> AttributesByName
		{
			get
			{
				if (_attributesByName == null)
				{
					_attributesByName = new Dictionary<string, AssociationAttribute>(
						StringComparer.OrdinalIgnoreCase);
					foreach (AssociationAttribute attribute in _attributes)
					{
						_attributesByName.Add(attribute.Name, attribute);
					}
				}

				return _attributesByName;
			}
		}

		#endregion
	}
}
