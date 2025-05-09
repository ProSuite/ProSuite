namespace ProSuite.DomainModel.Core.DataModel
{
	public class ErrorTableDataset : TableDataset, IErrorDataset
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ErrorTableDataset"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected ErrorTableDataset() { }

		public ErrorTableDataset(string name) : base(name) { }

		public ErrorTableDataset(string name, string abbreviation)
			: base(name, abbreviation) { }

		public ErrorTableDataset(string name, string abbreviation, string aliasName)
			: base(name, abbreviation, aliasName) { }

		#endregion

		public override string TypeDescription => "Error Table";

		public override DatasetImplementationType ImplementationType =>
			DatasetImplementationType.ErrorTable;
	}
}
