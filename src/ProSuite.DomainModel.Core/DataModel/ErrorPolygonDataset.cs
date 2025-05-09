namespace ProSuite.DomainModel.Core.DataModel
{
	public class ErrorPolygonDataset : ErrorVectorDataset
	{
		#region Constructors

		/// <remarks>Required for NHibernate</remarks>
		protected ErrorPolygonDataset() { }

		public ErrorPolygonDataset(string name) : base(name) { }

		public ErrorPolygonDataset(string name, string abbreviation)
			: base(name, abbreviation) { }

		public ErrorPolygonDataset(string name, string abbreviation, string aliasName)
			: base(name, abbreviation, aliasName) { }

		#endregion

		public override string TypeDescription => "Error Polygons";

		public override DatasetImplementationType ImplementationType =>
			DatasetImplementationType.ErrorPolygon;
	}
}
