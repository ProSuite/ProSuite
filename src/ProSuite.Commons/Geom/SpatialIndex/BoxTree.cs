using System;
using System.Collections;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom.SpatialIndex
{
	public partial class BoxTree
	{
		private int _dimension;
		private Box _unitBox;
		private Box _mainBox;
		private int[] _mainCounter;
		private int[] _mainSize;

		// private Box mSearchBox;
		private int _maxElemPerTile;
		private int _nElemJoin;
		private readonly bool _dynamic;

		[NotNull] private BoxTile _mainTile;

		private const int _maximumAllowedTileLevels = 30;
		private int _maxDenominator = 1 << _maximumAllowedTileLevels;

		private const int _defaultMaxElementCountPerTile = 64; // TODO revise
		private const bool _defaultDynamic = true;

		internal BoxTree(int dimension, [NotNull] BoxTile mainTile)
			: this(dimension, _defaultMaxElementCountPerTile, _defaultDynamic, mainTile) { }

		internal BoxTree(int dimension, int nElem, bool dynamic, [NotNull] BoxTile mainTile)
		{
			_dimension = dimension;
			_mainTile = mainTile;
			_maxElemPerTile = nElem;
			_dynamic = dynamic;
		}

		public void Init(int dimension, Box box, int maxElemPerTile, int nElemJoin)
		{
			_dimension = dimension;
			_maxElemPerTile = maxElemPerTile;
			_nElemJoin = nElemJoin;

			_mainCounter = new int[dimension];
			_mainSize = new int[dimension];

			if (box != null)
			{
				_unitBox = box.Clone();
				_mainBox = _unitBox.Clone();
				for (var i = 0; i < dimension; i++)
				{
					_mainBox.Max[i] += _mainBox.Max[i] - _mainBox.Min[i];
					_mainSize[i] = 2;
				}
			}
			else
			{
				_unitBox = null;
				_mainBox = null;
			}
		}

		public void Init(int dimension, int maxElemPerTile, int nElemJoin)
		{
			Init(dimension, null, maxElemPerTile, nElemJoin);
		}

		public void Init(int dimension, int maxElemPerTile)
		{
			Init(dimension, null, maxElemPerTile, maxElemPerTile / 2);
		}

		public void Init(Box box, int maxElemPerTile, int nElemJoin)
		{
			Init(box.Dimension, box, maxElemPerTile, nElemJoin);
		}

		public void Init(Box box, int maxElemPerTile)
		{
			Init(box.Dimension, box, maxElemPerTile, maxElemPerTile / 2);
		}

		public void InitSize([NotNull] IEnumerable<IGmtry> data)
		{
			InitSize((IEnumerable) data);
		}

		private void InitSize()
		{
			InitSize(_mainTile.EnumElems());
		}

		private void InitSize([NotNull] IEnumerable data)
		{
			Assert.ArgumentNotNull(data, nameof(data));

			_unitBox = null;

			foreach (object row in data)
			{
				var geometry = row as IGmtry;
				IBox box = geometry?.Extent ?? ((TileEntry) row).Box;

				if (_unitBox == null)
				{
					Pnt min = Pnt.Create(_dimension);
					Pnt max = Pnt.Create(_dimension);
					for (var i = 0; i < _dimension; i++)
					{
						min[i] = box.Min[i];
						max[i] = box.Max[i];
					}

					_unitBox = new Box(min, max);
					continue;
				}

				for (var i = 0; i < _dimension; i++)
				{
					if (box.Min[i] < _unitBox.Min[i])
					{
						_unitBox.Min[i] = box.Min[i];
					}

					if (box.Max[i] > _unitBox.Max[i])
					{
						_unitBox.Max[i] = box.Max[i];
					}
				}
			}

			if (_unitBox == null)
			{
				throw new InvalidOperationException("data containes no values");
			}

			// double the size of each dimension
			_mainBox = _unitBox.Clone();
			_mainCounter = new int[_dimension];
			_mainSize = new int[_dimension];

			for (var i = 0; i < _dimension; i++)
			{
				_mainBox.Max[i] = 2 * _unitBox.Max[i] - _unitBox.Min[i];
				_mainCounter[i] = 0;
				_mainSize[i] = 2;
			}
		}

		public Box UnitBox => _unitBox;

		[NotNull]
		internal BoxTile MainTile => _mainTile;

		internal Box MainBox => _mainBox;

		public bool Dynamic => _dynamic;

		public int NElemJoin => _nElemJoin;

		public int MaxTileLevels
		{
			get
			{
				var levels = 0;
				int denominator = _maxDenominator;
				while (denominator > 1)
				{
					levels++;
					denominator /= 2;
				}

				return levels;
			}
			set
			{
				Assert.ArgumentCondition(value <= _maximumAllowedTileLevels,
				                         "Maximum allowed tile level: {0}",
				                         _maximumAllowedTileLevels);
				Assert.ArgumentCondition(value > 0, "Maximum tile level must be > 0");

				_maxDenominator = 1 << value;
			}
		}

		public Box GetTile(IBox box, double maxSize)
		{
			if (_mainBox == null)
			{
				throw new InvalidOperationException("main box not initialized");
			}

			VerifyExtent(box);

			IList<int> counter0 = null;
			IList<int> denominator = null;

			if (_unitBox != null)
			{
				GetPositions(out counter0, out denominator);
			}

			BoxTile addTile = FindAddTile(_mainTile, box, true, counter0, denominator);

			while (addTile.MaxInParentSplitDim - addTile.MinInParentSplitDim > maxSize)
			{
				addTile.Split(_unitBox, counter0, denominator, _mainCounter, _mainSize);
				BoxTile childTile = FindAddTile(addTile, box, false, counter0, denominator);
				if (childTile == addTile)
				{
					break;
				}

				addTile = childTile;
			}

			Box tileBox = GetBox(addTile);

			return tileBox;
		}

		[NotNull]
		public List<Box> GetLeaves(bool nonEmpty = false)
		{
			var leaves = new List<BoxTile>();

			AddLeaves(_mainTile, leaves, nonEmpty);
			var leafBoxs = new List<Box>(leaves.Count);
			foreach (BoxTile leaf in leaves)
			{
				leafBoxs.Add(GetBox(leaf));
			}

			return leafBoxs;
		}

		private void AddLeaves([NotNull] BoxTile parent, [NotNull] List<BoxTile> leaves,
		                       bool nonEmpty)
		{
			if (parent.Child0 == null)
			{
				if (! nonEmpty || parent.ElemsCount > 0)
				{
					leaves.Add(parent);
				}

				return;
			}

			AddLeaves(parent.Child0, leaves, nonEmpty);
			AddLeaves(parent.Child1, leaves, nonEmpty);
		}

		internal bool VerifyPointTree()
		{
			return VerifyTile(_mainTile);
		}

		[CanBeNull]
		protected virtual IEnumerator<TileEntry> GetTileEnumerator(
			TileEntryEnumerator enumerator, IEnumerable<TileEntry> list)
		{
			return list?.GetEnumerator();
		}

		private bool VerifyTile(BoxTile tile)
		{
			if (tile == null)
			{
				return true;
			}

			if (VerifyTile(tile.Child0) == false)
			{
				return false;
			}

			if (VerifyTile(tile.Child1) == false)
			{
				return false;
			}

			return tile.VerifyPointTile(this);
		}

		internal BoxTile StartFindAddTile(IBox geomExtent, bool dynamic)
		{
			IList<int> counter0 = null;
			IList<int> denominator = null;

			if (dynamic && _unitBox != null)
			{
				GetPositions(out counter0, out denominator);
			}

			return FindAddTile(_mainTile, geomExtent, dynamic, counter0, denominator);
		}

		private void GetPositions(out IList<int> counter0, out IList<int> denominator)
		{
			counter0 = new int[_dimension];
			denominator = new List<int>(_dimension);
			for (var i = 0; i < _dimension; i++)
			{
				denominator.Add(4);
			}
		}

		/// <summary>
		/// find tile where geomExtent fits inside
		/// </summary>
		[NotNull]
		private BoxTile FindAddTile([NotNull] BoxTile parent,
		                            [NotNull] IBox geomExtent,
		                            bool dynamic,
		                            IList<int> counter0,
		                            IList<int> denominator)
		{
			BoxTile child0 = parent.Child0;
			if (child0 != null)
			{
				BoxTile child1 = parent.Child1;
				int divDim = parent.SplitDimension;

				if (geomExtent.Min[divDim] < child1.MinInParentSplitDim)
				{
					if (geomExtent.Max[divDim] < child0.MaxInParentSplitDim)
					{
						if (dynamic)
						{
							counter0[divDim] *= 2;
							denominator[divDim] *= 2;
						}

						bool addDynamic = dynamic;
						if (denominator[divDim] >= _maxDenominator)
						{
							addDynamic = false;
						}

						return FindAddTile(child0, geomExtent, addDynamic, counter0, denominator);
					}

					return parent;
				}

				if (geomExtent.Max[divDim] < child1.MaxInParentSplitDim)
				{
					if (dynamic)
					{
						counter0[divDim] += counter0[divDim] + 1;
						denominator[divDim] *= 2;
					}

					bool addDynamic = dynamic;
					if (denominator[divDim] >= _maxDenominator)
					{
						addDynamic = false;
					}

					return FindAddTile(child1, geomExtent, addDynamic, counter0, denominator);
				}

				return parent;
			}

			// tile has no children
			if (! dynamic || parent.ElemsCount < _maxElemPerTile)
			{
				return parent;
			}

			// split tile and try again
			if (_unitBox == null)
			{
				InitSize();
				VerifyExtent(geomExtent);

				return StartFindAddTile(geomExtent, true);
			}

			parent.Split(_unitBox, counter0, denominator, _mainCounter, _mainSize);
			return FindAddTile(parent, geomExtent, false, counter0, denominator);
		}

		protected void VerifyExtent([NotNull] IBox box)
		{
			if (_mainBox != null)
			{
				int dim = _mainBox.ExceedDimension(box);
				while (dim != 0)
				{
					Enlarge(dim);
					dim = _mainBox.ExceedDimension(box);
				}
			}
		}

		/// <summary>
		/// Enlarge Region. Remark: when data is load, numerical problems may occure
		/// </summary>
		/// <param name="directedDimension">directed dimension to enlarge: (-dim -1) : add negativ (dim + 1) : add positiv </param>
		/// <returns>new parent tile</returns>
		[NotNull]
		private BoxTile Enlarge(int directedDimension)
		{
			int dimension = Math.Abs(directedDimension) - 1;

			BoxTile newTile = _mainTile.CreateEmptyTile();

			int size = _mainSize[dimension];

			_mainSize[dimension] *= 2;

			double u0 = _unitBox.Min[dimension];
			double u = _unitBox.Max[dimension] - _unitBox.Min[dimension];
			if (directedDimension > 0)
			{
				_mainBox.Max[dimension] = Position(u0, u, _mainCounter[dimension] + 2 * size, 1);

				newTile.InitChildren(_mainTile, _mainTile.CreateEmptyTile(),
				                     u0, u, _mainCounter[dimension], size / 2, 1);
			}
			else
			{
				if ((size & 1) != 0)
				{
					throw new InvalidProgramException(
						string.Format(
							"Error in software design assumption size {0} must be divisible by 2",
							size));
				}

				_mainCounter[dimension] -= size / 2;

				_mainBox.Min[dimension] = Position(u0, u, _mainCounter[dimension], 1);
				_mainBox.Max[dimension] = Position(u0, u, _mainCounter[dimension] + 2 * size, 1);

				newTile.InitChildren(_mainTile.CreateEmptyTile(), _mainTile,
				                     u0, u, _mainCounter[dimension], size / 2, 1);
			}

			newTile.SplitDimension = dimension;
			_mainTile = newTile;

			return newTile;
		}

		/// <summary>
		/// Get the Extent of Tile
		/// </summary>
		[NotNull]
		private Box TileExtent([NotNull] BoxTile tile)
		{
			Box box = _mainBox.Clone();
			BoxTile child = tile;
			BoxTile parent = child.Parent;
			double d = -1;
			double dMax = 0;

			// proceed up so long until it is verified that all dimensions of the 
			// box are set to the correct extent
			while (parent != null && d <= dMax)
			{
				int splitDim = parent.SplitDimension;

				d = child.MaxInParentSplitDim - child.MinInParentSplitDim;
				if (box.Min[splitDim] < child.MinInParentSplitDim)
				{
					box.Min[splitDim] = child.MinInParentSplitDim;
				}

				if (box.Max[splitDim] > child.MaxInParentSplitDim)
				{
					box.Max[splitDim] = child.MaxInParentSplitDim;
				}

				if (Math.Abs(dMax) < double.Epsilon)
				{
					dMax = parent.MaxInSplitDim - parent.MinInSplitDim;
					// dMax was the largest extent when splitting the parent of tile,
					// maybe together with others.
					// So if d > dMax, it must be dimension, that was split already
				}

				child = parent;
				parent = parent.Parent;
			}

			return box;
		}

		[NotNull]
		private double[] GetExtent([NotNull] BoxTile tile, int dimension)
		{
			BoxTile t = tile;
			while (t.Parent != null)
			{
				if (dimension == t.Parent.SplitDimension)
				{
					return new[] {t.MinInParentSplitDim, t.MaxInParentSplitDim};
				}

				t = t.Parent;
			}

			return new[] {_mainBox.Min[dimension], _mainBox.Max[dimension]};
		}

		[NotNull]
		private Box GetBox([NotNull] BoxTile tile)
		{
			Pnt min = Pnt.Create(_dimension);
			Pnt max = Pnt.Create(_dimension);
			var handled = new bool[_dimension];
			var nHandled = 0;
			BoxTile t = tile;
			while (t.Parent != null && nHandled < _dimension)
			{
				if (! handled[t.Parent.SplitDimension])
				{
					min[t.Parent.SplitDimension] = t.MinInParentSplitDim;
					max[t.Parent.SplitDimension] = t.MaxInParentSplitDim;
					handled[t.Parent.SplitDimension] = true;
					nHandled++;
				}

				t = t.Parent;
			}

			if (nHandled < _dimension)
			{
				for (var iHandled = 0; iHandled < _dimension; iHandled++)
				{
					if (! handled[iHandled])
					{
						min[iHandled] = _mainBox.Min[iHandled];
						max[iHandled] = _mainBox.Max[iHandled];
					}
				}
			}

			return new Box(min, max);
		}

		protected static double Position(double u0, double u, int counter, int denominator)
		{
			while (denominator > 1 && (counter & 1) == 0)
			{
				denominator /= 2;
				counter /= 2;
			}

			double pos = u0 + counter * u / denominator;
			return pos;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="currentTile"></param>
		/// <param name="currentBox">begin: box of current tile; end: box of return tile</param>
		/// <param name="extent">extent which must intersect or touch next tile</param>
		/// <param name="stopTile"></param>
		/// <returns></returns>
		[CanBeNull]
		internal static BoxTile NextSelTile([NotNull] BoxTile currentTile,
		                                    [NotNull] Box currentBox,
		                                    [CanBeNull] IBox extent,
		                                    BoxTile stopTile)
		{
			int splitDim = currentTile.SplitDimension;
			BoxTile child = currentTile.Child0;
			if (child != null)
			{
				if (Math.Abs(currentBox.Min[splitDim] - child.MinInParentSplitDim) >
				    double.Epsilon)
				{
					throw new InvalidProgramException(
						"Error in software design assumption: SplitDim");
				}

				currentBox.Max[splitDim] = child.MaxInParentSplitDim;
				if (extent == null || currentBox.Intersects(extent))
				{
					return child;
				}
			}

			while (currentTile != stopTile && currentTile != null)
			{
				if (child != currentTile.Child1)
				{
					child = currentTile.Child1;
					currentBox.Min[splitDim] = child.MinInParentSplitDim;
					currentBox.Max[splitDim] = child.MaxInParentSplitDim;
					if (extent == null || currentBox.Intersects(extent))
					{
						return child;
					}
				}

				// reset box to current extent
				if (child != null)
				{
					currentBox.Min[splitDim] = currentTile.MinInSplitDim;
					currentBox.Max[splitDim] = currentTile.MaxInSplitDim;
				}

				// one step up
				child = currentTile;
				currentTile = currentTile.Parent;
				if (currentTile != null)
				{
					splitDim = currentTile.SplitDimension;
				}
			}

			// für genuegend unterteilte Tiles (sonst ev. Min.X == 0 o.ae.):
			// Debug.Assert(currentBox.Equals(mMainBox)); 
			return null;
		}

		#region IGeometry Members

		public int Dimension => _dimension;

		public int Topology => _dimension;

		public Box Extent => _mainBox;

		public bool Intersects(Box box)
		{
			return Extent.Intersects(box);
		}

		#endregion

		#region IList Members

		public void Clear()
		{
			_mainTile = _mainTile.CreateEmptyTile();
		}

		public bool IsFixedSize => false;

		#endregion

		#region ICollection Members

		public int Count => _mainTile.Count;

		#endregion

		public abstract class TileEntry
		{
			private readonly IBox _box;

			protected TileEntry([NotNull] IBox box)
			{
				_box = box;
			}

			public IBox Box => _box;
		}

		internal abstract class BoxTile
		{
			private BoxTile _parent;
			private BoxTile _child0;
			private BoxTile _child1;
			private int _splitDimension;
			private int _count;

			private double _minSplitDim;
			private double _maxSplitDim;
			private double _minParentDim;
			private double _maxParentDim;

			//private static int _currentId;
			//private readonly int _id;
			//protected BoxTile()
			//{
			//  _currentId++;
			//  _id = _currentId;
			//}

			public double MinInParentSplitDim
			{
				get { return _minParentDim; }
				set { _minParentDim = value; }
			}

			public double MaxInParentSplitDim
			{
				get { return _maxParentDim; }
				set { _maxParentDim = value; }
			}

			public int Count
			{
				get { return _count; }
				internal set { _count = value; }
			}

			public double MinInSplitDim
			{
				get { return _minSplitDim; }
				set { _minSplitDim = value; }
			}

			public double MaxInSplitDim
			{
				get { return _maxSplitDim; }
				set { _maxSplitDim = value; }
			}

			public BoxTile Parent
			{
				get { return _parent; }
				private set { _parent = value; }
			}

			public BoxTile Child0 => _child0;

			public BoxTile Child1 => _child1;

			public int SplitDimension
			{
				get { return _splitDimension; }
				set { _splitDimension = value; }
			}

			internal abstract BoxTile CreateEmptyTile();

			protected abstract IEnumerable<TileEntry> InitSplit();

			internal abstract IEnumerable<TileEntry> EnumElems();

			internal abstract int ElemsCount { get; }

			internal void InitChildren(BoxTile child0, BoxTile child1, double u0, double u,
			                           int counter0, int size, int denominator)
			{
				_child0 = child0;
				_child0.Parent = this;
				_child0.MinInParentSplitDim = Position(u0, u, counter0, denominator);
				//== _minSplitDim;
				_child0.MaxInParentSplitDim = Position(u0, u, counter0 + 2 * size, denominator);
				//(_maxSplitDim + _minSplitDim) / 2.0;

				// double d;
				_child1 = child1;
				_child1.Parent = this;
				// d = (_maxSplitDim - _minSplitDim) / 4.0;
				_child1.MinInParentSplitDim = Position(u0, u, counter0 + size, denominator);
				//_minSplitDim + d;
				_child1.MaxInParentSplitDim = Position(u0, u, counter0 + 3 * size, denominator);
				//_maxSplitDim - d;
			}

			/// <summary>
			/// Split the tile along the largest dimesion of box
			/// Assumption: extent represents the actual extent of the tile (see method BoxExtent in BoxTree)
			/// </summary>
			public void Split(Box unit, IList<int> counter0, IList<int> denominator,
			                  IList<int> mainCounter, IList<int> mainSize)
			{
				int dim = counter0.Count;

				if (_child0 != null)
				{
					throw new InvalidProgramException("child0 already initialized");
				}

				if (_child1 != null)
				{
					throw new InvalidProgramException("child1 already initialized");
				}

				double d0 = (unit.Max[0] - unit.Min[0]) * mainSize[0] / denominator[0];
				var splitDim = 0;
				for (var i = 1; i < dim; i++)
				{
					double d = (unit.Max[i] - unit.Min[i]) * mainSize[i] / denominator[i];
					if (d0 < d)
					{
						d0 = d;
						splitDim = i;
					}
				}

				double u0 = unit.Min[splitDim];
				double u = unit.Max[splitDim] - unit.Min[splitDim];

				//TODO: verify
				_splitDimension = splitDim;
				int c0 = 2 * mainSize[splitDim] * counter0[splitDim] +
				         mainCounter[splitDim] * denominator[splitDim];
				_minSplitDim = Position(u0, u, c0, denominator[splitDim]);
				_maxSplitDim = Position(u0, u, c0 + 4 * mainSize[splitDim], denominator[splitDim]);

				InitChildren(CreateEmptyTile(), CreateEmptyTile(),
				             u0, u, c0, mainSize[splitDim], denominator[splitDim]);

				if (_child0 == null)
				{
					throw new InvalidProgramException("child0 not initiatlized");
				}

				if (_child1 == null)
				{
					throw new InvalidProgramException("child1 not initiatlized");
				}

				double tx1 = _child0.MaxInParentSplitDim;

				double ux0 = _child1.MinInParentSplitDim;
				double ux1 = _child1.MaxInParentSplitDim;

				_count = 0;
				const bool splitting = true;
				foreach (TileEntry entry in InitSplit())
				{
					double dMin = entry.Box.Min[splitDim];
					double dMax = entry.Box.Max[splitDim];
					if (dMin < ux0)
					{
						if (dMax < tx1)
						{
							_child0.Add(entry, splitting);
						}
						else
						{
							Add(entry, splitting);
						} // TODO: do not add directly to mElemList (sorting in future);
					}
					else
					{
						if ((dMin <= tx1) == false)
						{
							BoxTile child = this;
							BoxTile parent = _parent;
							while (parent != null)
							{
								if (parent.SplitDimension == splitDim && parent.Child0 == child)
								{
									throw new InvalidProgramException("dMin <= tx1");
								}

								child = parent;
								parent = parent.Parent;
							}
						}

						if (dMax < ux1)
						{
							_child1.Add(entry, splitting);
						}
						else
						{
							Add(entry, splitting);
						}
					}
				}

				_child0.Count = _child0.ElemsCount;
				_child1.Count = _child1.ElemsCount;
				_count = _child0.Count + _child1.Count + ElemsCount;
			}

			protected abstract void AddCore([NotNull] TileEntry entry);

			protected abstract void AddRange([NotNull] BoxTile child);

			protected void Add(TileEntry entry, bool splitting)
			{
				// TODO: implement sorting
				AddCore(entry);

				if (splitting)
				{
					return;
				}

				_count++;

				BoxTile parent = Parent;
				while (parent != null)
				{
					parent.Count++;
					parent = parent.Parent;
				}
			}

			public void Collapse()
			{
				if ((_child0.Child0 == null) == false)
				{
					throw new InvalidProgramException("_child0.child0 == null");
				}

				if ((_child1.Child0 == null) == false)
				{
					throw new InvalidProgramException("_child1.child0 == null");
				}

				AddRange(_child0);
				AddRange(_child1);

				_child0 = null;
				_child1 = null;
			}

			internal bool VerifyPointTile(BoxTree tree)
			{
				IBox extent = tree.TileExtent(this);
				foreach (TileEntry entry in EnumElems())
				{
					if (extent.Contains(entry.Box) == false)
					{
						return false;
					}
				}

				return true;
			}
		}

		protected class xTileHandler
		{
			private BoxTile _tile;
			private Box _box;

			public static xTileHandler CreateMain([NotNull] BoxTree tree)
			{
				var created = new xTileHandler
				              {
					              _tile = tree._mainTile,
					              _box = tree._mainBox.Clone()
				              };
				return created;
			}

			internal BoxTile Tile => _tile;

			public Box Box => _box;

			public int SplitDimension => _tile.SplitDimension;

			internal IEnumerable<TileEntry> ElemList => _tile.EnumElems();

			public int ElemsCount => _tile.ElemsCount;

			public xTileHandler GetChild0()
			{
				BoxTile child = _tile.Child0;
				if (child == null)
				{
					return null;
				}

				int splitDim = _tile.SplitDimension;

				Box childBox = _box.Clone();
				childBox.Max[splitDim] = child.MaxInParentSplitDim;
				var childTile = new xTileHandler {_tile = child, _box = childBox};
				return childTile;
			}

			public xTileHandler GetChild1()
			{
				BoxTile child = _tile.Child1;
				if (child == null)
				{
					return null;
				}

				int splitDim = _tile.SplitDimension;

				Box childBox = _box.Clone();
				childBox.Min[splitDim] = child.MinInParentSplitDim;
				childBox.Max[splitDim] = child.MaxInParentSplitDim;

				var childTile = new xTileHandler {_tile = child, _box = childBox};
				return childTile;
			}
		}
	}

	public partial class BoxTree<T> : BoxTree
	{
		public BoxTree(int dimension)
			: base(dimension, new BoxTile()) { }

		public BoxTree(int dimension, int nElem, bool dynamic)
			: base(dimension, nElem, dynamic, new BoxTile()) { }

		internal new BoxTile MainTile => (BoxTile) base.MainTile;

		public TileEntry this[int index] => MainTile[index];

		public int Add([NotNull] IBox box, T value)
		{
			VerifyExtent(box);

			var addTile = (BoxTile) StartFindAddTile(box, Dynamic);
			addTile.Add(box, value);

			return MainTile.Count - 1;
		}

		/// <summary>
		/// Inserts a geometry at position.
		/// </summary>
		public void Insert(int index, [NotNull] TileEntry entry)
		{
			// TODO , only valid if just one tail exists
			MainTile.Insert(index, entry);
		}

		public void Remove([NotNull] TileEntry entry)
		{
			var tile = (BoxTile) StartFindAddTile(entry.Box, false);
			tile.Remove(entry);

			if (Dynamic && tile.Child0 == null && tile.Parent != null &&
			    tile.Parent.Child0.ElemList.Count + tile.Parent.Child1.ElemList.Count +
			    tile.Parent.ElemList.Count < NElemJoin)
			{
				tile.Parent.Collapse();
			}
		}

		[NotNull]
		public TileEntryEnumerable Search(IBox box)
		{
			return new TileEntryEnumerable(this, box);
		}

		public bool Contains([NotNull] TileEntry value)
		{
			var tile = (BoxTile) StartFindAddTile(value.Box, false);
			if (tile == null)
			{
				return false;
			}

			return tile.ElemList.Contains(value);
		}

		public void Sort(IComparer<TileEntry> comparer)
		{
			Sort(MainTile, comparer);
		}

		private void Sort([CanBeNull] BoxTile tile, IComparer<TileEntry> comparer)
		{
			if (tile == null)
			{
				return;
			}

			tile.ElemList.Sort(comparer);

			Sort(tile.Child0, comparer);
			Sort(tile.Child1, comparer);
		}

		public new class TileEntry : BoxTree.TileEntry
		{
			private readonly T _value;

			public TileEntry([NotNull] IBox box, T value)
				: base(box)
			{
				_value = value;
			}

			[NotNull]
			public T Value => _value;
		}

		internal new sealed class BoxTile : BoxTree.BoxTile
		{
			private List<TileEntry> _elemList = new List<TileEntry>();

			public new BoxTile Parent => (BoxTile) base.Parent;

			public new BoxTile Child0 => (BoxTile) base.Child0;

			public new BoxTile Child1 => (BoxTile) base.Child1;

			internal override BoxTree.BoxTile CreateEmptyTile()
			{
				return new BoxTile();
			}

			protected override void AddCore(BoxTree.TileEntry entry)
			{
				_elemList.Add((TileEntry) entry);
			}

			protected override IEnumerable<BoxTree.TileEntry> InitSplit()
			{
				List<TileEntry> toSplit = _elemList;
				_elemList = new List<TileEntry>();
				foreach (TileEntry splitEntry in toSplit)
				{
					yield return splitEntry;
				}
			}

			internal override IEnumerable<BoxTree.TileEntry> EnumElems()
			{
				foreach (TileEntry entry in _elemList)
				{
					yield return entry;
				}
			}

			public List<TileEntry> ElemList => _elemList;

			internal override int ElemsCount => _elemList.Count;

			[NotNull]
			public TileEntry this[int index]
			{
				get
				{
					if (index < _elemList.Count)
					{
						return _elemList[index];
					}

					index -= _elemList.Count;
					if (index < Child0.Count)
					{
						return Child0[index];
					}

					index -= Child0.Count;
					return Child1[index];
				}
			}

			public void Add([NotNull] IBox box, T value)
			{
				Add(new TileEntry(box, value), false);
			}

			protected override void AddRange(BoxTree.BoxTile child)
			{
				var tile = (BoxTile) child;
				AddRange(tile._elemList);
			}

			private void AddRange([NotNull] List<TileEntry> elemList)
			{
				// TODO: implement sorting
				_elemList.AddRange(elemList);
			}

			public void Insert(int index, [NotNull] TileEntry entry)
			{
				Count++;
				_elemList.Insert(index, entry);

				BoxTree.BoxTile parent = Parent;
				while (parent != null)
				{
					parent.Count++;
					parent = parent.Parent;
				}
			}

			public void Remove([NotNull] TileEntry entry)
			{
				_elemList.Remove(entry);

				Count--;
				BoxTree.BoxTile parent = Parent;
				while (parent != null)
				{
					parent.Count--;
					parent = parent.Parent;
				}
			}
		}
	}
}