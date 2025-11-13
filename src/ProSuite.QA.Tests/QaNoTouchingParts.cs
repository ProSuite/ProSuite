using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[GeometryTest]
	public class QaNoTouchingParts : ContainerTest
	{
		private readonly double _tolerance;
		private readonly string _shapeFieldName;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string PartsTouch = "PartsTouch";

			public Code() : base("TouchingParts") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaNoTouchingParts_0))]
		public QaNoTouchingParts(
			[Doc(nameof(DocStrings.QaNoTouchingParts_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass)
			: base(featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			_tolerance = SpatialReferenceUtils.GetXyResolution(featureClass.SpatialReference);
			_shapeFieldName = featureClass.ShapeFieldName;
		}

		[InternallyUsedTest]
		public QaNoTouchingParts([NotNull] QaNoTouchingPartsDefinition definition)
			: this((IReadOnlyFeatureClass) definition.FeatureClass) { }

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

			IGeometry shape = feature.Shape;

			if (shape.GeometryType != esriGeometryType.esriGeometryPolygon &&
			    shape.GeometryType != esriGeometryType.esriGeometryPolyline)
			{
				return NoError;
			}

			var geometryCollection = (IGeometryCollection) shape;
			if (geometryCollection.GeometryCount < 2)
			{
				return NoError;
			}

			int partCount = geometryCollection.GeometryCount;

			var locationsOnPreviousParts =
				new Dictionary<Location, short>(new LocationComparer(_tolerance));

			int errorCount = 0;

			int partIndex = 0;
			foreach (IGeometry part in OrderByAscendingPointCount(
				         GeometryUtils.GetParts(geometryCollection)))
			{
				bool isLastPart = partIndex == partCount - 1;

				errorCount += CheckPart(part, isLastPart, locationsOnPreviousParts, row);
				partIndex++;

				Marshal.ReleaseComObject(part);
			}

			return errorCount;
		}

		private int CheckPart(
			[NotNull] IGeometry part,
			bool isLastPart,
			[NotNull] IDictionary<Location, short> locationsOnPreviousParts,
			[NotNull] IReadOnlyRow row)
		{
			Assert.ArgumentNotNull(part, nameof(part));
			Assert.ArgumentNotNull(locationsOnPreviousParts, nameof(locationsOnPreviousParts));
			Assert.ArgumentNotNull(row, nameof(row));

			int errorCount = 0;

			var partLocations = new List<Location>(GetLocations(part));

			if (locationsOnPreviousParts.Count > 0)
			{
				int pointIndex = 0;
				foreach (Location location in partLocations)
				{
					if (locationsOnPreviousParts.ContainsKey(location))
					{
						errorCount += ReportError(row, part, pointIndex);
					}

					pointIndex++;
				}
			}

			if (! isLastPart)
			{
				// only if not the last, largest ring: add to previous ring locations
				foreach (Location partLocation in partLocations)
				{
					locationsOnPreviousParts[partLocation] = 0;
				}
			}

			return errorCount;
		}

		private int ReportError([NotNull] IReadOnlyRow row, [NotNull] IGeometry part,
		                        int pointIndex)
		{
			IPoint point = ((IPointCollection) part).get_Point(pointIndex);

			return ReportError(
				"Parts touch", InvolvedRowUtils.GetInvolvedRows(row), point,
				Codes[Code.PartsTouch], _shapeFieldName);
		}

		[NotNull]
		private static IEnumerable<IGeometry> OrderByAscendingPointCount(
			[NotNull] IEnumerable<IGeometry> parts)
		{
			var sortedParts = new List<PartHolder>();

			foreach (IGeometry part in parts)
			{
				sortedParts.Add(new PartHolder(part));
			}

			sortedParts.Sort(CompareByPointCountAscending);

			return sortedParts.Select(partHolder => partHolder.Part);
		}

		private static int CompareByPointCountAscending([NotNull] PartHolder r1,
		                                                [NotNull] PartHolder r2)
		{
			// use wrapper to avoid expensive interop in compare method
			return r1.PointCount.CompareTo(r2.PointCount);
		}

		[NotNull]
		private static IEnumerable<Location> GetLocations([NotNull] IGeometry geometry)
		{
			WKSPoint[] pointArray = GeometryUtils.GetWKSPoints(geometry);

			return pointArray.Select(point => new Location(point.X, point.Y));
		}

		private sealed class PartHolder
		{
			private readonly IGeometry _part;
			public readonly int PointCount;

			public PartHolder([NotNull] IGeometry part)
			{
				_part = part;
				var points = part as IPointCollection;
				PointCount = points?.PointCount ?? 0;
			}

			[NotNull]
			public IGeometry Part => _part;
		}
	}
}
