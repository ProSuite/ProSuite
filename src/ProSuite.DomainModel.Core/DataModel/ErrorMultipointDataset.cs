namespace ProSuite.DomainModel.Core.DataModel
{
	public class ErrorMultipointDataset : ErrorVectorDataset
	{
		#region Constructors

		/// <remarks>Required for NHibernate</remarks>
		public ErrorMultipointDataset() { }

		public ErrorMultipointDataset(string name) : base(name) { }

		public ErrorMultipointDataset(string name, string abbreviation)
			: base(name, abbreviation) { }

		public ErrorMultipointDataset(string name, string abbreviation, string aliasName)
			: base(name, abbreviation, aliasName) { }

		#endregion

		public override string TypeDescription => "Error Multipoints";

		public override DatasetImplementationType ImplementationType =>
			DatasetImplementationType.ErrorMultipoint;
	}
}
