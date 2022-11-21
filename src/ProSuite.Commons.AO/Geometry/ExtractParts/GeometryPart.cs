using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry.ExtractParts
{
	public class GeometryPart
	{
		private double? _size;

		public GeometryPart([NotNull] IGeometry lowLevelGeometry)
		{
			Assert.ArgumentNotNull(lowLevelGeometry, nameof(lowLevelGeometry));

			LowLevelGeometries = new List<IGeometry> {lowLevelGeometry};

			InnerRings = new List<IGeometry>();
		}

		public GeometryPart([NotNull] GeometryPart geometryPart)
		{
			Assert.ArgumentNotNull(geometryPart, nameof(geometryPart));

			LowLevelGeometries =
				new List<IGeometry>(geometryPart.LowLevelGeometries.Count);
			InnerRings = new List<IGeometry>(geometryPart.InnerRings.Count);

			JoinPart(geometryPart);
		}

		[CanBeNull]
		public string LabelText { get; set; }

		public List<IGeometry> LowLevelGeometries { get; }

		public List<IGeometry> InnerRings { get; }

		public IGeometry FirstGeometry
			=> LowLevelGeometries.Count == 0 ? null : LowLevelGeometries[0];

		[CanBeNull]
		public IRing MainOuterRing => FirstGeometry as IRing;

		public double Size
		{
			get
			{
				if (_size == null)
				{
					_size = GetSize(LowLevelGeometries);
				}

				return (double) _size;
			}
		}

		public Color Color { get; set; }

		public bool Selected { get; set; }

		public override string ToString()
		{
			return
				$"GeometryPart with low-level geometry count {LowLevelGeometries.Count} and size {Size}";
		}

		public void AddInnerRingGeometry([NotNull] IGeometry geometry)
		{
			LowLevelGeometries.Add(geometry);

			InnerRings.Add(geometry);
		}

		public esriGeometryType GetHighLevelGeometryType()
		{
			Assert.NotNull(FirstGeometry, "No geometry");

			return GetHighLevelGeometryType(FirstGeometry);
		}

		public bool Intersects([NotNull] IGeometry geometry)
		{
			foreach (IGeometry singlePart in LowLevelGeometries)
			{
				IGeometry highLevelPart =
					GeometryUtils.GetHighLevelGeometry(singlePart, true);

				bool intersects = GeometryUtils.Intersects(highLevelPart, geometry);

				// Attention: do not release point in multi-point geometry
				if (highLevelPart != singlePart)
				{
					Marshal.ReleaseComObject(highLevelPart);
				}

				if (intersects)
				{
					return true;
				}
			}

			return false;
		}

		public bool IsWithin([NotNull] IGeometry geometry,
		                     [NotNull] IGeometry highLevelGeometryTemplate)
		{
			// TOP-4881: Ring-by ring comparison leads to wrong results with polygon selection geometries
			//           when (exterior) multipatches are oriented in the wrong direction (e.g. a bit more 
			//           than vertical -> negative area) because Contains for a high-level polygon with an 
			//           inner ring returns false even when all vertices are within.
			// TOP-4986: It seems that IRelationalOperator.Contains is extremely unreliable when it comes 
			//           to (vertical) multipatches. Using polylines instead.
			IGeometry highLevelPart = CreateAsPolyline(highLevelGeometryTemplate);

			bool within = GeometryUtils.Contains(geometry, highLevelPart);

			return within;
		}

		public bool ContainsLowLevelGeometry([NotNull] IGeometry geometry)
		{
			return LowLevelGeometries.Contains(geometry);
		}

		public void MergeWithGeometry([NotNull] IGeometry highLevelGeometry)
		{
			AddGeometries(LowLevelGeometries, highLevelGeometry);
		}

		public void JoinPart([NotNull] GeometryPart other)
		{
			LowLevelGeometries.AddRange(other.LowLevelGeometries);
			InnerRings.AddRange(other.InnerRings);
		}

		[NotNull]
		public IGeometry CreateAsHighLevelGeometry([NotNull] IGeometry geometrySchema)
		{
			IGeometry result = GeometryFactory.CreateEmptyGeometry(geometrySchema);

			AddGeometries(LowLevelGeometries, result);

			return result;
		}

		[NotNull]
		public IGeometry CreateAsHighLevelGeometry(esriGeometryType resultGeometyType)
		{
			IGeometry result = GeometryFactory.CreateEmptyGeometry(resultGeometyType);

			// Avoid using an un-aware container geometry. Otherwise the added parts lose their awareness
			// and Z/M-values as well.
			if (FirstGeometry is IZAware zAware)
			{
				((IZAware) result).ZAware = zAware.ZAware;
			}

			if (FirstGeometry is IMAware mAware)
			{
				((IMAware) result).MAware = mAware.MAware;
			}

			AddGeometries(LowLevelGeometries, result);

			return result;
		}

		private IGeometry CreateAsPolyline(IGeometry highLevelGeometryTemplate)
		{
			IGeometry result =
				GeometryFactory.CreateEmptyPolyline(highLevelGeometryTemplate);

			// ReSharper disable once RedundantEnumerableCastCall
			AddGeometries(LowLevelGeometries.Select(GetPath)
			                                .Cast<IGeometry>(),
			              result);

			return result;
		}

		[NotNull]
		private static IPath GetPath([NotNull] IGeometry lowLevelGeometry)
		{
			if (lowLevelGeometry.GeometryType == esriGeometryType.esriGeometryPath)
			{
				return (IPath) lowLevelGeometry;
			}

			if (lowLevelGeometry.GeometryType == esriGeometryType.esriGeometryRing)
			{
				return GeometryFactory.CreatePath((IRing) lowLevelGeometry);
			}

			throw new InvalidOperationException(
				$"Unsupported input geometry type: {lowLevelGeometry.GeometryType}");
		}

		private void AddGeometries([NotNull] IEnumerable<IGeometry> lowLevelGeometries,
		                           [NotNull] IGeometry toHighLevelGeometry)
		{
			var geometryCollection = (IGeometryCollection) toHighLevelGeometry;

			var multiPatch = toHighLevelGeometry as IMultiPatch;

			object missing = Type.Missing;

			foreach (IGeometry geometry in lowLevelGeometries)
			{
				if (toHighLevelGeometry.SpatialReference == null)
				{
					toHighLevelGeometry.SpatialReference = geometry.SpatialReference;
				}

				geometryCollection.AddGeometry(geometry, ref missing, ref missing);

				var ring = geometry as IRing;

				if (multiPatch != null && ring != null)
				{
					if (InnerRings.Contains(geometry))
					{
						multiPatch.PutRingType(
							ring, esriMultiPatchRingType.esriMultiPatchInnerRing);
					}
					else
					{
						// TODO: do we need to maintain the original ring type -> test with FirstRing!
						multiPatch.PutRingType(
							ring, esriMultiPatchRingType.esriMultiPatchOuterRing);
					}
				}
			}
		}

		private static double GetSize([NotNull] IEnumerable<IGeometry> lowLevelGeometries)
		{
			double result = 0;

			foreach (IGeometry lowLevelGeometry in lowLevelGeometries)
			{
				var area = lowLevelGeometry as IArea;
				var point = lowLevelGeometry as IPoint;

				double size;
				if (area != null)
				{
					// inner / exterior ring is mostly incorrect (for multipatch rings)
					size = Math.Abs(area.Area);
				}
				else if (point != null)
				{
					size = 1;
				}
				else
				{
					var curve = lowLevelGeometry as ICurve;

					size = curve?.Length ??
					       ((IPointCollection) lowLevelGeometry).PointCount;
				}

				result += size;
			}

			return result;
		}

		private static esriGeometryType GetHighLevelGeometryType(
			[NotNull] IGeometry lowLevelGeometry)
		{
			switch (lowLevelGeometry.GeometryType)
			{
				case esriGeometryType.esriGeometryRing:
					return esriGeometryType.esriGeometryPolygon;
				case esriGeometryType.esriGeometryPath:
					return esriGeometryType.esriGeometryPolyline;
				case esriGeometryType.esriGeometryPoint:
					return esriGeometryType.esriGeometryMultipoint;
				default:
					return esriGeometryType.esriGeometryNull;
			}
		}

		public static IEnumerable<GeometryPart> FromGeometry(
			IGeometry originalGeometry, bool groupPartsByPointIDs = false)
		{
			// each outer ring builds a low level part with all its inner ring
			ICollection<GeometryPart> lowLevelParts = CollectionUtils.GetCollection(
				GetGeometryPartsPerLowLevelGeometry(originalGeometry));

			if (! groupPartsByPointIDs ||
			    ! GeometryUtils.IsPointIDAware(originalGeometry))
			{
				return lowLevelParts;
			}

			var partsById = new Dictionary<int, GeometryPart>();

			foreach (GeometryPart lowLevelPart in lowLevelParts)
			{
				IGeometry mainLowLevelGeometry = lowLevelPart.FirstGeometry;

				int vertexId;

				if (GeometryUtils.HasUniqueVertexId(mainLowLevelGeometry, out vertexId))
				{
					GeometryPart existingPart;

					if (partsById.TryGetValue(vertexId, out existingPart))
					{
						existingPart.JoinPart(lowLevelPart);
					}
					else
					{
						// It is important not to use a direct reference of the lowLevelPart, otherwise it will be changed
						// by merging other lowLevelParts into it. Create a new, aggregate part for each vertex id instead.
						var aggregate = new GeometryPart(lowLevelPart);
						aggregate.LabelText = Convert.ToString(vertexId);

						partsById.Add(vertexId, aggregate);
					}
				}
				else
				{
					// all parts need proper vertex ids!
					return lowLevelParts;
				}
			}

			return partsById.Values;
		}

		private static IEnumerable<GeometryPart> GetGeometryPartsPerLowLevelGeometry(
			[NotNull] IGeometry originalGeometry)
		{
			// get exterior rings with all their interior rings
			// alternative (add options?) treat inner rings the same, allow extracting an inner ring as well?

			var polygon = originalGeometry as IPolygon4;

			if (polygon != null)
			{
				return GetPolygonExteriorRingParts(polygon);
			}

			var multipatch = originalGeometry as IMultiPatch2;

			if (multipatch != null)
			{
				return GetMultipatchExteriorRingParts(multipatch);
			}

			return GeometryUtils.GetParts((IGeometryCollection) originalGeometry).Select(
				part => new GeometryPart(part));
		}

		/// <summary>
		/// Identifies each exterior ring as separate part, containing all its interior rings
		/// </summary>
		/// <param name="polygon"></param>
		/// <returns></returns>
		private static IEnumerable<GeometryPart> GetPolygonExteriorRingParts(
			[NotNull] IPolygon4 polygon)
		{
			// for all interior rings, assign to exterior ring's part
			// all exterior rings, add them to result if not yet added
			var partsByExteriorRing = new Dictionary<IRing, GeometryPart>();

			var geometryCollection = (IGeometryCollection) polygon;
			for (var i = 0; i < geometryCollection.GeometryCount; i++)
			{
				var ring = (IRing) geometryCollection.Geometry[i];

				if (ring.IsExterior)
				{
					partsByExteriorRing.Add(ring, new GeometryPart(ring));
				}
				else
				{
					IRing exteriorRing = polygon.FindExteriorRing(ring);

					Assert.NotNull("No exterior ring found for inner ring");

					GeometryPart part;
					if (! partsByExteriorRing.TryGetValue(exteriorRing, out part))
					{
						part = new GeometryPart(exteriorRing);
						partsByExteriorRing.Add(exteriorRing, part);
					}

					part.AddInnerRingGeometry(ring);
				}
			}

			return partsByExteriorRing.Values;
		}

		private static IList<GeometryPart> GetMultipatchExteriorRingParts(
			[NotNull] IMultiPatch2 multipatch)
		{
			var geometryCollection = (IGeometryCollection) multipatch;

			var partsByExteriorRing = new Dictionary<IRing, GeometryPart>();
			for (var i = 0; i < geometryCollection.GeometryCount; i++)
			{
				var ring = geometryCollection.Geometry[i] as IRing;

				if (ring == null)
				{
					continue;
				}

				// for multipatches we cannot use IsExterior property - it's just not correct
				var isBeginningRing = false;
				bool isExterior = multipatch.GetRingType(ring, ref isBeginningRing) !=
				                  esriMultiPatchRingType.esriMultiPatchInnerRing;

				if (isExterior)
				{
					var newExteriorPart = new GeometryPart(ring)
					                      {
						                      LabelText = Convert.ToString(i)
					                      };

					partsByExteriorRing.Add(ring, newExteriorPart);
				}
				else
				{
					IRing exteriorRing = multipatch.FindBeginningRing(ring);

					Assert.NotNull(exteriorRing, "No exterior ring found for inner ring");

					GeometryPart part;
					if (! partsByExteriorRing.TryGetValue(exteriorRing, out part))
					{
						part = new GeometryPart(exteriorRing);
						partsByExteriorRing.Add(exteriorRing, part);
					}

					part.AddInnerRingGeometry(ring);
				}
			}

			return partsByExteriorRing.Values.ToList();
		}
	}
}
