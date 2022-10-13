using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class ForeignKeyAssociationEnd : AssociationEnd
	{
		[UsedImplicitly] private ObjectAttribute _foreignKey;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ForeignKeyAssociationEnd"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		[UsedImplicitly]
		protected ForeignKeyAssociationEnd() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ForeignKeyAssociationEnd"/> class.
		/// </summary>
		/// <param name="association">The association.</param>
		/// <param name="foreignKey">The foreign key.</param>
		public ForeignKeyAssociationEnd([NotNull] Association association,
		                                [NotNull] ObjectAttribute foreignKey)
			: base(association, foreignKey.Dataset, true)
		{
			_foreignKey = foreignKey;
		}

		#endregion

		public void Redirect([NotNull] ObjectAttribute foreignKey)
		{
			_foreignKey = foreignKey;

			Redirect(foreignKey.Dataset);
		}

		public override bool HasForeignKey => true;

		public override bool HasPrimaryKey => false;

		public override Attribute ForeignKey => _foreignKey;

		public override ObjectAttribute PrimaryKey => null;

		protected override bool CanChangeDocumentAssociationEditCore => false;

		protected override bool CanChangeCascadeDeletionCore => true;
	}
}
