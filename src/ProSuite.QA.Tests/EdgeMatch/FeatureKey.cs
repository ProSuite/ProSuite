namespace ProSuite.QA.Tests.EdgeMatch
{
	internal class FeatureKey
	{
		public FeatureKey(int objectId, int tableIndex)
		{
			ObjectId = objectId;
			TableIndex = tableIndex;
		}

		public int ObjectId { get; }

		public int TableIndex { get; }
	}
}
