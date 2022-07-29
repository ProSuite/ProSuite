using System;
using System.Collections.Generic;
using System.Text;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container.Geometry;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Properties;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using IPnt = ProSuite.Commons.Geom.IPnt;
using Pnt = ProSuite.Commons.Geom.Pnt;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;

namespace ProSuite.QA.Tests.Coincidence
{
	public abstract class QaNearCoincidenceBase : QaPolycurveCoincidenceBase
	{
		private readonly double _coincidenceToleranceSquared;
		private readonly double _coincidenceTolerance;

		private readonly SegmentNeighbors _coincidentParts =
			new SegmentNeighbors(new SegmentPartComparer());

		private readonly IPairDistanceProvider _connectedMinLengthProvider;
		private readonly IPairDistanceProvider _disjointMinLengthProvider;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string NearlyCoincidentSection_BetweenFeatures =
				"NearlyCoincidentSection.BetweenFeatures";

			public const string NearlyCoincidentSection_WithinFeature =
				"NearlyCoincidentSection.WithinFeature";

			public Code() : base("NearCoincidence") { }
		}

		#endregion

		protected QaNearCoincidenceBase(
			[NotNull] IEnumerable<IReadOnlyFeatureClass> featureClasses,
			double searchDistance,
			[NotNull] IFeatureDistanceProvider nearDistanceProvider,
			[NotNull] IPairDistanceProvider connectedMinLengthProvider,
			[NotNull] IPairDistanceProvider disjointMinLengthProvider,
			bool is3D,
			double coincidenceTolerance)
			: base(featureClasses, searchDistance, nearDistanceProvider, is3D)
		{
			_connectedMinLengthProvider = connectedMinLengthProvider;
			_disjointMinLengthProvider = disjointMinLengthProvider;

			_coincidenceTolerance = coincidenceTolerance;
			_coincidenceToleranceSquared = coincidenceTolerance * coincidenceTolerance;
		}

		protected override NeighborhoodFinder GetNeighborhoodFinder(
			IFeatureRowsDistance rowsDistance, IReadOnlyFeature feature, int tableIndex,
			IReadOnlyFeature neighbor, int neighborTableIndex)
		{
			return new NearNeighborhoodFinder(
				rowsDistance, feature, tableIndex, neighbor, neighborTableIndex, _coincidentParts,
				_coincidenceTolerance,
				_coincidenceToleranceSquared, Is3D);
		}

		protected override int Check(IReadOnlyFeature feat0, int tableIndex,
		                             SortedDictionary<SegmentPart, SegmentParts> processed0,
		                             IReadOnlyFeature feat1, int neighborTableIndex,
		                             SortedDictionary<SegmentPart, SegmentParts> processed1,
		                             double near)
		{
			var errorCount = 0;

			IPairRowsDistance connectedRowsDistance =
				_connectedMinLengthProvider.GetRowsDistance(feat0, tableIndex);
			double connectedMinLength = connectedRowsDistance.GetAddedDistance(
				feat1, neighborTableIndex);

			IPairRowsDistance disjointRowsDistance =
				_disjointMinLengthProvider.GetRowsDistance(feat0, tableIndex);
			double disjointMinLength = disjointRowsDistance.GetAddedDistance(
				feat1, neighborTableIndex);

			errorCount += CheckPartCoincidence(feat0, feat1, processed0, _coincidentParts,
			                                   near, connectedMinLength, disjointMinLength);

			processed0.Clear();
			processed1.Clear();

			_coincidentParts.Clear();

			return errorCount;
		}

