namespace ProSuite.Commons.AO.Geometry
{
	public struct VertexIndex
	{
		public VertexIndex(int partIndex, int vertexIndexInPart, bool isLastInPart) : this()
		{
			PartIndex = partIndex;
			VertexIndexInPart = vertexIndexInPart;
			IsLastInPart = isLastInPart;
		}

		public int PartIndex { get; set; }

		public int VertexIndexInPart { get; set; }

		public bool IsLastInPart { get; set; }

		public bool Equals(VertexIndex other)
		{
			return VertexIndexInPart == other.VertexIndexInPart &&
			       PartIndex == other.PartIndex;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			return obj is VertexIndex index && Equals(index);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (VertexIndexInPart * 397) ^ PartIndex;
			}
		}
	}
}
