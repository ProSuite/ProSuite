using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public abstract class TableDataset : ObjectDataset, ITableDataset
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="TableDataset"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected TableDataset() { }

		protected TableDataset([NotNull] string name) : base(name) { }

		protected TableDataset([NotNull] string name,
		                       [CanBeNull] string abbreviation)
			: base(name, abbreviation) { }

		protected TableDataset([NotNull] string name,
		                       [CanBeNull] string abbreviation,
		                       [CanBeNull] string aliasName)
			: base(name, abbreviation, aliasName) { }

		#endregion

		public override bool HasGeometry => false;

		public override DatasetType DatasetType => DatasetType.Table;
	}
}
