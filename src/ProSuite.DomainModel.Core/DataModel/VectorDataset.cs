using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public abstract class VectorDataset : ObjectDataset, IVectorDataset
	{
		[UsedImplicitly] private LayerFile _defaultSymbology;

		[UsedImplicitly] private double? _minimumSegmentLength; // name should be ..Override

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="VectorDataset"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected VectorDataset() { }

		protected VectorDataset([NotNull] string name) : base(name) { }

		protected VectorDataset([NotNull] string name,
		                        [CanBeNull] string abbreviation)
			: base(name, abbreviation) { }

		protected VectorDataset([NotNull] string name,
		                        [CanBeNull] string abbreviation,
		                        [CanBeNull] string aliasName)
			: base(name, abbreviation, aliasName) { }

		#endregion

		[UsedImplicitly]
		private LayerFile DefaultSymbology => _defaultSymbology;

		#region IVectorDataset

		public double MinimumSegmentLength
		{
			get { return _minimumSegmentLength ?? Model.DefaultMinimumSegmentLength; }
			set { _minimumSegmentLength = value; }
		}

		public double? MinimumSegmentLengthOverride
		{
			get { return _minimumSegmentLength; }
			set { _minimumSegmentLength = value; }
		}

		public override bool HasGeometry => true;

		public LayerFile DefaultLayerFile
		{
			get { return _defaultSymbology; }
			set { _defaultSymbology = value; }
		}

		#endregion
	}
}
