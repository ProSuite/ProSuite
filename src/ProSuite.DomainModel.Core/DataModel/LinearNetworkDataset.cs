using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class LinearNetworkDataset : IEquatable<LinearNetworkDataset>
	{
		[UsedImplicitly] private VectorDataset _dataset;
		[UsedImplicitly] private string _whereClause;
		[UsedImplicitly] private bool _isDefaultJunction;
		[UsedImplicitly] private bool _splitting;

		public LinearNetworkDataset() { }

		public LinearNetworkDataset(VectorDataset dataset)
		{
			_dataset = dataset;
			_splitting = true;
		}

		/// <summary>
		/// The dataset whose features participate in the linear network.
		/// </summary>
		[NotNull]
		public VectorDataset Dataset => _dataset;

		/// <summary>
		/// An optional where clause that restricts the features of the dataset.
		/// </summary>
		[CanBeNull]
		public string WhereClause
		{
			get { return _whereClause; }
			set { _whereClause = value; }
		}

		/// <summary>
		/// Whether this (point!) dataset represents the default junction class.
		/// Default junctions are automatically created by the linear network edit agent
		/// if no other junction exists at an edge's start or end point.
		/// </summary>
		public bool IsDefaultJunction
		{
			get { return _isDefaultJunction; }
			set { _isDefaultJunction = value; }
		}

		/// <summary>
		/// Whether a junction shall split or an edge is split in case a junction intersects an
		/// edge's interior.
		/// </summary>
		public bool Splitting
		{
			get => _splitting;
			set => _splitting = value;
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
