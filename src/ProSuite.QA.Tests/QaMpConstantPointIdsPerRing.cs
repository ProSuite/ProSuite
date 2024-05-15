using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Reports non-linear polycurve segments as errors
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	public class QaMpConstantPointIdsPerRing : ContainerTest
	{
		[ThreadStatic] private static IPoint _queryPoint;

		private readonly bool _includeInnerRings;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string InnerRingIdDifferentFromOuterRingId =
				"InnerRingIdDifferentFromOuterRingId";

			public const string DifferentIdInRing = "DifferentIdInRing";

			public Code() : base("MpConstantPointIdsPerRing") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaMpConstantPointIdsPerRing_0))]
		public QaMpConstantPointIdsPerRing(
			[Doc(nameof(DocStrings.QaMpConstantPointIdsPerRing_multiPatchClass))] [NotNull]
			IReadOnlyFeatureClass
				multiPatchClass,
			[Doc(nameof(DocStrings.QaMpConstantPointIdsPerRing_includeInnerRings))]
			bool includeInnerRings)
			: base(multiPatchClass)
		{
			_includeInnerRings = includeInnerRings;
		}

		[InternallyUsedTest]
		public QaMpConstantPointIdsPerRing(
			[NotNull] QaMpConstantPointIdsPerRingDefinition definition)
			: this((IReadOnlyFeatureClass) definition.MultiPatchClass,
			       definition.IncludeInnerRings) { }

		private static IPoint QueryPoint => _queryPoint ?? (_queryPoint = new PointClass());

		public override bool IsQueriedTable(int tableIndex)
		{
			return false;
		}

		public override bool RetestRowsPerIntersectedTile(int tableIndex)
		{
			return false;
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			var feature = row as IReadOnlyFeature;
			if (feature == null)
			{
				return NoError;
			}

			var geometryCollection = feature.Shape as IGeometryCollection;
			if (geometryCollection == null)
			{
				return NoError;
			}

			var multiPatch = feature.Shape as IMultiPatch;
			if (multiPatch == null)
			{
				return NoError;
			}

			RingsProvider ringsProvider = GetRingsProvider(multiPatch);

			int errorCount = 0;

			Rings rings;
			while ((rings = ringsProvider.ReadRings()) != null)
			{
				Dictionary<int, List<int>> idPointIndexesDictionary =
					rings.GetIdPointIndexesDictionary();

				if (idPointIndexesDictionary.Count > 1)
				{
					errorCount += ReportError(rings, idPointIndexesDictionary, row);
				}
			}

			return errorCount;
		}

		[NotNull]
		private RingsProvider GetRingsProvider([NotNull] IMultiPatch multiPatch)
		{
			return ! _includeInnerRings
				       ? (RingsProvider) new RingRingsProvider(multiPatch)
				       : new OuterRingRingsProvider(multiPatch);
		}

		private int ReportError(
			[NotNull] Rings rings,
			[NotNull] Dictionary<int, List<int>> pointIndexesById,
			[NotNull] IReadOnlyRow row)
		{
			int firstId;
			if (RingsHaveDifferentIds(pointIndexesById, rings, out firstId))
			{
				int outerRingIds = firstId;
				string description = string.Format("Point ids within rings are constant, " +
				                                   "but inner ring point ids differ from outer ring point ids ({0})" +
				                                   "(outer ring = {1}. patch in multipatch)",
				                                   outerRingIds, rings.FirstPatchIndex + 1);

				IGeometry errorGeometry = rings.CreateMultiPatch();

				return ReportError(
					description, InvolvedRowUtils.GetInvolvedRows(row), errorGeometry,
					Codes[Code.InnerRingIdDifferentFromOuterRingId],
					TestUtils.GetShapeFieldName(row));
			}

			int? maxPointsId = GetMaxPointsId(pointIndexesById);

			if (maxPointsId == null)
			{
				IGeometry errorGeometry = rings.CreateMultiPatch();
				string description = string.Format(
					rings.RingsCount <= 1
						? "Different point ids exist in this ring ({0}. patch in multipatch)"
						: "Different point ids exist in these rings (out ring = {0}. patch in multipatch)",
					rings.FirstPatchIndex + 1);

				return ReportError(
					description, InvolvedRowUtils.GetInvolvedRows(row), errorGeometry,
					Codes[Code.DifferentIdInRing], TestUtils.GetShapeFieldName(row));
			}

			return ReportError(rings, pointIndexesById, maxPointsId.Value, row);
		}

		private int ReportError(
			[NotNull] Rings rings,
			[NotNull] IDictionary<int, List<int>> pointIndexesById,
			int maxPointsId,
			[NotNull] IReadOnlyRow row)
		{
			int totalPointCount = 0;
			int errorPointCount = 0;

			foreach (KeyValuePair<int, List<int>> pair in pointIndexesById)
			{
				int id = pair.Key;
				List<int> pointIndexes = pair.Value;

				totalPointCount += pointIndexes.Count;

				if (id == maxPointsId)
				{
					continue;
				}

				errorPointCount += pointIndexes.Count;
			}

			if (errorPointCount < 5 && errorPointCount * 2 < totalPointCount)
			{
				return ReportPointErrors(pointIndexesById, maxPointsId, rings, row);
			}

			string description;
			if (rings.RingsCount > 1)
			{
				description = string.Format(
					"{0} point ids differ from the most frequent point id {1} " +
					"({2} occurrences) in the rings (outer ring = {3}. patch in multipatch)",
					errorPointCount, maxPointsId, pointIndexesById[maxPointsId].Count,
					rings.FirstPatchIndex);
			}
			else
			{
				description = string.Format(
					"{0} point ids differ from the most frequent point id {1} " +
					"({2} occurrences) in the ring ({3}. patch in multipatch)",
					errorPointCount, maxPointsId, pointIndexesById[maxPointsId].Count,
					rings.FirstPatchIndex);
			}

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(row), rings.CreateMultiPatch(),
				Codes[Code.DifferentIdInRing], TestUtils.GetShapeFieldName(row));
		}

		private int ReportPointErrors(
			[NotNull] IDictionary<int, List<int>> pointIndexesById,
			int maxPointsId,
			[NotNull] Rings rings,
			[NotNull] IReadOnlyRow row)
		{
			object missing = Type.Missing;

			IPointCollection points = new MultipointClass();
			GeometryUtils.EnsureSpatialReference((IGeometry) points,
			                                     rings.SpatialReference);

			foreach (KeyValuePair<int, List<int>> pair in pointIndexesById)
			{
				int id = pair.Key;
				if (id == maxPointsId)
				{
					continue;
				}

				List<int> pointIndexes = pair.Value;

				foreach (int pointIndex in pointIndexes)
				{
					IPoint point = rings.get_Point(pointIndex);
					points.AddPoint(point, ref missing, ref missing);
				}
			}

			string description;
			if (rings.RingsCount > 1)
			{
				description = string.Format(
					"The point ids of these points differ from the most frequent point id {0} " +
					"({1} occurrences) in the rings (outer ring = {2}. patch in multipatch)",
					maxPointsId, pointIndexesById[maxPointsId].Count,
					rings.FirstPatchIndex + 1);
			}
			else
			{
				description = string.Format(
					"The point ids of these points differ from the most frequent point id {0} " +
					"({1} occurrences) in the ring ({2}. patch in Multipatch)",
					maxPointsId, pointIndexesById[maxPointsId].Count,
					rings.FirstPatchIndex + 1);
			}

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(row), (IGeometry) points,
				Codes[Code.DifferentIdInRing], TestUtils.GetShapeFieldName(row));
		}

		private static int? GetMaxPointsId(
			[NotNull] Dictionary<int, List<int>> pointIndexesById)
		{
			int maxPointsCount = 0;
			int? maxPointsId = null;

			foreach (KeyValuePair<int, List<int>> pair in pointIndexesById)
			{
				int pointId = pair.Key;
				List<int> pointIndexes = pair.Value;
				if (pointIndexes.Count > maxPointsCount)
				{
					maxPointsCount = pointIndexes.Count;
					maxPointsId = pointId;
				}
				else if (pointIndexes.Count == maxPointsCount)
				{
					maxPointsId = null;
				}
			}

			return maxPointsId;
		}

		private static bool RingsHaveDifferentIds(
			[NotNull] Dictionary<int, List<int>> pointIndexesById,
			[NotNull] Rings rings,
			out int firstPointId)
		{
			var errorRingsById = new Dictionary<int, List<IRing>>();
			firstPointId = 0;
			bool first = true;

			foreach (KeyValuePair<int, List<int>> pair in pointIndexesById)
			{
				int pointId = pair.Key;
				List<int> pointIndexes = pair.Value;

				if (first)
				{
					firstPointId = pointId;
				}

				first = false;

				List<IRing> errorRings = rings.GetRingsIfComplete(pointIndexes);

				if (errorRings == null)
				{
					errorRingsById = null;
					break;
				}

				errorRingsById.Add(pointId, errorRings);
			}

			return errorRingsById != null;
		}

		/// <summary>
		/// use private or by unit tests only
		/// </summary>
		private class Rings
		{
			private readonly List<IRing> _rings;
			private readonly int _firstPatchIndex;

			public Rings([NotNull] IRing ring, int patchIndex)
			{
				_firstPatchIndex = patchIndex;
				_rings = new List<IRing> { ring };
			}

			public Rings([NotNull] List<IRing> rings, int firstPatchIndex)
			{
				_rings = rings;
				_firstPatchIndex = firstPatchIndex;
			}

			public int RingsCount => _rings.Count;

			public int FirstPatchIndex => _firstPatchIndex;

			public ISpatialReference SpatialReference => _rings[0].SpatialReference;

			[NotNull]
			public Dictionary<int, List<int>> GetIdPointIndexesDictionary()
			{
				var result = new Dictionary<int, List<int>>();
				int baseIndex = 0;

				foreach (IRing ring in _rings)
				{
					var ringPointCollection = (IPointCollection) ring;
					FillIdPointIndexesDictionary(result, ringPointCollection, baseIndex);
					baseIndex += ringPointCollection.PointCount;
				}

				return result;
			}

			private static void FillIdPointIndexesDictionary(
				[NotNull] IDictionary<int, List<int>> idPointIndexesDictionary,
				[NotNull] IPointCollection pointCollection,
				int baseIndex)
			{
				int pointCount = pointCollection.PointCount;
				for (int pointIndex = 0; pointIndex < pointCount; pointIndex++)
				{
					pointCollection.QueryPoint(pointIndex, QueryPoint);
					int id = QueryPoint.ID;

					List<int> pointIndexes;
					if (! idPointIndexesDictionary.TryGetValue(id, out pointIndexes))
					{
						pointIndexes = new List<int>(pointCount);

						idPointIndexesDictionary.Add(id, pointIndexes);
					}

					pointIndexes.Add(pointIndex + baseIndex);
				}
			}

			[CanBeNull]
			public List<IRing> GetRingsIfComplete([NotNull] ICollection<int> pointIndexes)
			{
				int baseIndex = 0;
				var completeRings = new List<IRing>();

				foreach (IRing ring in _rings)
				{
					var ringPointCollection = (IPointCollection) ring;

					bool partly = false;
					bool canBeComplete = false;
					int endIndex = baseIndex + ringPointCollection.PointCount;

					for (int pointIndex = baseIndex; pointIndex < endIndex; pointIndex++)
					{
						if (pointIndexes.Contains(pointIndex))
						{
							partly = true;
							canBeComplete = true;
						}
						else if (partly)
						{
							canBeComplete = false;
							break;
						}
					}

					if (canBeComplete)
					{
						completeRings.Add((IRing) ringPointCollection);
					}
					else if (partly)
					{
						return null;
					}

					baseIndex += ringPointCollection.PointCount;
				}

				return completeRings;
			}

			[NotNull]
			public IMultiPatch CreateMultiPatch()
			{
				var result = new MultiPatchClass();

				var geometryCollection = (IGeometryCollection) result;
				object missing = Type.Missing;

				foreach (IRing ring in _rings)
				{
					IRing clone = GeometryFactory.Clone(ring);
					geometryCollection.AddGeometry(clone, ref missing, ref missing);
				}

				GeometryUtils.EnsureSpatialReference(result, _rings[0].SpatialReference);

				return result;
			}

			[NotNull]
			public IPoint get_Point(int pointIndex)
			{
				int baseIndex = 0;

				foreach (IRing ring in _rings)
				{
					var ringPointCollection = (IPointCollection) ring;
					int endIndex = baseIndex + ringPointCollection.PointCount;
					if (endIndex > pointIndex)
					{
						return ringPointCollection.get_Point(pointIndex - baseIndex);
					}

					baseIndex += ringPointCollection.PointCount;
				}

				throw new ArgumentOutOfRangeException(nameof(pointIndex));
			}
		}

		/// <summary>
		/// use private or by unit tests only
		/// </summary>
		private abstract class RingsProvider
		{
			[CanBeNull]
			public abstract Rings ReadRings();
		}

		/// <summary>
		/// use private or by unit tests only
		/// </summary>
		private class RingRingsProvider : RingsProvider
		{
			private readonly IGeometryCollection _patches;

			private int _currentIndex;

			public RingRingsProvider([NotNull] IMultiPatch multiPatch)
			{
				_patches = (IGeometryCollection) multiPatch;
				_currentIndex = 0;
			}

			public override Rings ReadRings()
			{
				IRing ring = null;
				int patchIndex = 0;
				while (ring == null)
				{
					if (_currentIndex >= _patches.GeometryCount)
					{
						return null;
					}

					ring = _patches.get_Geometry(_currentIndex) as IRing;
					patchIndex = _currentIndex;
					_currentIndex++;
				}

				return new Rings(ring, patchIndex);
			}
		}

		/// <summary>
		/// use private or by unit tests only
		/// </summary>
		private class OuterRingRingsProvider : RingsProvider
		{
			private readonly IGeometryCollection _patches;

			private int _currentIndex;

			public OuterRingRingsProvider([NotNull] IMultiPatch multiPatch)
			{
				_patches = (IGeometryCollection) multiPatch;
				_currentIndex = 0;
			}

			public override Rings ReadRings()
			{
				var multiPatch = (IMultiPatch) _patches;
				List<IRing> rings = null;
				int firstPatchIndex = 0;

				while (rings == null)
				{
					if (_currentIndex >= _patches.GeometryCount)
					{
						return null;
					}

					var ring = _patches.get_Geometry(_currentIndex) as IRing;

					if (ring != null)
					{
						bool isBeginningRing = false;
						multiPatch.GetRingType(ring, ref isBeginningRing);

						if (isBeginningRing)
						{
							firstPatchIndex = _currentIndex;
							int followingRingCount = multiPatch.get_FollowingRingCount(ring);

							rings = new List<IRing>(followingRingCount + 1) { ring };

							if (followingRingCount > 0)
							{
								var followingRings = new IRing[followingRingCount];
								GeometryUtils.GeometryBridge.QueryFollowingRings(multiPatch, ring,
									ref
									followingRings);

								rings.AddRange(followingRings);
							}
						}
					}

					_currentIndex++;
				}

				return new Rings(rings, firstPatchIndex);
			}
		}
	}
}
