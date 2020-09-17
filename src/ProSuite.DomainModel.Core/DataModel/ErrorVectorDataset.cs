namespace ProSuite.DomainModel.Core.DataModel
{
	public abstract class ErrorVectorDataset : VectorDataset, IErrorDataset
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ErrorVectorDataset"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected ErrorVectorDataset() { }

		protected ErrorVectorDataset(string name) : base(name) { }

		protected ErrorVectorDataset(string name, string abbreviation)
			: base(name, abbreviation) { }

		protected ErrorVectorDataset(string name, string abbreviation, string aliasName)
			: base(name, abbreviation, aliasName) { }

		#endregion
	}
}