using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class ForeignKeyAssociation : Association
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ForeignKeyAssociation"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected ForeignKeyAssociation() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ForeignKeyAssociation"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="cardinality">The association cardinality.</param>
		/// <param name="foreignKey">The foreign key (on destination table).</param>
		/// <param name="primaryKey">The primary key (on origin table).</param>
		public ForeignKeyAssociation([NotNull] string name,
		                             AssociationCardinality cardinality,
		                             [NotNull] ObjectAttribute foreignKey,
		                             [NotNull] ObjectAttribute primaryKey)
			: base(name, cardinality)
		{
			Assert.ArgumentNotNull(foreignKey, nameof(foreignKey));
			Assert.ArgumentNotNull(primaryKey, nameof(primaryKey));

			Assert.NotNullOrEmpty(foreignKey.Name, "fk name is null");
			Assert.NotNullOrEmpty(primaryKey.Name, "pk name is null");

			DestinationEnd = new ForeignKeyAssociationEnd(this, foreignKey);
			OriginEnd = new PrimaryKeyAssociationEnd(this, primaryKey);
		}

		#endregion

		public override bool IsAttributed => false;

		[NotNull]
		[PublicAPI]
		public ForeignKeyAssociationEnd ForeignKeyEnd =>
			(ForeignKeyAssociationEnd) DestinationEnd;

		[NotNull]
		[PublicAPI]
		public PrimaryKeyAssociationEnd PrimaryKeyEnd =>
			(PrimaryKeyAssociationEnd) OriginEnd;

		protected override bool IsValidCardinality(AssociationCardinality cardinality)
		{
			switch (cardinality)
			{
				case AssociationCardinality.Unknown:
				case AssociationCardinality.ManyToMany:
					return false;

				case AssociationCardinality.OneToOne:
				case AssociationCardinality.OneToMany:
					return true;

				default:
					throw new ArgumentOutOfRangeException(nameof(cardinality), cardinality,
					                                      @"Unexpected cardinality");
			}
		}
	}
}
