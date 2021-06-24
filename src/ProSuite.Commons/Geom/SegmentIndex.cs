using System;

namespace ProSuite.Commons.Geom
{
	/// <summary>
	/// Segment index used to address a single <see cref="Line3D"/> in a
	/// <see cref="MultiLinestring"/>.
	/// </summary>
	public readonly struct SegmentIndex : IComparable<SegmentIndex>,
	                                      IEquatable<SegmentIndex>
	{
		public SegmentIndex(int partIndex, int localIndex)
		{
			PartIndex = partIndex;
			LocalIndex = localIndex;
		}

		public int PartIndex { get; }
		public int LocalIndex { get; }

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			return obj is SegmentIndex && Equals((SegmentIndex) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (PartIndex * 397) ^ LocalIndex;
			}
		}

		public int CompareTo(object value)
		{
			return CompareTo((SegmentIndex) value);
		}

		public int CompareTo(SegmentIndex other)
		{
			int partCompare = PartIndex.CompareTo(other.PartIndex);

			if (partCompare != 0)
			{
				return partCompare;
			}

			return LocalIndex.CompareTo(other.LocalIndex);
		}

		public override string ToString()
		{
			return $"Part index: {PartIndex} / Local index: {LocalIndex}";
		}

		public bool Equals(SegmentIndex other)
		{
			return PartIndex == other.PartIndex && LocalIndex == other.LocalIndex;
		}
	}
}