		private int CheckPartCoincidence(
			[NotNull] IReadOnlyRow row0,
			[NotNull] IReadOnlyRow row1,
			[NotNull] SortedDictionary<SegmentPart, SegmentParts> nearList0,
			[NotNull] SortedDictionary<SegmentPart, SegmentParts> toleranceList0,
			double near,
			double connectedMinLength,
			double disjointMinLength)
		{
			var errorCount = 0;

			IList<SubClosedCurve> nearCurves;
			IList<SubClosedCurve> nearSelfCurves;
			GetSubcurves(row0, nearList0, near, connectedMinLength,
			             out nearCurves, out nearSelfCurves);

			nearCurves = GetLongSubcurves(nearCurves,
			                              Math.Min(connectedMinLength, disjointMinLength));

			IList<SubClosedCurve> coincidentCurves;
			IList<SubClosedCurve> emptyList;
			GetSubcurves(row0, toleranceList0, near, connectedMinLength,
			             out coincidentCurves, out emptyList);

			errorCount += CheckPartCoincidence(row0, row1, nearCurves, coincidentCurves, false,
			                                   near, connectedMinLength, disjointMinLength);
			errorCount += CheckPartCoincidence(row0, row1, nearSelfCurves, emptyList, true,
			                                   near, connectedMinLength, disjointMinLength);

			return errorCount;
		}

		private int CheckPartCoincidence([NotNull] IReadOnlyRow row0, [NotNull] IReadOnlyRow row1,
		                                 [NotNull] IList<SubClosedCurve> nearCurves,
		                                 [NotNull] IList<SubClosedCurve> coincidentCurves,
		                                 bool nearSelf,
		                                 double near,
		                                 double connectedMinLength, double disjointMinLength)
		{
			if (nearCurves.Count == 0)
			{
				return NoError;
			}

			IList<SubClosedCurve> disjointCurves = new List<SubClosedCurve>(nearCurves.Count);
			IList<SubClosedCurve> connectedCurves = new List<SubClosedCurve>(nearCurves.Count);

			// Find and handle disjoint subcurves
			foreach (SubClosedCurve nearCurve in nearCurves)
			{
				if (nearSelf)
				{
					connectedCurves.Add(nearCurve);
					continue;
				}

				SubClosedCurve within = null;
				foreach (SubClosedCurve candidate in coincidentCurves)
				{
					if (nearCurve.IsWithin(candidate))
					{
						within = candidate;
						break;
					}
				}

				if (within == null)
				{
					disjointCurves.Add(nearCurve);
				}
				else
				{
					connectedCurves.Add(nearCurve);
				}
			}

			var errorCount = 0;
			foreach (SubClosedCurve disjointCurve in disjointCurves)
			{
				errorCount += PartCoincidenceErrors(row0, row1, disjointCurve,
				                                    near, disjointMinLength,
				                                    true);
			}

			// Handle connected curves

			double coincidenceMinLength = _coincidenceTolerance * connectedMinLength / near;
			coincidentCurves = GetLongSubcurves(coincidentCurves, coincidenceMinLength);
			foreach (SubClosedCurve coincidentCurve in coincidentCurves)
			{
				SubClosedCurve surround = null;
				foreach (SubClosedCurve candidate in connectedCurves)
				{
					if (candidate.IsWithin(coincidentCurve))
					{
						surround = candidate;
						break;
					}
				}

				if (surround == null)
				{
					// surrounding curve is < _connectedMinLength and was filtered away.
					continue;
				}

				connectedCurves.Remove(surround);

				if (surround.IsCompleteClosedPart())
				{
					if (coincidentCurve.IsCompleteClosedPart())
					{
						continue;
					}

					var rest = new SubClosedCurve(surround.BaseGeometry,
					                              surround.PartIndex, coincidentCurve.EndFullIndex,
					                              coincidentCurve.StartFullIndex);
					if (rest.GetLength() >= connectedMinLength)
					{
						connectedCurves.Add(rest);
					}

					continue;
				}

				var pre = new SubClosedCurve(surround.BaseGeometry,
				                             surround.PartIndex, surround.StartFullIndex,
				                             coincidentCurve.StartFullIndex);
				if (pre.GetLength() >= connectedMinLength)
				{
					connectedCurves.Add(pre);
				}

				var post = new SubClosedCurve(surround.BaseGeometry,
				                              surround.PartIndex, coincidentCurve.EndFullIndex,
				                              surround.EndFullIndex);
				if (post.GetLength() >= connectedMinLength)
				{
					connectedCurves.Add(post);
				}
			}

			foreach (SubClosedCurve connectedCurve in connectedCurves)
			{
				errorCount += PartCoincidenceErrors(row0, row1, connectedCurve,
				                                    near, connectedMinLength);
			}

			return errorCount;
		}

