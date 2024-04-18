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
	public class QaWithinBox : ContainerTest
	{
		private readonly double _xMin;
		private readonly double _yMin;
		private readonly double _xMax;
		private readonly double _yMax;
		private readonly bool _reportOnlyOutsideParts;
		private readonly IEnvelope _featureExtent = new EnvelopeClass();
		private IRelationalOperator _boxRelOp;
		private IPolygon _boxPolygon;
		private readonly string _shapeFieldName;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string GeometryNotWithinBox = "GeometryNotWithinBox";

			public Code() : base("WithinBox") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaWithinBox_0))]
		public QaWithinBox(
				[Doc(nameof(DocStrings.QaWithinBox_featureClass))] [NotNull]
				IReadOnlyFeatureClass featureClass,
				[Doc(nameof(DocStrings.QaWithinBox_xMin))]
				double xMin,
				[Doc(nameof(DocStrings.QaWithinBox_yMin))]
				double yMin,
				[Doc(nameof(DocStrings.QaWithinBox_xMax))]
				double xMax,
				[Doc(nameof(DocStrings.QaWithinBox_yMax))]
				double yMax)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, xMin, yMin, xMax, yMax, false) { }

		[Doc(nameof(DocStrings.QaWithinBox_0))]
		public QaWithinBox(
			[Doc(nameof(DocStrings.QaWithinBox_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaWithinBox_xMin))]
			double xMin,
			[Doc(nameof(DocStrings.QaWithinBox_yMin))]
			double yMin,
			[Doc(nameof(DocStrings.QaWithinBox_xMax))]
			double xMax,
			[Doc(nameof(DocStrings.QaWithinBox_yMax))]
			double yMax,
			[Doc(nameof(DocStrings.QaWithinBox_reportOnlyOutsideParts))]
			bool reportOnlyOutsideParts)
			: base(featureClass)
		{
			Assert.ArgumentNotNaN(xMin, nameof(xMin));
			Assert.ArgumentNotNaN(yMin, nameof(yMin));
			Assert.ArgumentNotNaN(xMax, nameof(xMax));
			Assert.ArgumentNotNaN(yMax, nameof(yMax));
			Assert.ArgumentCondition(xMin < xMax, "xMin must be smaller than xMax");
			Assert.ArgumentCondition(yMin < yMax, "yMin must be smaller than yMax");

			_xMin = xMin;
			_yMin = yMin;
			_xMax = xMax;
			_yMax = yMax;
			_reportOnlyOutsideParts = reportOnlyOutsideParts;
			_shapeFieldName = featureClass.ShapeFieldName;
		}

		[InternallyUsedTest]
		public QaWithinBox([NotNull] QaWithinBoxDefinition definition)
			: this((IReadOnlyFeatureClass)definition.FeatureClass, definition.XMin,
			       definition.YMin, definition.XMax, definition.YMax, definition.ReportOnlyOutsideParts)
		{ }

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

			feature.Shape.QueryEnvelope(_featureExtent);
			if (_featureExtent.IsEmpty)
			{
				return NoError;
			}

			if (_boxRelOp == null)
			{
				ISpatialReference spatialReference = GetBoxSpatialReference(
					_featureExtent.SpatialReference,
					_xMin, _yMin,
					_xMax, _yMax);

				IEnvelope box = GeometryFactory.CreateEnvelope(
					_xMin, _yMin, _xMax, _yMax,
					spatialReference);

				_boxRelOp = (IRelationalOperator) box;
			}

			if (_boxRelOp.Contains(_featureExtent))
			{
				return NoError;
			}

			IGeometry errorGeometry = GetErrorGeometry(feature, (IEnvelope) _boxRelOp);
			if (errorGeometry.IsEmpty)
			{
				// tolerance issue?
				return NoError;
			}

			const string description = "Geometry is not within expected extent";
			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(feature), errorGeometry,
				Codes[Code.GeometryNotWithinBox], _shapeFieldName);
		}

		[NotNull]
		private static ISpatialReference GetBoxSpatialReference(
			[NotNull] ISpatialReference sref,
			double xMin, double yMin,
			double xMax, double yMax)
		{
			double domainXMin;
			double domainYMin;
			double domainXMax;
			double domainYMax;
			sref.GetDomain(out domainXMin, out domainXMax,
			               out domainYMin, out domainYMax);
			double xyResolution = SpatialReferenceUtils.GetXyResolution(sref);

			var outOfBounds = false;
			double epsilon = xyResolution * 10;
			if (xMin < domainXMin + epsilon)
			{
				domainXMin = xMin - epsilon;
				outOfBounds = true;
			}

			if (yMin < domainYMin + epsilon)
			{
				domainYMin = yMin - epsilon;
				outOfBounds = true;
			}

			if (xMax > domainXMax - epsilon)
			{
				domainXMax = xMax + epsilon;
				outOfBounds = true;
			}

			if (yMax > domainYMax - epsilon)
			{
				domainYMax = yMax + epsilon;
				outOfBounds = true;
			}

			if (! outOfBounds)
			{
				return sref;
			}

			var copy = (ISpatialReference) ((IClone) sref).Clone();

			// TODO round to resolution --> no odd grid origins
			copy.SetDomain(domainXMin, domainXMax, domainYMin, domainYMax);
			return copy;
		}

		[NotNull]
		private IGeometry GetErrorGeometry([NotNull] IReadOnlyFeature feature,
		                                   [NotNull] IEnvelope box)
		{
			if (! _reportOnlyOutsideParts)
			{
				// always report full feature
				return feature.ShapeCopy;
			}

			IGeometry shape = feature.Shape;

			var topoOp = shape as ITopologicalOperator;

			if (topoOp == null)
			{
				// shape does not implement ITopologicalOperator, use entire shape
				return feature.ShapeCopy;
			}

			// box will be needed as polygon, create it lazily
			if (_boxPolygon == null)
			{
				_boxPolygon = GeometryFactory.CreatePolygon(box);
			}

			var relOp = (IRelationalOperator) shape;
			if (relOp.Disjoint(_boxPolygon))
			{
				// fully outside box
				return feature.ShapeCopy;
			}

			IGeometry difference = topoOp.Difference(_boxPolygon);

			if (! difference.IsEmpty && GeometryUtils.IsZAware(shape))
			{
				GeometryUtils.MakeZAware(difference);

				((IZ) difference).CalculateNonSimpleZs();
			}

			return difference;
		}
	}
}
