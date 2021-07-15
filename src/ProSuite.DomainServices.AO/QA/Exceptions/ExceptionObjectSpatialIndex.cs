using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.SpatialIndex;
using ProSuite.QA.Container.Geometry;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public class ExceptionObjectSpatialIndex
	{
		[CanBeNull] private ExceptionsBoxTree _boxTree;
		[NotNull] private readonly IEnvelope _envelopeTemplate = new EnvelopeClass();

		[NotNull] private readonly List<ExceptionObject> _exceptionObjects =
			new List<ExceptionObject>();

		private double _xyTolerance;
		[CanBeNull] private readonly IBox _areaOfInterestBox;

		public ExceptionObjectSpatialIndex([CanBeNull] IBox areaOfInterestBox = null)
		{
			_areaOfInterestBox = areaOfInterestBox;
		}

		public void Add([NotNull] ExceptionObject exceptionObject)
		{
			Assert.ArgumentNotNull(exceptionObject, nameof(exceptionObject));
			Assert.Null(_boxTree, "box tree already initialized");

			IBox box = exceptionObject.ShapeEnvelope;

			if (box == null)
			{
				return;
			}

			if (exceptionObject.XYTolerance != null)
			{
				_xyTolerance = Math.Max(exceptionObject.XYTolerance.Value, _xyTolerance);
			}

			// gather all exception objects; initialize box tree (with extent of all added issues) on first search
			_exceptionObjects.Add(exceptionObject);
		}

		[NotNull]
		public IEnumerable<ExceptionObject> Search([NotNull] IGeometry geometry)
		{
			if (_boxTree == null)
			{
				_boxTree = CreateBoxTree(_exceptionObjects, _xyTolerance);

				_exceptionObjects.Clear();
			}

			if (_boxTree.Count == 0 || geometry.IsEmpty)
			{
				yield break;
			}

			IBox searchBox = QaGeometryUtils.CreateBox(geometry, _xyTolerance);

			IBox issueBox = null;
			IBox clippedIssueBox = null;

			foreach (BoxTree<ExceptionObject>.TileEntry tileEntry in _boxTree.Search(searchBox)
			)
			{
				ExceptionObject exceptionObject = tileEntry.Value;

				if (issueBox == null)
				{
					issueBox = GetBox(geometry);
				}

				if (Matches(exceptionObject, tileEntry.Box, issueBox))
				{
					yield return exceptionObject;
				}
				else
				{
					// Check if clipped envelope matches
					if (_areaOfInterestBox == null ||
					    _areaOfInterestBox.Contains(issueBox) ||
					    exceptionObject.AreaOfInterestShapeEnvelope == null)
					{
						continue;
					}

					if (clippedIssueBox == null)
					{
						clippedIssueBox = GetBox(GetClippedGeometry(geometry, _areaOfInterestBox));
					}

					if (Matches(exceptionObject, exceptionObject.AreaOfInterestShapeEnvelope,
					            clippedIssueBox))
					{
						yield return exceptionObject;
					}
				}
			}
		}

		[NotNull]
		public IEnumerable<ExceptionObject> Search(
			[NotNull] ExceptionObject exceptionObject)
		{
			if (_boxTree == null)
			{
				_boxTree = CreateBoxTree(_exceptionObjects, _xyTolerance);

				_exceptionObjects.Clear();
			}

			if (_boxTree.Count == 0 || exceptionObject.ShapeEnvelope == null)
			{
				yield break;
			}

			IBox searchBox = GeomUtils.CreateBox(exceptionObject.ShapeEnvelope, _xyTolerance);

			foreach (BoxTree<ExceptionObject>.TileEntry tileEntry in _boxTree.Search(searchBox)
			)
			{
				ExceptionObject candidateExceptionObject = tileEntry.Value;

				if (Matches(candidateExceptionObject, tileEntry.Box,
				            exceptionObject.ShapeEnvelope))
				{
					yield return candidateExceptionObject;
				}
			}
		}

		[NotNull]
		private IGeometry GetClippedGeometry([NotNull] IGeometry geometry,
		                                     [NotNull] IBox clipperBox)
		{
			_envelopeTemplate.PutCoords(clipperBox.Min.X, clipperBox.Min.Y,
			                            clipperBox.Max.X, clipperBox.Max.Y);

			var polygon = geometry as IPolygon;
			if (polygon != null)
			{
				// implements workarounds for QueryClipped issues on polygons
				return GeometryUtils.GetClippedPolygon(polygon, _envelopeTemplate);
			}

			IGeometry clipped = GeometryFactory.CreateEmptyGeometry(geometry);
			((ITopologicalOperator) geometry).QueryClipped(_envelopeTemplate, clipped);
			return clipped;
		}

		[NotNull]
		private static ExceptionsBoxTree CreateBoxTree(
			[NotNull] ICollection<ExceptionObject> exceptionObjects,
			double expansionDistance)
		{
			IBox box = GetBox(exceptionObjects, expansionDistance);

			var result = new ExceptionsBoxTree();

			if (box != null)
			{
				result.InitSize(new IGmtry[] {box});
			}

			foreach (ExceptionObject exceptionObject in exceptionObjects)
			{
				if (exceptionObject.ShapeEnvelope != null)
				{
					result.Add(exceptionObject.ShapeEnvelope, exceptionObject);
				}
			}

			return result;
		}

		[CanBeNull]
		private static IBox GetBox([NotNull] IEnumerable<ExceptionObject> exceptionObjects,
		                           double expansionDistance)
		{
			double xmin = double.MaxValue;
			double ymin = double.MaxValue;
			double xmax = double.MinValue;
			double ymax = double.MinValue;

			var hasBox = false;
			foreach (ExceptionObject exceptionObject in exceptionObjects)
			{
				IBox box = exceptionObject.ShapeEnvelope;
				if (box == null)
				{
					continue;
				}

				if (box.Min.X < xmin)
				{
					xmin = box.Min.X;
				}

				if (box.Min.Y < ymin)
				{
					ymin = box.Min.Y;
				}

				if (box.Max.X > xmax)
				{
					xmax = box.Max.X;
				}

				if (box.Max.Y > ymax)
				{
					ymax = box.Max.Y;
				}

				hasBox = true;
			}

			return hasBox
				       ? GeomUtils.CreateBox(xmin - expansionDistance,
				                             ymin - expansionDistance,
				                             xmax + expansionDistance,
				                             ymax + expansionDistance)
				       : null;
		}

		[NotNull]
		private IBox GetBox([NotNull] IGeometry geometry)
		{
			geometry.QueryEnvelope(_envelopeTemplate);
			return Assert.NotNull(GeometryUtils.Get2DBox(_envelopeTemplate));
		}

		private static bool Matches([NotNull] ExceptionObject exceptionObject,
		                            [NotNull] IBox exceptionBox,
		                            [NotNull] IBox issueBox)
		{
			if (exceptionObject.ShapeMatchCriterion == ShapeMatchCriterion.IgnoreShape)
			{
				return true;
			}

			const double toleranceFactor = 2;

			double xyTolerance = Assert.NotNull(exceptionObject.XYTolerance).Value;

			double matchTolerance = xyTolerance * toleranceFactor;

			switch (exceptionObject.ShapeMatchCriterion)
			{
				case ShapeMatchCriterion.EqualEnvelope:
					return AreEqual(exceptionBox, issueBox, matchTolerance);

				case ShapeMatchCriterion.WithinEnvelope:
					return Contains(exceptionBox, issueBox, matchTolerance);

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private static bool Contains([NotNull] IBox box,
		                             [NotNull] IBox otherBox,
		                             double xyTolerance)
		{
			if (box.Min.X - otherBox.Min.X > xyTolerance)
			{
				return false;
			}

			if (box.Min.Y - otherBox.Min.Y > xyTolerance)
			{
				return false;
			}

			if (otherBox.Max.X - box.Max.X > xyTolerance)
			{
				return false;
			}

			if (otherBox.Max.Y - box.Max.Y > xyTolerance)
			{
				return false;
			}

			return true;
		}

		private static bool AreEqual([NotNull] IBox box,
		                             [NotNull] IBox otherBox,
		                             double xyTolerance)
		{
			if (Math.Abs(box.Min.X - otherBox.Min.X) > xyTolerance)
			{
				return false;
			}

			if (Math.Abs(box.Min.Y - otherBox.Min.Y) > xyTolerance)
			{
				return false;
			}

			if (Math.Abs(box.Max.X - otherBox.Max.X) > xyTolerance)
			{
				return false;
			}

			if (Math.Abs(box.Max.Y - otherBox.Max.Y) > xyTolerance)
			{
				return false;
			}

			return true;
		}

		private class ExceptionsBoxTree : BoxTree<ExceptionObject>
		{
			private const int _dimension = 2;
			private const int _maximumElementCountPerTile = 16;
			private const bool _dynamic = true;

			public ExceptionsBoxTree()
				: base(_dimension, _maximumElementCountPerTile, _dynamic) { }
		}
	}
}
