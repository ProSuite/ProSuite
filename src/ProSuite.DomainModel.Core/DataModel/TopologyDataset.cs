using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public abstract class TopologyDataset : Dataset, ITopologyDataset
	{
		[UsedImplicitly] private LayerFile _defaultSymbology;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="TopologyDataset"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected TopologyDataset() { }

		protected TopologyDataset(string name) : base(name) { }

		protected TopologyDataset(string name, string abbreviation)
			: base(name, abbreviation) { }

		protected TopologyDataset(string name, string abbreviation, string aliasName)
			: base(name, abbreviation, aliasName) { }

		#endregion

		public override string TypeDescription => "Topology";

		#region ISpatialDataset

		public LayerFile DefaultLayerFile
		{
			get { return _defaultSymbology; }
			set { _defaultSymbology = value; }
		}

		[UsedImplicitly]
		private LayerFile DefaultSymbology => _defaultSymbology;

		#endregion
	}
}
