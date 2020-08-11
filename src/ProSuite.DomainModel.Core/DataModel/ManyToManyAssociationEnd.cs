using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class ManyToManyAssociationEnd : AssociationEnd
	{
		[UsedImplicitly] private readonly AssociationAttribute _foreignKey;
		[UsedImplicitly] private ObjectAttribute _primaryKey;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ManyToManyAssociationEnd"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected ManyToManyAssociationEnd() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ManyToManyAssociationEnd"/> class.
		/// </summary>
		/// <param name="association">The association.</param>
		/// <param name="foreignKey">The foreign key.</param>
		/// <param name="primaryKey">The primary key.</param>
		public ManyToManyAssociationEnd([NotNull] AttributedAssociation association,
		                                [NotNull] AssociationAttribute foreignKey,
		                                [NotNull] ObjectAttribute primaryKey)
			: base(association, primaryKey.Dataset, false)
		{
			Assert.ArgumentNotNull(foreignKey, nameof(foreignKey));
			Assert.ArgumentNotNull(primaryKey, nameof(primaryKey));

			_foreignKey = foreignKey;
			_primaryKey = primaryKey;
		}

		#endregion

		public override bool HasForeignKey => true;

		public override bool HasPrimaryKey => true;

		public override Attribute ForeignKey => _foreignKey;

		public override ObjectAttribute PrimaryKey => _primaryKey;

		protected override bool CanChangeDocumentAssociationEditCore => true;

		protected override bool CanChangeCascadeDeletionCore => false;

		public AttributedAssociation AttributedAssociation =>
			Association as AttributedAssociation;

		public bool IsDestinationEnd => Equals(Association.DestinationEnd, this);

		public bool IsOriginEnd => Equals(Association.OriginEnd, this);

		public void Redirect([NotNull] ObjectAttribute primaryKey)
		{
			Assert.ArgumentNotNull(primaryKey, nameof(primaryKey));

			_primaryKey = primaryKey;

			Redirect(primaryKey.Dataset);
		}
	}
}