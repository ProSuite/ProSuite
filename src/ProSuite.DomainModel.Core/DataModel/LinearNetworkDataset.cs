using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class LinearNetworkDataset : IEquatable<LinearNetworkDataset>
	{
		[UsedImplicitly] private VectorDataset _dataset;
		[UsedImplicitly] private string _whereClause;
		[UsedImplicitly] private bool _isDefaultJunction;

		public LinearNetworkDataset() { }

		public LinearNetworkDataset(VectorDataset dataset)
		{
			_dataset = dataset;
		}

		[NotNull]
		public VectorDataset Dataset => _dataset;

		[CanBeNull]
		public string WhereClause
		{
			get { return _whereClause; }
			set { _whereClause = value; }
		}

		public bool IsDefaultJunction
		{
			get { return _isDefaultJunction; }
			set { _isDefaultJunction = value; }
		}

		public bool Equals(LinearNetworkDataset other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return Equals(_dataset.Id, other._dataset.Id) && _whereClause == other._whereClause &&
			       _isDefaultJunction == other._isDefaultJunction;
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

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((LinearNetworkDataset) obj);
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
