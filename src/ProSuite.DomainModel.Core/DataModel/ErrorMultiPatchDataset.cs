namespace ProSuite.DomainModel.Core.DataModel
{
	public class ErrorMultiPatchDataset : ErrorVectorDataset
	{
		#region Constructors

		/// <remarks>Required for NHibernate</remarks>
		public ErrorMultiPatchDataset() { }

		public ErrorMultiPatchDataset(string name) : base(name) { }

		public ErrorMultiPatchDataset(string name, string abbreviation)
			: base(name, abbreviation) { }

		public ErrorMultiPatchDataset(string name, string abbreviation, string aliasName)
			: base(name, abbreviation, aliasName) { }

		#endregion

		public override string TypeDescription => "Error MultiPatches";

		public override DatasetImplementationType ImplementationType =>
			DatasetImplementationType.ErrorMultiPatch;
	}
}
