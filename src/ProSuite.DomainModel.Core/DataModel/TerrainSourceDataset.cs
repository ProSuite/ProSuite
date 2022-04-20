using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class TerrainSourceDataset
	{
		[UsedImplicitly] private IVectorDataset _dataset;
		[UsedImplicitly] private string _whereClause;
		[UsedImplicitly] private TinSurfaceType _surfaceFeatureType;

		public TerrainSourceDataset() { }

		public TerrainSourceDataset([NotNull] VectorDataset dataset,
		                            TinSurfaceType surfaceFeatureType,
		                            [CanBeNull] string whereClause = null)
		{
			Assert.NotNull(dataset, nameof(dataset));

			_dataset = dataset;
			_surfaceFeatureType = surfaceFeatureType;
			_whereClause = whereClause;
		}

		[NotNull]
		public IVectorDataset Dataset => _dataset;

		[CanBeNull]
		public string WhereClause
		{
			get { return _whereClause; }
			set { _whereClause = value; }
		}

		public TinSurfaceType SurfaceFeatureType
		{
			get { return _surfaceFeatureType; }
			set { _surfaceFeatureType = value; }
		}

		public bool Equals(TerrainSourceDataset other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return Equals(_dataset.Id, other._dataset.Id) &&
			       _whereClause == other._whereClause &&
			       _surfaceFeatureType == other._surfaceFeatureType;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((TerrainSourceDataset) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((_dataset != null ? _dataset.GetHashCode() : 0) * 397) ^
				       (_whereClause != null ? _whereClause.GetHashCode() : 0);
			}
		}
	}
}
