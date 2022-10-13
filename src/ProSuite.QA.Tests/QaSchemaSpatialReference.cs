using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Properties;
using ProSuite.QA.Tests.Schema;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[SchemaTest]
	public class QaSchemaSpatialReference : QaSchemaTestBase
	{
		private readonly IReadOnlyFeatureClass _featureClass;
		private readonly ISpatialReference _expectedSpatialReference;
		private readonly bool _compareXyPrecision;
		private readonly bool _compareZPrecision;
		private readonly bool _compareMPrecision;
		private readonly bool _compareXyTolerance;
		private readonly bool _compareZTolerance;
		private readonly bool _compareMTolerance;
		private readonly bool _compareVerticalCoordinateSystems;
		private readonly string _shapeFieldName;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string PrecisionDifferent_XY = "PrecisionDifferent.XY";
			public const string PrecisionDifferent_Z = "PrecisionDifferent.Z";
			public const string PrecisionDifferent_M = "PrecisionDifferent.M";
			public const string ToleranceDifferent_XY = "ToleranceDifferent.XY";
			public const string ToleranceDifferent_Z = "ToleranceDifferent.Z";
			public const string ToleranceDifferent_M = "ToleranceDifferent.M";
			public const string CoordinateSystemDifferent_XY = "CoordinateSystemDifferent.XY";
			public const string CoordinateSystemDifferent_Z = "CoordinateSystemDifferent.Z";

			public Code() : base("SpatialReference") { }
		}

		#endregion

		#region Constructors

		[Doc(nameof(DocStrings.QaSchemaSpatialReference_0))]
		public QaSchemaSpatialReference(
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_referenceFeatureClass))] [NotNull]
			IReadOnlyFeatureClass referenceFeatureClass,
			bool compareXYPrecision, bool compareZPrecision,
			bool compareMPrecision, bool compareTolerances,
			bool compareVerticalCoordinateSystems)
			: this(featureClass, GetSpatialReference(referenceFeatureClass),
			       compareXYPrecision, compareZPrecision, compareMPrecision,
			       compareTolerances, compareTolerances, compareTolerances,
			       compareVerticalCoordinateSystems)
		{
			AddMissingFeatureClass(referenceFeatureClass);
		}

		[Doc(nameof(DocStrings.QaSchemaSpatialReference_1))]
		public QaSchemaSpatialReference(
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_spatialReferenceXml))] [NotNull]
			string spatialReferenceXml,
			bool compareXYPrecision, bool compareZPrecision,
			bool compareMPrecision, bool compareTolerances,
			bool compareVerticalCoordinateSystems)
			: this(featureClass, SpatialReferenceUtils.FromXmlString(spatialReferenceXml),
			       compareXYPrecision, compareZPrecision, compareMPrecision,
			       compareTolerances, compareTolerances, compareTolerances,
			       compareVerticalCoordinateSystems) { }

		[Doc(nameof(DocStrings.QaSchemaSpatialReference_2))]
		public QaSchemaSpatialReference(
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_referenceFeatureClass))] [NotNull]
			IReadOnlyFeatureClass referenceFeatureClass,
			bool compareUsedPrecisions, bool compareTolerances,
			bool compareVerticalCoordinateSystems)
			: this(featureClass, GetSpatialReference(referenceFeatureClass),
			       compareUsedPrecisions,
			       compareUsedPrecisions && DatasetUtils.GetGeometryDef(featureClass).HasZ,
			       compareUsedPrecisions && DatasetUtils.GetGeometryDef(featureClass).HasM,
			       compareTolerances,
			       compareTolerances && DatasetUtils.GetGeometryDef(featureClass).HasZ,
			       compareTolerances && DatasetUtils.GetGeometryDef(featureClass).HasM,
			       compareVerticalCoordinateSystems)
		{
			AddMissingFeatureClass(referenceFeatureClass);
		}

		[Doc(nameof(DocStrings.QaSchemaSpatialReference_3))]
		public QaSchemaSpatialReference(
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_spatialReferenceXml))] [NotNull]
			string spatialReferenceXml,
			bool compareUsedPrecisions, bool compareTolerances,
			bool compareVerticalCoordinateSystems)
			: this(featureClass,
			       SpatialReferenceUtils.FromXmlString(spatialReferenceXml),
			       compareUsedPrecisions,
			       compareUsedPrecisions && DatasetUtils.GetGeometryDef(featureClass).HasZ,
			       compareUsedPrecisions && DatasetUtils.GetGeometryDef(featureClass).HasM,
			       compareTolerances,
			       compareTolerances && DatasetUtils.GetGeometryDef(featureClass).HasZ,
			       compareTolerances && DatasetUtils.GetGeometryDef(featureClass).HasM,
			       compareVerticalCoordinateSystems) { }

		/// <summary>
		/// Prevents a default instance of the <see cref="QaSchemaSpatialReference"/> class from being created.
		/// </summary>
		private QaSchemaSpatialReference(
			[NotNull] IReadOnlyFeatureClass featureClass,
			[NotNull] ISpatialReference expectedSpatialReference,
			bool compareXyPrecision, bool compareZPrecision, bool compareMPrecision,
			bool compareXyTolerance, bool compareZTolerance, bool compareMTolerance,
			bool compareVerticalCoordinateSystems) : base(featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));
			Assert.ArgumentNotNull(expectedSpatialReference, nameof(expectedSpatialReference));

			_featureClass = featureClass;
			_expectedSpatialReference = expectedSpatialReference;
			_compareXyPrecision = compareXyPrecision;
			_compareZPrecision = compareZPrecision;
			_compareMPrecision = compareMPrecision;
			_compareXyTolerance = compareXyTolerance;
			_compareZTolerance = compareZTolerance;
			_compareMTolerance = compareMTolerance;
			_compareVerticalCoordinateSystems = compareVerticalCoordinateSystems;
			_shapeFieldName = featureClass.ShapeFieldName;
		}

		/// <summary>
		/// Prevents an exception saying that the number of constraints is not equal the number of involved tables.
		/// </summary>
		/// <param name="referenceFeatureClass"></param>
		private void AddMissingFeatureClass(IReadOnlyFeatureClass referenceFeatureClass)
		{
			AddInvolvedTable(referenceFeatureClass, null, false);
		}

		#endregion

		#region Overrides of QaSchemaTestBase

		public override int Execute()
		{
			ISpatialReference actualSpatialReference = GetSpatialReference(_featureClass);

			//const bool comparePrecisionAndTolerance = true;
			//const bool compareVerticalCoordinateSystems = true;

			bool coordinateSystemDifferent;
			bool vcsDifferent;
			bool xyPrecisionDifferent;
			bool zPrecisionDifferent;
			bool mPrecisionDifferent;
			bool xyToleranceDifferent;
			bool zToleranceDifferent;
			bool mToleranceDifferent;
			bool equal = SpatialReferenceUtils.AreEqual(_expectedSpatialReference,
			                                            actualSpatialReference,
			                                            out coordinateSystemDifferent,
			                                            out vcsDifferent,
			                                            out xyPrecisionDifferent,
			                                            out zPrecisionDifferent,
			                                            out mPrecisionDifferent,
			                                            out xyToleranceDifferent,
			                                            out zToleranceDifferent,
			                                            out mToleranceDifferent);

			if (equal)
			{
				return NoError;
			}

			int errorCount = 0;

			if (coordinateSystemDifferent)
			{
				errorCount += Report(
					Codes[Code.CoordinateSystemDifferent_XY],
					LocalizableStrings.QaSchemaSpatialReference_CoordinateSystemDifferent,
					_expectedSpatialReference.Name, actualSpatialReference.Name);
			}

			if (_compareVerticalCoordinateSystems && vcsDifferent)
			{
				errorCount += Report(
					Codes[Code.CoordinateSystemDifferent_Z],
					LocalizableStrings.QaSchemaSpatialReference_VerticalCoordinateSystemDifferent,
					GetVCSDisplayName(_expectedSpatialReference),
					GetVCSDisplayName(actualSpatialReference));
			}

			if (_compareXyPrecision && xyPrecisionDifferent)
			{
				errorCount += Report(
					Codes[Code.PrecisionDifferent_XY],
					LocalizableStrings.QaSchemaSpatialReference_XYDomainOrPrecisionDifferent,
					GetXyPrecisionString(_expectedSpatialReference),
					GetXyPrecisionString(actualSpatialReference));
			}

			if (_compareZPrecision && zPrecisionDifferent)
			{
				errorCount += Report(
					Codes[Code.PrecisionDifferent_Z],
					LocalizableStrings.QaSchemaSpatialReference_ZDomainOrPrecisionDifferent,
					GetZPrecisionString(_expectedSpatialReference),
					GetZPrecisionString(actualSpatialReference));
			}

			if (_compareMPrecision && mPrecisionDifferent)
			{
				errorCount += Report(
					Codes[Code.PrecisionDifferent_M],
					LocalizableStrings.QaSchemaSpatialReference_MDomainOrPrecisionDifferent,
					GetMPrecisionString(_expectedSpatialReference),
					GetMPrecisionString(actualSpatialReference));
			}

			if (_compareXyTolerance && xyToleranceDifferent)
			{
				errorCount += Report(
					Codes[Code.ToleranceDifferent_XY],
					LocalizableStrings.QaSchemaSpatialReference_XYToleranceDifferent,
					GetXYToleranceString(_expectedSpatialReference),
					GetXYToleranceString(actualSpatialReference));
			}

			if (_compareZTolerance && zToleranceDifferent)
			{
				errorCount += Report(
					Codes[Code.ToleranceDifferent_Z],
					LocalizableStrings.QaSchemaSpatialReference_ZToleranceDifferent,
					GetZToleranceString(_expectedSpatialReference),
					GetZToleranceString(actualSpatialReference));
			}

			if (_compareMTolerance && mToleranceDifferent)
			{
				errorCount += Report(
					Codes[Code.ToleranceDifferent_M],
					LocalizableStrings.QaSchemaSpatialReference_MToleranceDifferent,
					GetMToleranceString(_expectedSpatialReference),
					GetMToleranceString(actualSpatialReference));
			}

			return errorCount;
		}

		[StringFormatMethod("format")]
		private int Report([CanBeNull] IssueCode issueCode,
		                   [NotNull] string format,
		                   params object[] args)
		{
			Assert.ArgumentNotNullOrEmpty(format, nameof(format));

			return ReportSchemaPropertyError(issueCode, _shapeFieldName, format, args);
		}

		[NotNull]
		private static string GetXYToleranceString(
			[NotNull] ISpatialReference spatialReference)
		{
			var tolerance = spatialReference as ISpatialReferenceTolerance;

			return tolerance == null
				       ? LocalizableStrings.QaSchemaSpatialReference_NotDefined
				       : string.Format("{0}", tolerance.XYTolerance);
		}

		[NotNull]
		private static string GetZToleranceString(
			[NotNull] ISpatialReference spatialReference)
		{
			var tolerance = spatialReference as ISpatialReferenceTolerance;

			return tolerance == null
				       ? LocalizableStrings.QaSchemaSpatialReference_NotDefined
				       : string.Format("{0}", tolerance.ZTolerance);
		}

		[NotNull]
		private static string GetMToleranceString(
			[NotNull] ISpatialReference spatialReference)
		{
			var tolerance = spatialReference as ISpatialReferenceTolerance;

			return tolerance == null
				       ? LocalizableStrings.QaSchemaSpatialReference_NotDefined
				       : string.Format("{0}", tolerance.MTolerance);
		}

		[NotNull]
		private static string GetMPrecisionString(
			[NotNull] ISpatialReference spatialReference)
		{
			if (! spatialReference.HasZPrecision())
			{
				return LocalizableStrings.QaSchemaSpatialReference_NotDefined;
			}

			double mmin;
			double mmax;
			spatialReference.GetMDomain(out mmin, out mmax);

			var resolution = (ISpatialReferenceResolution) spatialReference;
			double mResolution = resolution.MResolution;

			return string.Format(LocalizableStrings.QaSchemaSpatialReference_MPrecision,
			                     mmin, mmax, mResolution);
		}

		[NotNull]
		private static string GetZPrecisionString(
			[NotNull] ISpatialReference spatialReference)
		{
			if (! spatialReference.HasZPrecision())
			{
				return LocalizableStrings.QaSchemaSpatialReference_NotDefined;
			}

			double zmin;
			double zmax;
			spatialReference.GetZDomain(out zmin, out zmax);

			var resolution = (ISpatialReferenceResolution) spatialReference;
			double zResolution = resolution.get_ZResolution(true);

			return string.Format(LocalizableStrings.QaSchemaSpatialReference_ZPrecision,
			                     zmin, zmax, zResolution);
		}

		[NotNull]
		private static string GetXyPrecisionString(
			[NotNull] ISpatialReference spatialReference)
		{
			if (! spatialReference.HasXYPrecision())
			{
				return LocalizableStrings.QaSchemaSpatialReference_NotDefined;
			}

			double xmin;
			double xmax;
			double ymin;
			double ymax;
			spatialReference.GetDomain(out xmin, out xmax, out ymin, out ymax);

			var resolution = (ISpatialReferenceResolution) spatialReference;
			double xyResolution = resolution.get_XYResolution(true);

			return string.Format(LocalizableStrings.QaSchemaSpatialReference_XYPrecision,
			                     xmin, ymin, xmax, ymax, xyResolution);
		}

		[NotNull]
		private static string GetVCSDisplayName([NotNull] ISpatialReference spatialReference)
		{
			IVerticalCoordinateSystem vcs =
				SpatialReferenceUtils.GetVerticalCoordinateSystem(spatialReference);

			return vcs == null
				       ? LocalizableStrings.QaSchemaSpatialReference_NotDefined
				       : vcs.Name;
		}

		#endregion

		#region Non-public

		[NotNull]
		private static ISpatialReference GetSpatialReference(
			[NotNull] IReadOnlyFeatureClass featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			ISpatialReference result = featureClass.SpatialReference;

			return Assert.NotNull(result,
			                      "Feature class has no spatial reference: {0}",
			                      featureClass.Name);
		}

		#endregion
	}
}
