using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Properties;
using ProSuite.QA.Tests.Schema;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[SchemaTest]
	public class QaSchemaSpatialReference : QaSchemaTestBase
	{
		private readonly IFeatureClass _featureClass;
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

		// TODO document
		/// <summary>
		/// Initializes a new instance of the <see cref="QaSchemaSpatialReference"/> class.
		/// </summary>
		/// <param name="featureClass">The feature class.</param>
		/// <param name="referenceFeatureClass">The reference feature class.</param>
		/// <param name="compareXYPrecision">if set to <c>true</c>, xy precision must be equal.</param>
		/// <param name="compareZPrecision">if set to <c>true</c>, z precision must be equal.</param>
		/// <param name="compareMPrecision">if set to <c>true</c>, m precision must be equal.</param>
		/// <param name="compareTolerances">if set to <c>true</c>, tolerances must be equal.</param>
		/// <param name="compareVerticalCoordinateSystems">if set to <c>true</c>, vertical coordinate system must be equal.</param>
		[Doc(nameof(DocStrings.QaSchemaSpatialReference_0))]
		public QaSchemaSpatialReference(
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_featureClass))] [NotNull]
			IFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_referenceFeatureClass))] [NotNull]
			IFeatureClass
				referenceFeatureClass,
			bool compareXYPrecision,
			bool compareZPrecision,
			bool compareMPrecision,
			bool compareTolerances,
			bool compareVerticalCoordinateSystems)
			: this(featureClass, GetSpatialReference(referenceFeatureClass),
			       compareXYPrecision, compareZPrecision, compareMPrecision,
			       compareTolerances, compareTolerances, compareTolerances,
			       compareVerticalCoordinateSystems) { }

		// TODO document
		/// <summary>
		/// Initializes a new instance of the <see cref="QaSchemaSpatialReference"/> class.
		/// </summary>
		/// <param name="featureClass">The feature class.</param>
		/// <param name="spatialReferenceXml">The spatial reference XML.</param>
		/// <param name="compareXYPrecision">if set to <c>true</c>, xy precision must be equal.</param>
		/// <param name="compareZPrecision">if set to <c>true</c>, z precision must be equal.</param>
		/// <param name="compareMPrecision">if set to <c>true</c>, m precision must be equal.</param>
		/// <param name="compareTolerances">if set to <c>true</c>, tolerances must be equal.</param>
		/// <param name="compareVerticalCoordinateSystems">if set to <c>true</c>, vertical coordinate system must be equal.</param>
		[Doc(nameof(DocStrings.QaSchemaSpatialReference_1))]
		public QaSchemaSpatialReference(
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_featureClass))] [NotNull]
			IFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_spatialReferenceXml))] [NotNull]
			string
				spatialReferenceXml,
			bool compareXYPrecision,
			bool compareZPrecision,
			bool compareMPrecision,
			bool compareTolerances,
			bool compareVerticalCoordinateSystems)
			: this(featureClass,
			       SpatialReferenceUtils.FromXmlString(spatialReferenceXml),
			       compareXYPrecision, compareZPrecision, compareMPrecision,
			       compareTolerances, compareTolerances, compareTolerances,
			       compareVerticalCoordinateSystems) { }

		// TODO document
		/// <summary>
		/// Initializes a new instance of the <see cref="QaSchemaSpatialReference"/> class.
		/// </summary>
		/// <param name="featureClass">The feature class.</param>
		/// <param name="referenceFeatureClass">The reference feature class.</param>
		/// <param name="compareUsedPrecisions">if set to <c>true</c> the precisions for the relevant dimensions must be equal.</param>
		/// <param name="compareTolerances">if set to <c>true</c>, tolerances of the relevant dimensions must be equal.</param>
		/// <param name="compareVerticalCoordinateSystems">if set to <c>true</c>, vertical coordinate system must be equal.</param>
		[Doc(nameof(DocStrings.QaSchemaSpatialReference_2))]
		public QaSchemaSpatialReference(
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_featureClass))] [NotNull]
			IFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_referenceFeatureClass))] [NotNull]
			IFeatureClass
				referenceFeatureClass,
			bool compareUsedPrecisions,
			bool compareTolerances,
			bool compareVerticalCoordinateSystems)
			: this(featureClass, GetSpatialReference(referenceFeatureClass),
			       compareUsedPrecisions,
			       compareUsedPrecisions && DatasetUtils.HasZ(featureClass),
			       compareUsedPrecisions && DatasetUtils.HasM(featureClass),
			       compareTolerances,
			       compareTolerances && DatasetUtils.HasZ(featureClass),
			       compareTolerances && DatasetUtils.HasM(featureClass),
			       compareVerticalCoordinateSystems) { }

		// TODO document
		/// <summary>
		/// Initializes a new instance of the <see cref="QaSchemaSpatialReference"/> class.
		/// </summary>
		/// <param name="featureClass">The feature class.</param>
		/// <param name="spatialReferenceXml">The spatial reference XML.</param>
		/// <param name="compareUsedPrecisions">if set to <c>true</c> the precisions for the relevant dimensions must be equal.</param>
		/// <param name="compareTolerances">if set to <c>true</c>, tolerances of the relevant dimensions must be equal.</param>
		/// <param name="compareVerticalCoordinateSystems">if set to <c>true</c>, vertical coordinate system must be equal.</param>
		[Doc(nameof(DocStrings.QaSchemaSpatialReference_3))]
		public QaSchemaSpatialReference(
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_featureClass))] [NotNull]
			IFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_spatialReferenceXml))] [NotNull]
			string
				spatialReferenceXml,
			bool compareUsedPrecisions,
			bool compareTolerances,
			bool compareVerticalCoordinateSystems)
			: this(featureClass,
			       SpatialReferenceUtils.FromXmlString(spatialReferenceXml),
			       compareUsedPrecisions,
			       compareUsedPrecisions && DatasetUtils.HasZ(featureClass),
			       compareUsedPrecisions && DatasetUtils.HasM(featureClass),
			       compareTolerances,
			       compareTolerances && DatasetUtils.HasZ(featureClass),
			       compareTolerances && DatasetUtils.HasM(featureClass),
			       compareVerticalCoordinateSystems) { }

		/// <summary>
		/// Prevents a default instance of the <see cref="QaSchemaSpatialReference"/> class from being created.
		/// </summary>
		/// <param name="featureClass">The feature class.</param>
		/// <param name="expectedSpatialReference">The expected spatial reference.</param>
		/// <param name="compareXyPrecision">if set to <c>true</c>, xy precision must be equal.</param>
		/// <param name="compareZPrecision">if set to <c>true</c>, z precision must be equal.</param>
		/// <param name="compareMPrecision">if set to <c>true</c>, m precision must be equal.</param>
		/// <param name="compareXyTolerance">if set to <c>true</c>, xy tolerance must be equal.</param>
		/// <param name="compareZTolerance">if set to <c>true</c>, z tolerance must be equal.</param>
		/// <param name="compareMTolerance">if set to <c>true</c>, m tolerance must be equal.</param>
		/// <param name="compareVerticalCoordinateSystems">if set to <c>true</c>, vertical coordinate system must be equal.</param>
		private QaSchemaSpatialReference(
			[NotNull] IFeatureClass featureClass,
			[NotNull] ISpatialReference expectedSpatialReference,
			bool compareXyPrecision,
			bool compareZPrecision,
			bool compareMPrecision,
			bool compareXyTolerance,
			bool compareZTolerance,
			bool compareMTolerance,
			bool compareVerticalCoordinateSystems)
			: base((ITable) featureClass)
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
			[NotNull] IFeatureClass featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			ISpatialReference result = ((IGeoDataset) featureClass).SpatialReference;

			return Assert.NotNull(result,
			                      "Feature class has no spatial reference: {0}",
			                      DatasetUtils.GetName(featureClass));
		}

		#endregion
	}
}
