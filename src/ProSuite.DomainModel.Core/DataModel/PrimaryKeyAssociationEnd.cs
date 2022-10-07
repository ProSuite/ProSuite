using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class PrimaryKeyAssociationEnd : AssociationEnd
	{
		[UsedImplicitly] private ObjectAttribute _primaryKey;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="PrimaryKeyAssociationEnd"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		[UsedImplicitly]
		protected PrimaryKeyAssociationEnd() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="PrimaryKeyAssociationEnd"/> class.
		/// </summary>
		/// <param name="association">The association.</param>
		/// <param name="primaryKey">The primary key.</param>
		public PrimaryKeyAssociationEnd([NotNull] Association association,
		                                [NotNull] ObjectAttribute primaryKey)
			: base(association, primaryKey.Dataset, false)
		{
			_primaryKey = primaryKey;
		}

		#endregion

		public void Redirect([NotNull] ObjectAttribute primaryKey)
		{
			Assert.ArgumentNotNull(primaryKey, nameof(primaryKey));

			_primaryKey = primaryKey;

			Redirect(primaryKey.Dataset);
		}

		public override bool HasForeignKey => false;

		public override bool HasPrimaryKey => true;

		public override Attribute ForeignKey => null;

		public override ObjectAttribute PrimaryKey => _primaryKey;

		protected override bool CanChangeDocumentAssociationEditCore => false;

		protected override bool CanChangeCascadeDeletionCore =>
			Association is ForeignKeyAssociation;
	}
}