		private static IList<SubClosedCurve> GetLongSubcurves(
			[NotNull] IEnumerable<SubClosedCurve> subcurves, double minLength)
		{
			var longCurves = new List<SubClosedCurve>();
			foreach (SubClosedCurve subcurve in subcurves)
			{
				if (subcurve.GetLength() >= minLength)
				{
					longCurves.Add(subcurve);
				}
			}

			return longCurves;
		}

		private void GetSubcurves(IReadOnlyRow row,
		                          SortedDictionary<SegmentPart, SegmentParts> parts,
		                          double near,
		                          double connectedMinLength,
		                          out IList<SubClosedCurve> connected,
		                          out IList<SubClosedCurve> nearSelfConnected)
		{
			var feat = (IReadOnlyFeature) row;
			IIndexedSegments geom = IndexedSegmentUtils.GetIndexedGeometry(feat, false);
			double nearSquared = near * near;

			var curves = new List<Subcurve>();
			var nearSelfCurves = new List<Subcurve>();
			foreach (SegmentParts segmentParts in parts.Values)
			{
				segmentParts.Sort(new SegmentPartComparer());
				foreach (SegmentPart part in segmentParts)
				{
					AddSegment(feat, geom, curves, nearSelfCurves, part, connectedMinLength,
					           nearSquared);
				}
			}

			connected = GetClosedCurves(curves);
			nearSelfConnected = GetClosedCurves(nearSelfCurves);
		}

		private static List<SubClosedCurve> GetClosedCurves(List<Subcurve> curves)
		{
			var connected = new List<SubClosedCurve>(curves.Count);

			Subcurve atStart = null;
			foreach (Subcurve curve in curves)
			{
				if (MathUtils.AreEqual(curve.StartFullIndex, 0))
				{
					if (atStart != null)
					{
						connected.Add(new SubClosedCurve(atStart));
					}

					atStart = curve;
					continue;
				}

				if (atStart != null &&
				    atStart.PartIndex == curve.PartIndex &&
				    curve.BaseGeometry.IsPartClosed(curve.PartIndex) &&
				    MathUtils.AreEqual(curve.EndFullIndex,
				                       curve.BaseGeometry.GetPartSegmentCount(curve.PartIndex)))
				{
					connected.Add(new SubClosedCurve(atStart, curve));
					atStart = null;
				}
				else
				{
					connected.Add(new SubClosedCurve(curve));
				}
			}

			if (atStart != null)
			{
				connected.Add(new SubClosedCurve(atStart));
			}

			return connected;
		}

