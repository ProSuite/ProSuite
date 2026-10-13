using System;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Surface
{
	/// <summary>
	/// Keeps the frozen triangles themselves and tests them exactly. Nothing is written to the
	/// TIN, so no tag can be inherited by a successor triangle and the triangulation stays
	/// unconstrained.
	/// </summary>
	public sealed class FrozenTriangleIndexSpikeFreePointInserter
		: FreezingSpikeFreePointInserter
	{
		private FrozenTriangleGrid _frozenTriangles;

		public FrozenTriangleIndexSpikeFreePointInserter(
			double freezeDistance, double insertionBuffer)
			: base(freezeDistance, insertionBuffer) { }

		protected override SpikeFreeFreezeCarrier FreezeCarrier =>
			SpikeFreeFreezeCarrier.FrozenTriangleIndex;

		protected override void Prepare(ITinAdvanced advancedTin)
		{
			_frozenTriangles = new FrozenTriangleGrid(advancedTin.Extent);
		}

		protected override bool RejectsPointAt(ITinTriangle triangle, double x, double y)
		{
			return Assert.NotNull(_frozenTriangles).Contains(x, y);
		}

		protected override void Freeze(ITinEdit tin, ITinTriangle triangle)
		{
			triangle.QueryVertices(out WKSPointZ v0, out WKSPointZ v1, out WKSPointZ v2);

			Assert.NotNull(_frozenTriangles).Add(v0, v1, v2);
		}

		/// <summary>
		/// The frozen triangles in a bucket grid, cell size matched to their median area of
		/// 0.03 m2: only a few per cell, so the test is a handful of exact point-in-triangle
		/// checks.
		/// </summary>
		private sealed class FrozenTriangleGrid
		{
			private const double CellSize = 0.5;

			private readonly double _minX;
			private readonly double _minY;
			private readonly int _columns;
			private readonly int _rows;
			private readonly int[] _head;

			private int[] _next = new int[1 << 20];
			private int[] _entryTriangle = new int[1 << 20];
			private float[] _vertices = new float[6 << 18];
			private int _entryCount;
			private int _triangleCount;

			public FrozenTriangleGrid([NotNull] IEnvelope extent)
			{
				_minX = extent.XMin - 1;
				_minY = extent.YMin - 1;
				_columns = (int) Math.Ceiling((extent.Width + 2) / CellSize) + 1;
				_rows = (int) Math.Ceiling((extent.Height + 2) / CellSize) + 1;

				long cells = (long) _columns * _rows;

				if (cells > 100_000_000L)
				{
					throw new InvalidOperationException(
						$"Frozen triangle index would need {cells:N0} cells.");
				}

				_head = new int[cells];

				for (long i = 0; i < cells; i++)
				{
					_head[i] = -1;
				}
			}

			public void Add(WKSPointZ v0, WKSPointZ v1, WKSPointZ v2)
			{
				if (_triangleCount * 6 + 6 > _vertices.Length)
				{
					System.Array.Resize(ref _vertices, _vertices.Length * 2);
				}

				int triangle = _triangleCount++;
				int b = triangle * 6;
				_vertices[b] = (float) (v0.X - _minX);
				_vertices[b + 1] = (float) (v0.Y - _minY);
				_vertices[b + 2] = (float) (v1.X - _minX);
				_vertices[b + 3] = (float) (v1.Y - _minY);
				_vertices[b + 4] = (float) (v2.X - _minX);
				_vertices[b + 5] = (float) (v2.Y - _minY);

				double minX = Math.Min(v0.X, Math.Min(v1.X, v2.X));
				double maxX = Math.Max(v0.X, Math.Max(v1.X, v2.X));
				double minY = Math.Min(v0.Y, Math.Min(v1.Y, v2.Y));
				double maxY = Math.Max(v0.Y, Math.Max(v1.Y, v2.Y));

				int c0 = Math.Max(0, (int) ((minX - _minX) / CellSize));
				int c1 = Math.Min(_columns - 1, (int) ((maxX - _minX) / CellSize));
				int r0 = Math.Max(0, (int) ((minY - _minY) / CellSize));
				int r1 = Math.Min(_rows - 1, (int) ((maxY - _minY) / CellSize));

				for (int r = r0; r <= r1; r++)
				{
					for (int c = c0; c <= c1; c++)
					{
						if (_entryCount >= _next.Length)
						{
							System.Array.Resize(ref _next, _next.Length * 2);
							System.Array.Resize(ref _entryTriangle, _entryTriangle.Length * 2);
						}

						long cell = (long) r * _columns + c;
						_entryTriangle[_entryCount] = triangle;
						_next[_entryCount] = _head[cell];
						_head[cell] = _entryCount;
						_entryCount++;
					}
				}
			}

			public bool Contains(double x, double y)
			{
				var c = (int) ((x - _minX) / CellSize);
				var r = (int) ((y - _minY) / CellSize);

				if (c < 0 || c >= _columns || r < 0 || r >= _rows)
				{
					return false;
				}

				var px = (float) (x - _minX);
				var py = (float) (y - _minY);

				for (int e = _head[(long) r * _columns + c]; e >= 0; e = _next[e])
				{
					int b = _entryTriangle[e] * 6;
					float ax = _vertices[b], ay = _vertices[b + 1];
					float bx = _vertices[b + 2], by = _vertices[b + 3];
					float cx = _vertices[b + 4], cy = _vertices[b + 5];

					float d1 = (px - bx) * (ay - by) - (ax - bx) * (py - by);
					float d2 = (px - cx) * (by - cy) - (bx - cx) * (py - cy);
					float d3 = (px - ax) * (cy - ay) - (cx - ax) * (py - ay);

					// Equal signs on all three edge products means inside.
					bool negative = d1 < 0 || d2 < 0 || d3 < 0;
					bool positive = d1 > 0 || d2 > 0 || d3 > 0;

					if (! (negative && positive))
					{
						return true;
					}
				}

				return false;
			}
		}
	}
}
