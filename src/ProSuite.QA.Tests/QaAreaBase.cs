using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Base class for tests that check the area of polygons.
	/// </summary>
	public abstract class QaAreaBase : ContainerTest
	{
		private readonly bool _perPart;
		private readonly int _areaFieldIndex;
		private readonly bool _useField;
		private readonly string _shapeFieldName;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="QaAreaBase"/> class.
		/// </summary>
		/// <param name="featureClass">The featureClass.</param>
		/// <param name="limit">The limit.</param>
		/// <param name="perPart">if set to <c>true</c> inidividual parts are tested.</param>
		protected QaAreaBase([NotNull] IReadOnlyFeatureClass featureClass,
		                     double limit, bool perPart) : base(featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			Limit = limit;
			_perPart = perPart;
			_shapeFieldName = featureClass.ShapeFieldName;

			NumberFormat = "N0";

			IField areaField = featureClass.AreaField;

			_areaFieldIndex = areaField == null
				                  ? -1
				                  : featureClass.FindField(areaField.Name);
			_useField = _areaFieldIndex >= 0;
		}

		#endregion

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
			var feature = (IReadOnlyFeature) row;

			IGeometry shape = feature.Shape;

			var polygonArea = shape as IArea;
			if (polygonArea == null)
			{
				return 0;
			}

			if (shape is IMultiPatch multipatch)
			{
				return ExecuteMultipatch(row, multipatch, _perPart);
			}

			var polygon = shape as IPolygon;
			if (polygon == null)
			{
				return ExecutePart(row, shape);
			}

			PolygonPartType partType = _perPart
				                           ? PolygonPartType.ExteriorRing | PolygonPartType.Ring
				                           : PolygonPartType.Full;

			if (partType == PolygonPartType.Full || GeometryUtils.GetPartCount(polygon) == 1)
			{
				if (_useField)
				{
					double? areaValue = GdbObjectUtils.ReadRowValue<double>(row, _areaFieldIndex);

					if (areaValue != null)
					{
						return CheckArea(Math.Abs(areaValue.Value), shape, row);
					}
				}

				return CheckArea(Math.Abs(polygonArea.Area), shape, row);
			}

			// need to check the individual rings

			int errorCount = 0;

			foreach (IGeometry part in TestUtils.GetParts(polygon, partType))
			{
				errorCount += ExecutePart(row, part);

				if (part != polygon)
				{
					// the part is some sub-component of the polygon, either 
					// a ring or a connected-component polygon
					// -> release it to avoid pushing the VM allocation up
					Marshal.ReleaseComObject(part);
				}
			}

			return errorCount;
		}

		protected double Limit { get; }

		protected int ReportError([NotNull] IGeometry shape,
		                          double area,
		                          [NotNull] string relation,
		                          [CanBeNull] IssueCode issueCode,
		                          [NotNull] IReadOnlyRow row)
		{
			string description = string.Format("Area {0}",
			                                   FormatAreaComparison(area, relation, Limit,
				                                   shape.SpatialReference));

			return ReportError(description, InvolvedRowUtils.GetInvolvedRows(row),
			                   GetErrorGeometry(shape),
			                   issueCode, _shapeFieldName);
		}

		protected abstract int CheckArea(double area,
		                                 [NotNull] IGeometry shape,
		                                 [NotNull] IReadOnlyRow row);

		[NotNull]
		private static IGeometry GetErrorGeometry([NotNull] IGeometry shape)
		{
			// TODO when to copy the error geometry? Is this guaranteed to happen when 
			// the error event is consumed?

			var ring = shape as IRing;
			if (ring == null)
			{
				return GeometryFactory.Clone(shape);
			}

			IPolygon result = GeometryFactory.CreatePolygon(ring);

			if (! ring.IsExterior)
			{
				result.ReverseOrientation();
			}

			return result;
		}

		private int ExecutePart(IReadOnlyRow row, IGeometry shape)
		{
			double area = ((IArea) shape).Area;
			double absoluteArea = Math.Abs(area);

			return CheckArea(absoluteArea, shape, row);
		}

		private int ExecuteMultipatch([NotNull] IReadOnlyRow row,
		                              [NotNull] IMultiPatch multipatch,
		                              bool perPart)
		{
			// TODO: Check what happens with narrow triangles
			if (! GeometryUtils.IsRingBasedMultipatch(multipatch))
			{
				return ExecutePart(row, multipatch);
			}

			// TOP-5659:
			// Multipatches use the footprint's area which is not correct for
			// small or narrow multipatches -> the footprint is the envelope!
			var geometryCollection = (IGeometryCollection) multipatch;

			int errorCount = 0;
			double totalArea = 0;
			foreach (IRing ring in GeometryUtils.GetRings(multipatch))
			{
				if (ring == null)
				{
					continue;
				}

				// for multipatches we cannot use IsExterior property - it's just not correct
				var isBeginningRing = false;
				bool isExterior = multipatch.GetRingType(ring, ref isBeginningRing) !=
				                  esriMultiPatchRingType.esriMultiPatchInnerRing;

				List<Pnt3D> pntList = GeometryConversionUtils.GetPntList(ring);

				if (isExterior)
				{
					double ringArea = Math.Abs(GeomUtils.GetArea2D(pntList));

					if (perPart)
					{
						errorCount += CheckArea(ringArea, ring, row);
					}
					else
					{
						totalArea += ringArea;
					}
				}
				else
				{
					double innerRingArea = Math.Abs(GeomUtils.GetArea2D(pntList));

					totalArea -= innerRingArea;
				}
			}

			if (! perPart)
			{
				errorCount += CheckArea(totalArea, multipatch, row);
			}

			return errorCount;
		}
	}
}
