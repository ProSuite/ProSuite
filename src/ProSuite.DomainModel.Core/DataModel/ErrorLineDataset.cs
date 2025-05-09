namespace ProSuite.DomainModel.Core.DataModel
{
	public class ErrorLineDataset : ErrorVectorDataset
	{
		#region Constructors

		/// <remarks>Required for NHibernate</remarks>
		protected ErrorLineDataset() { }

		public ErrorLineDataset(string name) : base(name) { }

		public ErrorLineDataset(string name, string abbreviation)
			: base(name, abbreviation) { }

		public ErrorLineDataset(string name, string abbreviation, string aliasName)
			: base(name, abbreviation, aliasName) { }

		#endregion

		public override string TypeDescription => "Error Lines";

		public override DatasetImplementationType ImplementationType =>
			DatasetImplementationType.ErrorLine;
	}
}
