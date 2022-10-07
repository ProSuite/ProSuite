using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom.SpatialIndex
{
	public class BoxTreeSearcher<T> : ISpatialSearcher<T>
	{
		private readonly BoxTree<T> _boxTree;
		private readonly Box _searchBox;

		public BoxTreeSearcher(BoxTree<T> boxTree)
		{
			_boxTree = boxTree;
			_searchBox = new Box(new Pnt2D(), new Pnt2D());
		}

		public static BoxTreeSearcher<int> CreateSpatialSearcher(Linestring linestring)
		{
			BoxTree<int> boxTree = BoxTreeUtils.CreateBoxTree<int>(
				linestring.XMin, linestring.YMin, linestring.XMax, linestring.YMax,
				maxElementsPerTile: 8);

			var i = 0;
			foreach (Line3D line in linestring.Segments)
			{
				Box box2D = GeomUtils.CreateBox(line.Extent.Min.X, line.Extent.Min.Y,
				                                line.Extent.Max.X, line.Extent.Max.Y);
				boxTree.Add(box2D, i++);
			}

			return new BoxTreeSearcher<int>(boxTree);
		}

		public IEnumerable<T> Search(IBox searchBox, double tolerance)
		{
			return Search(searchBox.Min.X, searchBox.Min.Y, searchBox.Max.X,
			              searchBox.Max.Y, tolerance);
		}

		public IEnumerable<T> Search(
			double xMin, double yMin, double xMax, double yMax,
			double tolerance, Predicate<T> predicate = null)
		{
			// Avoid array instantiation for better performance:
			_searchBox.Min.X = xMin - tolerance;
			_searchBox.Min.Y = yMin - tolerance;
			_searchBox.Max.X = xMax + tolerance;
			_searchBox.Max.Y = yMax + tolerance;

			BoxTree<T>.TileEntryEnumerable tileEntries = _boxTree.Search(_searchBox);

			foreach (BoxTree<T>.TileEntry tileEntry in tileEntries)
			{
				if (predicate == null || predicate(tileEntry.Value))
				{
					yield return tileEntry.Value;
				}
			}
		}

		public IEnumerable<T> Search(
			double xMin, double yMin, double xMax, double yMax,
			[NotNull] IBoundedXY knownBounds, double tolerance,
			Predicate<T> predicate = null)
		{
			xMin = Math.Max(xMin, knownBounds.XMin);
			yMin = Math.Max(yMin, knownBounds.YMin);

			xMax = Math.Min(xMax, knownBounds.XMax);
			yMax = Math.Min(yMax, knownBounds.YMax);

			return Search(xMin, yMin, xMax, yMax, tolerance, predicate);
		}
	}
}