		private void AddSegment([NotNull] IReadOnlyFeature feature,
		                        [NotNull] IIndexedSegments geom,
		                        [NotNull] IList<Subcurve> standardConnectedList,
		                        [NotNull] IList<Subcurve> nearSelfConnectedList,
		                        [NotNull] SegmentPart segPart,
		                        double connectedMinLength,
		                        double nearSquared)
		{
			Subcurve current = null;
			IList<Subcurve> connectedList = standardConnectedList;

			SegmentProxy neighbor = segPart.NearSelf;

			if (neighbor != null)
			{
				// Get the distance between the parts that are near
				double d0 = double.MaxValue;
				int partIndex = segPart.PartIndex;

				if (geom.IsPartClosed(partIndex) || segPart.SegmentIndex > neighbor.SegmentIndex)
				{
					if (segPart.PartIndex == neighbor.PartIndex)
						// TODO revise; workaround to avoid exception (invalid index)
					{
						// raw estimate for distance betwenn segPart and neighborPart
						// this estimate is too small, because not the entire part of neighbor is near segPart
						var curve = new SubClosedCurve(geom, partIndex,
						                               neighbor.SegmentIndex + 1,
						                               segPart.FullMin);
						d0 = curve.GetLength();
						if (d0 < connectedMinLength)
						{
							double d0Max = d0 + neighbor.Length;
							if (d0Max > connectedMinLength)
							{
								// closer investigation necessary
								double min;
								double max;
								GetClosePart(neighbor, geom, segPart, nearSquared, Is3D,
								             out min, out max);

								d0 = d0 + (1 - max) * neighbor.Length;
							} // else segPart is definitly near neighbor
						} //else segPart is definitly not near neighbor
					}
				}

				if (geom.IsPartClosed(partIndex) || segPart.SegmentIndex < neighbor.SegmentIndex)
				{
					if (segPart.PartIndex == neighbor.PartIndex)
						// TODO revise; workaround to avoid exception (invalid index)
					{
						var curve = new SubClosedCurve(geom, partIndex,
						                               segPart.FullMax,
						                               neighbor.SegmentIndex);

						double d0Min = curve.GetLength();
						if (d0Min < connectedMinLength)
						{
							double d0Max = d0Min + neighbor.Length;
							if (d0Max > connectedMinLength)
							{
								// closer investigation necessary
								double min;
								double max;
								GetClosePart(neighbor, geom, segPart, nearSquared, Is3D,
								             out min, out max);

								d0Min = d0Min + min * neighbor.Length;
							} //else segPart is definitly near neighbor
						} // else segPart is definitly not near neighbor

						d0 = Math.Min(d0, d0Min);
					}
				}

				if (d0 < connectedMinLength)
				{
					connectedList = nearSelfConnectedList;
				}
			}

			if (connectedList.Count > 0)
			{
				current = connectedList[connectedList.Count - 1];
			}

			if (current != null)
			{
				if (current.PartIndex != segPart.PartIndex)
				{
					current = null;
				}
				else if (current.EndFullIndex < segPart.FullMin)
				{
					current = null;
				}
			}

			if (current == null)
			{
				current = new Subcurve(geom, segPart.PartIndex, segPart.SegmentIndex,
				                       segPart.MinFraction, segPart.SegmentIndex,
				                       segPart.MaxFraction);
				connectedList.Add(current);
			}
			else
			{
				if (current.EndFullIndex < segPart.FullMax)
				{
					current.EndSegmentIndex = segPart.SegmentIndex;
					current.EndFraction = segPart.MaxFraction;
				}
			}
		}

		private static void GetClosePart([NotNull] SegmentProxy neighbor,
		                                 [NotNull] IIndexedSegments geom,
		                                 [NotNull] SegmentPart segPart,
		                                 double searchDistanceSquared, bool is3D,
		                                 out double min, out double max)
		{
			SegmentProxy part = geom.GetSegment(segPart.PartIndex, segPart.SegmentIndex);
			IPnt start = part.GetPointAt(segPart.MinFraction);
			double minStart, maxStart;
			GetClosePart(neighbor, start, searchDistanceSquared, is3D, out minStart,
			             out maxStart);

			IPnt end = part.GetPointAt(segPart.MaxFraction);
			double minEnd, maxEnd;
			GetClosePart(neighbor, end, searchDistanceSquared, is3D, out minEnd, out maxEnd);

			min = Math.Min(minStart, minEnd);
			max = Math.Max(maxStart, maxEnd);
		}

		private static void GetClosePart([NotNull] SegmentProxy neighbor,
		                                 [NotNull] IPnt near,
		                                 double searchDistanceSquared, bool is3D,
		                                 out double min, out double max)
		{
			IList<double[]> limits;
			Pnt p = Pnt.Create(near);
			bool cut = SegmentUtils.CutCurveCircle(neighbor, p, searchDistanceSquared, is3D,
			                                       out limits);
			if (cut == false || limits.Count == 0)
			{
				min = SegmentUtils.GetClosestPointFraction(neighbor, p, is3D);
				max = min;
			}
			else
			{
				min = double.MaxValue;
				max = double.MinValue;
				foreach (double[] limit in limits)
				{
					foreach (double d in limit)
					{
						min = Math.Min(d, min);
						max = Math.Max(d, max);
					}
				}
			}

			min = Math.Max(min, 0);
			max = Math.Max(max, 0);
			min = Math.Min(min, 1);
			max = Math.Min(max, 1);
		}

