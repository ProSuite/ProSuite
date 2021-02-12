using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	public class CurveMeasureRange
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CurveMeasureRange"/> class.
		/// </summary>
		/// <param name="objectId">The object id.</param>
		/// <param name="tableIndex">Index of the table.</param>
		/// <param name="mMin">The m min.</param>
		/// <param name="mMax">The m max.</param>
		/// <param name="partIndex">Index of the part.</param>
		public CurveMeasureRange(int objectId, int tableIndex,
		                         double mMin, double mMax,
		                         int partIndex = -1)
		{
			ObjectId = objectId;
			TableIndex = tableIndex;
			MMin = mMin;
			MMax = mMax;
			PartIndex = partIndex;
		}

		public int ObjectId { get; }

		public int TableIndex { get; }

		public int PartIndex { get; }

		public double MMin { get; }

		public double MMax { get; }

		[CanBeNull]
		public Location MMinEndPoint { get; set; }

		[CanBeNull]
		public Location MMaxEndPoint { get; set; }

		public override string ToString()
		{
			return
				$"ObjectId: {ObjectId}, TableIndex: {TableIndex}, MMin: {MMin}, MMax: {MMax}, MMinEndPoint: {MMinEndPoint}, MMaxEndPoint: {MMaxEndPoint}, PartIndex: {PartIndex}";
		}
	}
}
