using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry.ExtractParts
{
	public static class PartExtractionUtils
	{
		public static IEnumerable<GeometryPart> GetGeometryParts(
			[NotNull] IGeometry originalGeometry, bool groupPartsByPointIDs)
		{
			return GeometryPart.FromGeometry(originalGeometry, groupPartsByPointIDs);
		}

		/// <summary>
		/// Explodes the input geometry into part groups which are defined as
		/// - connected components, i.e. outer rings with inner rings if <paramref name="groupByPointIds"/> is false
		/// - parts grouped by equal point id if <paramref name="groupByPointIds"/> is true.
		/// </summary>
		/// <param name="inputGeometry"></param>
		/// <param name="groupByPointIds">Whether parts are grouped by point id for point-id aware geometries.</param>
		/// <param name="resultOrderedBySizeDesc">Resulting high-level geometries ordered by size descending.</param>
		/// <param name="notification">Reason why the explode could not be performed.</param>
		/// <returns></returns>
		public static bool TryExplode([NotNull] IGeometry inputGeometry,
		                              bool groupByPointIds,
		                              out List<IGeometry> resultOrderedBySizeDesc,
		                              out string notification)
		{
			resultOrderedBySizeDesc = null;
			notification = null;

			var multipartGeometry =
				inputGeometry as IGeometryCollection;

			if (multipartGeometry == null)
			{
				notification =
					$"The geometry type {inputGeometry.GeometryType} does not allow multi-part geometries.";
				return false;
			}

			if (multipartGeometry.GeometryCount < 2)
			{
				notification = "The geometry is already single-part";
				return false;
			}

			List<GeometryPart> sortedParts =
				GetGeometryParts(inputGeometry, groupByPointIds)
					.OrderByDescending(p => p.Size)
					.ToList();

			if (sortedParts.Count < 2)
			{
				notification = "The geometry contains of only one connected component.";
				return false;
			}

			if (groupByPointIds && ! GeometryUtils.IsPointIDAware(inputGeometry))
			{
				notification = "The geometry is not point-id aware.";
				return false;
			}

			resultOrderedBySizeDesc =
				sortedParts.Select(part => part.CreateAsHighLevelGeometry(inputGeometry))
				           .ToList();

			foreach (IGeometry geometry in resultOrderedBySizeDesc)
			{
				CompressVertexIds(geometry);
			}

			return true;
		}

		public static void CompressVertexIDs(IGeometry extractedGeometry,
		                                     IGeometry adaptedOriginal)
		{
			// This might not be necessary - done for consistency with tefa

			CompressVertexIds(extractedGeometry);

			CompressVertexIds(adaptedOriginal);
		}

		public static void CompressVertexIDs(IEnumerable<IGeometry> extractedGeometries,
		                                     IGeometry adaptedOriginal)
		{
			CompressVertexIds(adaptedOriginal);

			foreach (IGeometry geometry in extractedGeometries)
			{
				CompressVertexIds(geometry);
			}
		}

		/// <summary>
		/// Removes gaps in the vertex id values.
		/// </summary>
		/// <param name="geometry"></param>
		private static void CompressVertexIds(IGeometry geometry)
		{
			var geometryCollection = (IGeometryCollection) geometry;

			int processingId = -1;
			int currentId = -1;
			for (var i = 0; i < geometryCollection.GeometryCount; i++)
			{
				IGeometry part = geometryCollection.Geometry[i];
				int vertexId;
				if (GeometryUtils.HasUniqueVertexId(part, out vertexId))
				{
					// NOTE: part id values are not necessarily ordered along geometry indexes
					// This assumption is not only violated when holes are created in the non-0 part, but 
					// also occurs when extracting several parts from an existing geometry.
					esriMultiPatchRingType ringType =
						GetMultipatchRingType(geometry, part);

					if (vertexId == 0 &&
					    ringType == esriMultiPatchRingType.esriMultiPatchInnerRing)
					{
						// The TEFA create hole tool always assigns 0 to the holes - leave them alone.
						continue;
					}

					if (processingId != vertexId)
					{
						// different part -> increment current id
						currentId++;
						processingId = vertexId;
					}

					if (vertexId != currentId)
					{
						GeometryUtils.AssignConstantPointID(
							(IGeometryCollection) geometry, i, currentId);
					}
				}
			}
		}

		private static esriMultiPatchRingType GetMultipatchRingType(
			[NotNull] IGeometry entireGeometry,
			[NotNull] IGeometry part)
		{
			var multipatchGeometry = entireGeometry as IMultiPatch;
			var currentRing = part as IRing;

			var ringType = esriMultiPatchRingType.esriMultiPatchUndefinedRing;
			if (multipatchGeometry != null && currentRing != null)
			{
				var isBeginningRing = false;
				ringType = multipatchGeometry.GetRingType(
					currentRing, ref isBeginningRing);
			}

			return ringType;
		}
	}
}