		private int PartCoincidenceErrors([NotNull] IReadOnlyRow row0,
		                                  [NotNull] IReadOnlyRow row1,
		                                  [NotNull] SubClosedCurve connected,
		                                  double near,
		                                  double minLength,
		                                  bool isDisjoint = false)
		{
			double length = connected.GetLength();
			if (length <= minLength)
			{
				return NoError;
			}

			IGeometry shape0 = ((IReadOnlyFeature) row0).Shape;
			IGeometry shape1 = ((IReadOnlyFeature) row1).Shape;

			bool isWithinFeature = row0 == row1;
			bool zAware = ((IZAware) shape0).ZAware &&
			              ((IZAware) shape1).ZAware;

			IPolyline errorGeometry = CreatePolyline(connected, zAware);

			// NOTE there can be multiple paths in the error geometry, when the curve touches itself 
			// see https://issuetracker02.eggits.net/browse/COM-47

			var errorCount = 0;

			string shapeFieldName = ((IReadOnlyFeatureClass) row0.Table).ShapeFieldName;

			foreach (IPath errorPath in GeometryUtils.GetPaths(errorGeometry))
			{
				if (errorPath.Length < minLength)
				{
					continue;
				}

				IPolyline errorPartGeometry = CreateErrorGeometry(errorPath, zAware);

				ISpatialReference spatialReference = shape0.SpatialReference;
				ICollection<object> values;
				string description = UsesConstantNearTolerance
					                     ? GetShortDescription(minLength, errorPartGeometry,
					                                           spatialReference, out values)
					                     : GetExtendedDescription(minLength, near,
					                                              errorPartGeometry,
					                                              spatialReference,
					                                              shape0, shape1,
					                                              isDisjoint, isWithinFeature,
					                                              out values);

				errorCount += isWithinFeature
					              ? ReportError(description, InvolvedRowUtils.GetInvolvedRows(row0),
					                            errorGeometry,
					                            Codes[Code.NearlyCoincidentSection_WithinFeature],
					                            shapeFieldName, values: values)
					              : ReportError(description,
					                            InvolvedRowUtils.GetInvolvedRows(row0, row1),
					                            errorGeometry,
					                            Codes[Code.NearlyCoincidentSection_BetweenFeatures],
					                            shapeFieldName, values: values);
			}

			return errorCount;
		}

		[NotNull]
		private string GetExtendedDescription(double minLength, double near,
		                                      [NotNull] IPolyline errorGeometry,
		                                      [NotNull] ISpatialReference spatialReference,
		                                      [NotNull] IGeometry shape0,
		                                      [NotNull] IGeometry shape1,
		                                      bool isDisjoint, bool isWithinFeature,
		                                      [NotNull] out ICollection<object> values)
		{
			values = new List<object>();

			var sb = new StringBuilder();
			sb.AppendFormat(
				LocalizableStrings.QaNearCoincidenceBase_NearlyCoincidentSection_Extended_Base,
				near,
				FormatLength(errorGeometry.Length, spatialReference),
				FormatLength(minLength, spatialReference));

			values.Add(errorGeometry.Length);

			if (! isWithinFeature)
			{
				if (isDisjoint)
				{
					double minimumDistance =
						((IProximityOperator) errorGeometry).ReturnDistance(shape1);

					values.Add(minimumDistance);

					sb.AppendFormat(
						LocalizableStrings
							.QaNearCoincidenceBase_NearlyCoincidentSection_Extended_DisjointPaths,
						FormatLength(minimumDistance, spatialReference));
				}
				else
				{
					sb.Append(
						LocalizableStrings
							.QaNearCoincidenceBase_NearlyCoincidentSection_Extended_ConnectedPaths);
				}
			}

			return sb.ToString();
		}

