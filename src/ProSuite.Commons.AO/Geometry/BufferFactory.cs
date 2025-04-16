using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geometry
{
	public class BufferFactory : IDisposable
	{
		private IBufferConstruction _construction;
		private IBufferConstructionProperties _properties;

		private IGeometryBag _templateBag;

		private IPolyline _dummyLine;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="BufferFactory"/> class.
		/// </summary>
		/// <param name="explodeBuffers">if set to <c>true</c> multipart buffers will be exploded.</param>
		/// <param name="densify">if set to <c>true</c> straight line segments are used to represent the buffer output (i.e. <see cref="GenerateCurves"/> is set to <c>false</c>). Otherwise, curve segments are used in the output.</param>
		/// <param name="densifyDeviation"> The maximum distance between a line connecting two buffer curve points and the true curve 
		/// (defaults to -1, indicating 1000 * xy tolerance of spatial reference of input geometries ).</param>
		public BufferFactory(bool explodeBuffers = false, bool densify = false,
		                     double densifyDeviation = -1)
		{
			_construction = new BufferConstructionClass();
			_properties = (IBufferConstructionProperties) _construction;

			_properties.ExplodeBuffers = explodeBuffers;

			if (densify)
			{
				_properties.GenerateCurves = false;
				_properties.DensifyDeviation = densifyDeviation;
			}
		}

		#endregion

		/// <summary>
		/// Specifies on which side of a polyline its buffer is constructed; defaults to 'esriBufferFull' (both sides).
		/// </summary>
		/// <value>The side option.</value>
		public esriBufferConstructionSideEnum SideOption
		{
			get { return _properties.SideOption; }
			set { _properties.SideOption = value; }
		}

		/// <summary>
		/// Excludes the inside of the input polygon from the output buffer (default = false).
		/// </summary>
		/// <value><c>true</c> if the inside of the input polygon is excluded; otherwise, <c>false</c>.</value>
		public bool OutsideOnly
		{
			get { return _properties.OutsideOnly; }
			set { _properties.OutsideOnly = value; }
		}

		/// <summary>
		/// Specifies the shape of the end caps of polyline buffers; defaults to 'esriBufferRound'.
		/// </summary>
		/// <value>The end option.</value>
		/// <remarks>When setting to Flat, it seems that this must be done BEFORE setting other properties, otherwise
		/// some unwanted tolerance is applied to the result and the line end points don't touch the buffer.</remarks>
		public esriBufferConstructionEndEnum EndOption
		{
			get { return _properties.EndOption; }
			set { _properties.EndOption = value; }
		}

		/// <summary>
		/// Specifies whether sequences of curve points are replaced with true curves in the output buffers (default is true).
		/// </summary>
		/// <value><c>true</c> if true curves are generated; otherwise, <c>false</c>.</value>
		public bool GenerateCurves
		{
			get { return _properties.GenerateCurves; }
			set { _properties.GenerateCurves = value; }
		}

		/// <summary>
		/// The maximum distance between a line connecting two buffer curve points and the true curve 
		/// (defaults to -1, indicating 1000 * xy tolerance of spatial reference of input geometries ).
		/// </summary>
		/// <value>The densify deviation.</value>
		public double DensifyDeviation
		{
			get { return _properties.DensifyDeviation; }
			set { _properties.DensifyDeviation = value; }
		}

		/// <summary>
		/// Specifies whether or not overlaps are preserved in the set of output buffers (default is false).
		/// </summary>
		/// <value>
		/// 	<c>true</c> if overlaps are removed in the set of output buffers; otherwise, <c>false</c>.
		/// </value>
		public bool UnionOverlappingBuffers
		{
			get { return _properties.UnionOverlappingBuffers; }
			set { _properties.UnionOverlappingBuffers = value; }
		}

		/// <summary>
		/// Specifies whether or not output buffers can have multiple outer rings (defaut is false).
		/// </summary>
		/// <value><c>true</c> if output buffers with multiple outer rings are exploded; otherwise, <c>false</c>.</value>
		public bool ExplodeBuffers
		{
			get { return _properties.ExplodeBuffers; }
			set { _properties.ExplodeBuffers = value; }
		}

		[NotNull]
		public IEnumerable<KeyValuePair<T, IPolygon>> Buffer<T>(
			[NotNull] IEnumerable<KeyValuePair<T, IGeometry>> mappedInput,
			double distance)
		{
			Assert.ArgumentNotNull(mappedInput, nameof(mappedInput));
			Assert.False(UnionOverlappingBuffers,
			             "Geometry mapping not possible with UnionOverlappingBuffers = true");
			Assert.False(ExplodeBuffers,
			             "Geometry mapping not possible with ExplodeBuffers = true");

			using (var geometryMapping = new BufferGeometryMapping<T>(mappedInput, distance))
			{
				_construction.ConstructBuffersByDistances(geometryMapping);

				return geometryMapping.Output;
			}
		}

		[NotNull]
		public IList<IPolygon> Buffer([NotNull] IGeometry input, double distance)
		{
			Assert.ArgumentNotNull(input, nameof(input));

			return input.IsEmpty
				       ? new List<IPolygon>(0)
				       : Buffer(new BufferInputGeometry(input), distance);
		}

		[NotNull]
		public IList<IPolygon> Buffer([NotNull] IEnumerable<IGeometry> input,
		                              double distance)
		{
			Assert.ArgumentNotNull(input, nameof(input));

			ICollection<IGeometry> inputCollection = CollectionUtils.GetCollection(input);

			if (inputCollection.Count == 0 || inputCollection.All(g => g.IsEmpty))
			{
				return new List<IPolygon>(0);
			}

			using (var inputGeometries = new BufferInputGeometries(inputCollection))
			{
				return Buffer(inputGeometries, distance);
			}
		}

		[NotNull]
		public IList<IPolygon> Buffer([NotNull] IEnumGeometry enumInput,
		                              double distance)
		{
			Assert.ArgumentNotNull(enumInput, nameof(enumInput));

			return HasOnlyEmptyElements(enumInput)
				       ? new List<IPolygon>(0)
				       : BufferCore(enumInput, distance);
		}

		[NotNull]
		public IList<IPolygon> Buffer([NotNull] IGeometryBag inputBag,
		                              [NotNull] IEnumerable<double> distances)
		{
			Assert.ArgumentNotNull(inputBag, nameof(inputBag));
			Assert.ArgumentNotNull(distances, nameof(distances));

			if (inputBag.IsEmpty || HasOnlyEmptyElements((IEnumGeometry) inputBag))
			{
				return new List<IPolygon>(0);
			}

			// NOTE: ConstructBuffersByDistances2() does NOT assign the spatial reference
			//		 to the output collection. ConstructBuffers() does.
			IGeometryCollection outputCollection = PrepareTemplateBag(inputBag.SpatialReference);

			try
			{
				_construction.ConstructBuffersByDistances2((IEnumGeometry) inputBag,
				                                           GetDoubleArray(distances),
				                                           outputCollection);

				return GetOutput(outputCollection);
			}
			finally
			{
				ResetTemplateBag();
			}
		}

		private static bool HasOnlyEmptyElements([NotNull] IEnumGeometry enumGeometry)
		{
			if (enumGeometry.Count == 0)
			{
				return true;
			}

			enumGeometry.Reset();

			try
			{
				IGeometry geometry;
				while ((geometry = enumGeometry.Next()) != null)
				{
					if (! geometry.IsEmpty)
					{
						return false;
					}
				}

				return true;
			}
			finally
			{
				enumGeometry.Reset();
			}
		}

		[NotNull]
		private IList<IPolygon> BufferCore([NotNull] IEnumGeometry enumInput,
		                                   double distance)
		{
			// does no longer work as of 9.3.1 (implementing IGeometryCollection is NOT sufficient)
			// BufferOutput output = new BufferOutput();

			if (_properties.EndOption == esriBufferConstructionEndEnum.esriBufferFlat)
			{
				ResetCorruptFlatEndEnvironment();
			}

			IGeometryCollection outputCollection = PrepareTemplateBag();

			try
			{
				try
				{
					_construction.ConstructBuffers(enumInput, distance,
					                               outputCollection);
				}
				catch (COMException comException)
				{
					_msg.Debug($"Error buffering {enumInput.Count} geometries with distance " +
					           $"{distance}. Checking geometries...", comException);

					// TOP-5939: Starting with 11.2 or 11.3 vertical polylines or those with several
					// points at the exact same XYZ location start throwing!

					enumInput.Reset();

					IGeometry currentGeometry = enumInput.Next();

					while (currentGeometry != null)
					{
						if (! currentGeometry.IsEmpty &&
						    currentGeometry is IPolycurve polycurve)
						{
							IPoint emergencyPoint = polycurve.FromPoint;

							var cloned = GeometryFactory.Clone(currentGeometry);
							GeometryUtils.Simplify(cloned, allowReorder: true);

							// Empty: all points in one location
							if (cloned.IsEmpty || ((IPolycurve) cloned).Length == 0)
							{
								// Let's use the start point instead
								IList<IPolygon> bufferResults = Buffer(emergencyPoint, distance);

								foreach (IPolygon polygon in bufferResults)
								{
									object missing = Type.Missing;
									outputCollection.AddGeometry(polygon, ref missing, ref missing);
								}
							}
						}

						currentGeometry = enumInput.Next();
					}

					if (outputCollection.GeometryCount == 0)
					{
						// Work around not effective...
						throw;
					}
				}
				catch (Exception)
				{
					_msg.DebugFormat(
						"Error buffering {0} geometries with distance {1}",
						enumInput.Count, distance);
					throw;
				}

				return GetOutput(outputCollection);
			}
			finally
			{
				ResetTemplateBag();
			}
		}

		private void ResetTemplateBag()
		{
			_templateBag.SetEmpty();
			_templateBag.SpatialReference = null;
		}

		private void ResetCorruptFlatEndEnvironment()
		{
			if (_dummyLine == null)
			{
				_dummyLine = GeometryFactory.CreatePolyline(0, 0, 10, 0);
			}

			// buffering any LINE resets whatever is the problem for flat end buffers, 
			// even when using the simple Buffer() method
			_construction.Buffer(_dummyLine, 1);
		}

		[NotNull]
		private static List<IPolygon> GetOutput(
			[NotNull] IGeometryCollection outputCollection)
		{
			int count = outputCollection.GeometryCount;

			var result = new List<IPolygon>(count);

			for (var i = 0; i < count; i++)
			{
				result.Add((IPolygon) outputCollection.Geometry[i]);
			}

			return result;
		}

		[NotNull]
		private static IDoubleArray GetDoubleArray([NotNull] IEnumerable<double> values)
		{
			Assert.ArgumentNotNull(values, nameof(values));

			IDoubleArray result = new DoubleArrayClass();

			foreach (double distance in values)
			{
				result.Add(distance);
			}

			return result;
		}

		[NotNull]
		private IGeometryCollection PrepareTemplateBag(
			[CanBeNull] ISpatialReference spatialReference = null)
		{
			if (_templateBag == null)
			{
				_templateBag = new GeometryBagClass();
			}
			else
			{
				_templateBag.SetEmpty();
				_templateBag.SpatialReference = null;
				((IEnumGeometry) _templateBag).Reset();
			}

			if (spatialReference != null)
			{
				_templateBag.SpatialReference = spatialReference;
			}

			return (IGeometryCollection) _templateBag;
		}

		#region Nested types

		#region Nested type: BufferGeometryMapping

		private class BufferGeometryMapping<T> : IGeometricBufferSourceSink, IDisposable
		{
			private readonly double _distance;
			private readonly Dictionary<int, T> _geomIdMap = new Dictionary<int, T>();
			private readonly IEnumerator<KeyValuePair<T, IGeometry>> _inputEnumerator;

			private readonly List<KeyValuePair<T, IPolygon>> _output =
				new List<KeyValuePair<T, IPolygon>>();

			private int _currentGeomId;

			/// <summary>
			/// Initializes a new instance of the <see cref="BufferGeometryMapping&lt;T&gt;"/> class.
			/// </summary>
			/// <param name="input">The input.</param>
			/// <param name="distance">The buffer distance.</param>
			public BufferGeometryMapping(
				[NotNull] IEnumerable<KeyValuePair<T, IGeometry>> input,
				double distance)
			{
				Assert.ArgumentNotNull(input, nameof(input));

				_distance = distance;

				_inputEnumerator = input.GetEnumerator();
			}

			[NotNull]
			public IEnumerable<KeyValuePair<T, IPolygon>> Output => _output;

			#region IDisposable Members

			public void Dispose()
			{
				_geomIdMap.Clear();
				_inputEnumerator.Dispose();
			}

			#endregion

			#region IGeometricBufferSourceSink Members

			void IGeometricBufferSourceSink.ReadNext(out int geomID,
			                                         out IGeometry nextGeometry,
			                                         out double pDistance)
			{
				if (_inputEnumerator.MoveNext())
				{
					_currentGeomId++;
					KeyValuePair<T, IGeometry> current = _inputEnumerator.Current;

					nextGeometry = current.Value;

					_geomIdMap.Add(_currentGeomId, current.Key);
				}
				else
				{
					nextGeometry = null;
				}

				pDistance = _distance;
				geomID = _currentGeomId;
			}

			void IGeometricBufferSourceSink.WriteNext(int geomID,
			                                          IGeometry bufferedGeometry)
			{
				Assert.ArgumentNotNull(bufferedGeometry, nameof(bufferedGeometry));

				T mapped;
				if (_geomIdMap.TryGetValue(geomID, out mapped))
				{
					_output.Add(new KeyValuePair<T, IPolygon>(mapped,
					                                          (IPolygon) bufferedGeometry));
				}
				else
				{
					throw new ArgumentException(
						string.Format("Invalid geomID: {0}", geomID), nameof(geomID));
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: BufferInputGeometries

		private class BufferInputGeometries : IEnumGeometry, IDisposable
		{
			private readonly IEnumerable<IGeometry> _geometries;
			private readonly IEnumerator<IGeometry> _enumerator;

			public BufferInputGeometries([NotNull] IEnumerable<IGeometry> geometries)
			{
				Assert.ArgumentNotNull(geometries, nameof(geometries));

				// ReSharper disable PossibleMultipleEnumeration
				_enumerator = geometries.GetEnumerator();
				_geometries = geometries;
				// ReSharper restore PossibleMultipleEnumeration
			}

			#region IDisposable Members

			public void Dispose()
			{
				_enumerator.Dispose();
			}

			#endregion

			#region IEnumGeometry Members

			IGeometry IEnumGeometry.Next()
			{
				return _enumerator.MoveNext()
					       ? _enumerator.Current
					       : null;
			}

			void IEnumGeometry.Reset()
			{
				_enumerator.Reset();
			}

			int IEnumGeometry.Count => new List<IGeometry>(_geometries).Count;

			#endregion
		}

		#endregion

		#region Nested type: BufferInputGeometry

		private class BufferInputGeometry : IEnumGeometry
		{
			private readonly IGeometry _geometry;
			private bool _atEnd;

			/// <summary>
			/// Initializes a new instance of the <see cref="BufferInputGeometry"/> class.
			/// </summary>
			/// <param name="geometry">The geometry.</param>
			public BufferInputGeometry([NotNull] IGeometry geometry)
			{
				Assert.ArgumentNotNull(geometry, nameof(geometry));

				_geometry = geometry;
			}

			#region IEnumGeometry Members

			public IGeometry Next()
			{
				if (_atEnd)
				{
					return null;
				}

				_atEnd = true;
				return _geometry;
			}

			public void Reset()
			{
				_atEnd = false;
			}

			public int Count => 1;

			#endregion
		}

		#endregion

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			ComUtils.ReleaseComObject(_construction);

			_properties = null;
			_construction = null;
		}

		#endregion
	}
}
