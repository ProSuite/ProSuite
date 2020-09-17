using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public interface IObjectDataset : IDdxDataset
	{
		/// <summary>
		/// Gets a value indicating whether the object class has geometry.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if the object dataset has geometry; otherwise, <c>false</c>.
		/// </value>
		bool HasGeometry { get; }

		/// <summary>
		/// Gets or sets the display format used for displaying 
		/// objects in this dataset.
		/// </summary>
		/// <value>The display format.</value>
		[CanBeNull]
		[UsedImplicitly]
		string DisplayFormat { get; set; }

		[NotNull]
		IList<ObjectAttribute> Attributes { get; }

		[NotNull]
		IList<AssociationEnd> AssociationEnds { get; }

		[NotNull]
		IList<ObjectType> ObjectTypes { get; }

		[CanBeNull]
		Association GetAssociation([NotNull] ObjectDataset associatedDataset,
		                           bool includeDeleted = false);

		[CanBeNull]
		Association GetAssociation([NotNull] string associationName);

		[CanBeNull]
		AssociationEnd GetAssociationEnd([NotNull] ObjectDataset associatedDataset,
		                                 bool includeDeleted = false);

		[CanBeNull]
		AssociationEnd GetAssociationEnd([NotNull] string associationName);

		[NotNull]
		IEnumerable<AssociationEnd> GetAssociationEnds(bool includeDeleted = false);

		[CanBeNull]
		ObjectType GetObjectType(int subtypeCode);

		/// <summary>
		/// Gets the object types for this dataset.
		/// </summary>
		[NotNull]
		IEnumerable<ObjectType> GetObjectTypes(bool includeDeleted = false);

		/// <summary>
		/// Gets the attribute for a given name.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="includeDeleted">Indicates if deleted attributes should be returned</param>
		/// <returns>The attribute having the name, or <c>null</c> if no such attribute
		/// exists or if it is marked as Deleted and includeDeleted is <c>false</c>.</returns>
		[CanBeNull]
		ObjectAttribute GetAttribute([NotNull] string name, bool includeDeleted = false);

		/// <summary>
		/// Gets the attribute that has a given special role in the dataset.
		/// </summary>
		/// <param name="role">The attribute role.</param>
		/// <returns>Attribute instance, or null if no attribute in the dataset 
		/// has the specified role. If the attribute that has the role is deleted, 
		/// null is returned also (Deleted attributes are considered not having a role).</returns>
		/// <exception cref="ArgumentException">Invalid role specified.</exception>
		[CanBeNull]
		ObjectAttribute GetAttribute([NotNull] AttributeRole role);

		[NotNull]
		IEnumerable<ObjectAttribute> GetAttributes(bool includeDeleted = false);
	}
}