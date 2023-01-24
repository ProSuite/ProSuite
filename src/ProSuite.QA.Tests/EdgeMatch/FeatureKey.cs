namespace ProSuite.QA.Tests.EdgeMatch
{
	internal class FeatureKey
	{
		public FeatureKey(long objectId, int tableIndex)
		{
			ObjectId = objectId;
			TableIndex = tableIndex;
		}

		public long ObjectId { get; }

		public int TableIndex { get; }
	}
}
