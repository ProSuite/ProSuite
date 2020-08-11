using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class AssociationAttribute : Attribute
	{
		[UsedImplicitly] private AttributedAssociation _association;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="AssociationAttribute"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		[UsedImplicitly]
		protected AssociationAttribute() { }

		public AssociationAttribute([NotNull] string name, FieldType fieldType)
			: base(name, fieldType) { }

		#endregion

		public override DdxModel Model => _association?.Model;

		public AttributedAssociation Association
		{
			get { return _association; }
			internal set { _association = value; }
		}

		protected override bool IsTableDeleted =>
			_association != null && _association.Deleted;
	}
}