		[NotNull]
		private string GetShortDescription(double minLength,
		                                   [NotNull] IPolyline errorGeometry,
		                                   [NotNull] ISpatialReference spatialReference,
		                                   [NotNull] out ICollection<object> values)
		{
			values = new object[] {errorGeometry.Length};

			return string.Format(
				LocalizableStrings.QaNearCoincidenceBase_NearlyCoincidentSection_Short,
				FormatLength(errorGeometry.Length, spatialReference),
				FormatLength(minLength, spatialReference));
		}

		[NotNull]
		private static IPolyline CreateErrorGeometry([NotNull] IPath errorPath, bool zAware)
		{
			IPolyline result = new PolylineClass();
			((IZAware) result).ZAware = zAware;

			((ISegmentCollection) result).AddSegmentCollection(
				(ISegmentCollection) errorPath);

			return result;
		}

		[NotNull]
		private static IPolyline CreatePolyline([NotNull] SubClosedCurve connected,
		                                        bool zAware)
		{
			IPolyline result = connected.GetGeometry();
			((IZAware) result).ZAware = zAware;

			return result;
		}

		protected sealed class NearNeighborhoodFinder : NeighborhoodFinder
		{
			private readonly SegmentNeighbors _coincidentParts;
			private readonly double _coincidenceTolerance;
			private readonly double _coincidenceToleranceSquared;
			private readonly bool _is3D;

			public NearNeighborhoodFinder(
				[NotNull] IFeatureRowsDistance rowsDistance,
				[NotNull] IReadOnlyFeature feature, int tableIndex,
				[CanBeNull] IReadOnlyFeature neighbor, int neighborTableIndex,
				SegmentNeighbors coincidentParts,
				double coincidenceTolerance,
				double coincidenceToleranceSquared, bool is3D)
				: base(rowsDistance, feature, tableIndex, neighbor, neighborTableIndex)
			{
				_coincidentParts = coincidentParts;
				_coincidenceTolerance = coincidenceTolerance;
				_coincidenceToleranceSquared = coincidenceToleranceSquared;
				_is3D = is3D;
			}

			protected override bool VerifyContinue(SegmentProxy seg0, SegmentProxy seg1,
			                                       SegmentNeighbors processed1,
			                                       SegmentParts partsOfSeg0, bool coincident)
			{
				TryAssignComplete(seg1, processed1, partsOfSeg0);

				SegmentParts coincidentPartsOfSeg0;
				var key = new SegmentPart(seg0, 0, 1, true);
				if (! _coincidentParts.TryGetValue(key, out coincidentPartsOfSeg0))
				{
					coincidentPartsOfSeg0 = new SegmentParts();
					_coincidentParts.Add(key, coincidentPartsOfSeg0);
				}

				if (coincident)
				{
					partsOfSeg0.IsComplete = true;
					coincidentPartsOfSeg0.Add(key);
					return false;
				}
				//return true;

				IBox seg0Box = seg0.Extent;
				seg0Box = new Box(Pnt.Create(seg0Box.Min), Pnt.Create(seg0Box.Max));
				if (_coincidenceTolerance > 0)
				{
					seg0Box.Min.X -= _coincidenceTolerance;
					seg0Box.Min.Y -= _coincidenceTolerance;
					seg0Box.Max.X += _coincidenceTolerance;
					seg0Box.Max.Y += _coincidenceTolerance;
				}

				if (! seg0Box.Intersects(seg1.Extent))
				{
					return true;
				}

				var cap = new RoundCap();
				NearSegment hullStart;
				NearSegment hullEnd;
				bool isCoincident;
				IList<double[]> limits =
					FindNeighborhood(
						new SegmentHull(seg0, 0, cap, cap),
						new SegmentHull(seg1, _coincidenceTolerance, cap, cap),
						_is3D, 0,
						out hullStart, out hullEnd, out isCoincident);
				IList<SegmentPart> addParts = GetSegmentParts(seg0, seg1, limits, isCoincident);
				coincidentPartsOfSeg0.AddRange(addParts);

				bool isComplete = SegmentPart.VerifyComplete(coincidentPartsOfSeg0);

				partsOfSeg0.IsComplete = isComplete;

				return ! isComplete;
			}
		}
	}
}
