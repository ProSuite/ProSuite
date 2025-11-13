using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core;
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
		private const bool _defaultCompareXyDomainOrigin = false;
		private const bool _defaultCompareZDomainOrigin = false;
		private const bool _defaultCompareMDomainOrigin = false;

		private const bool _defaultCompareXyResolution = false;
		private const bool _defaultCompareZResolution = false;
		private const bool _defaultCompareMResolution = false;

		private readonly IReadOnlyFeatureClass _featureClass;
		private readonly ISpatialReference _expectedSpatialReference;
		private bool _compareXyPrecision;
		private bool _compareZPrecision;
		private bool _compareMPrecision;
		private readonly bool _compareXyTolerance;
		private bool _compareZTolerance;
		private bool _compareMTolerance;
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
			public const string DomainOriginDifferent_XY = "DomainOriginDifferent.XY";
			public const string DomainOriginDifferent_Z = "DomainOriginDifferent.Z";
			public const string DomainOriginDifferent_M = "DomainOriginDifferent.M";
			public const string ResolutionDifferent_XY = "ResolutionDifferent.XY";
			public const string ResolutionDifferent_Z = "ResolutionDifferent.Z";
			public const string ResolutionDifferent_M = "ResolutionDifferent.M";

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
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_compareXYPrecision))]
			bool compareXYPrecision, bool compareZPrecision,
			bool compareMPrecision, bool compareTolerances,
			bool compareVerticalCoordinateSystems)
			: this(featureClass, GetSpatialReference(referenceFeatureClass),
				   compareXYPrecision, compareZPrecision, compareMPrecision,
				   compareTolerances, compareTolerances, compareTolerances,
				   compareVerticalCoordinateSystems)
		{
			AddDummyTable(referenceFeatureClass);
		}

		[Doc(nameof(DocStrings.QaSchemaSpatialReference_1))]
		public QaSchemaSpatialReference(
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_spatialReferenceXml))] [NotNull]
			string spatialReferenceXml,
			[Doc(nameof(DocStrings.QaSchemaSpatialReference_compareXYPrecision))]
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
			AddDummyTable(referenceFeatureClass);
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

			CompareXYDomainOrigin = _defaultCompareXyDomainOrigin;
			CompareZDomainOrigin = _defaultCompareZDomainOrigin;
			CompareMDomainOrigin = _defaultCompareMDomainOrigin;
		}

		/// <summary>
		/// Constructor using Definition. Must always be the last constructor!
		/// </summary>
		/// <param name = "schemaSpatialReferenceDef" ></param >
		[InternallyUsedTest]
		public QaSchemaSpatialReference(
			[NotNull] QaSchemaSpatialReferenceDefinition schemaSpatialReferenceDef)
			: base((IReadOnlyFeatureClass) schemaSpatialReferenceDef.FeatureClass)
		{
			_featureClass = (IReadOnlyFeatureClass) schemaSpatialReferenceDef.FeatureClass;
			_compareXyTolerance = schemaSpatialReferenceDef.CompareTolerances;
			_compareVerticalCoordinateSystems =
				schemaSpatialReferenceDef.CompareVerticalCoordinateSystems;
			_shapeFieldName = schemaSpatialReferenceDef.FeatureClass.ShapeFieldName;

			bool hasReferenceFeatureClass = schemaSpatialReferenceDef.ReferenceFeatureClass != null;
			bool hasCompareUsedPrecisions =
				schemaSpatialReferenceDef.CompareUsedPrecisions.HasValue;

			if (hasReferenceFeatureClass)
			{
				_expectedSpatialReference = GetSpatialReference(
					(IReadOnlyFeatureClass) schemaSpatialReferenceDef.ReferenceFeatureClass);
				AddDummyTable(
					(IReadOnlyFeatureClass) schemaSpatialReferenceDef.ReferenceFeatureClass);

				SetPrecisionAndToleranceValues(schemaSpatialReferenceDef,
				                               hasCompareUsedPrecisions);
			}
			else
			{
				_expectedSpatialReference =
					SpatialReferenceUtils.FromXmlString(
						schemaSpatialReferenceDef.SpatialReferenceXml);
				SetPrecisionAndToleranceValues(schemaSpatialReferenceDef,
				                               hasCompareUsedPrecisions);
			}
		}

		[TestParameter(_defaultCompareXyDomainOrigin)]
		[Doc(nameof(DocStrings.QaSchemaSpatialReference_CompareXYDomainOrigin))]
		public bool CompareXYDomainOrigin { get; set; }

		[TestParameter(_defaultCompareZDomainOrigin)]
		[Doc(nameof(DocStrings.QaSchemaSpatialReference_CompareZDomainOrigin))]
		public bool CompareZDomainOrigin { get; set; }

		[TestParameter(_defaultCompareMDomainOrigin)]
		[Doc(nameof(DocStrings.QaSchemaSpatialReference_CompareMDomainOrigin))]
		public bool CompareMDomainOrigin { get; set; }

		[TestParameter(_defaultCompareXyResolution)]
		[Doc(nameof(DocStrings.QaSchemaSpatialReference_CompareXYResolution))]
		public bool CompareXYResolution { get; set; }

		[TestParameter(_defaultCompareZResolution)]
		[Doc(nameof(DocStrings.QaSchemaSpatialReference_CompareZResolution))]
		public bool CompareZResolution { get; set; }

		[TestParameter(_defaultCompareMResolution)]
		[Doc(nameof(DocStrings.QaSchemaSpatialReference_CompareMResolution))]
		public bool CompareMResolution { get; set; }

		/// <summary>
		/// Prevents an exception saying that the number of constraints is not equal the number of involved tables.
		/// </summary>
		private void AddDummyTable(IReadOnlyFeatureClass referenceFc)
		{
			AddInvolvedTable(new DummyTable(referenceFc), null, false);
		}

		private class DummyTable : VirtualTable
		{
			private readonly IReadOnlyFeatureClass _referenceFc;
			public DummyTable(IReadOnlyFeatureClass referenceFc) : base(referenceFc.Name)
			{
				_referenceFc = referenceFc;
			}

			protected override bool EqualsCore(IReadOnlyTable obj)
			{
				return _referenceFc == obj;
			}
		}

		private void SetPrecisionAndToleranceValues(
			QaSchemaSpatialReferenceDefinition schemaSpatialReferenceDef,
			bool hasCompareUsedPrecisions)
		{
			if (hasCompareUsedPrecisions)
			{
				_compareXyPrecision =
					Assert.NotNull(schemaSpatialReferenceDef.CompareUsedPrecisions).Value;

				_compareZPrecision =
					Assert.NotNull(schemaSpatialReferenceDef.CompareUsedPrecisions).Value &&
					DatasetUtils.GetGeometryDef(
						(IReadOnlyFeatureClass) schemaSpatialReferenceDef.FeatureClass).HasZ;
				_compareMPrecision =
					Assert.NotNull(schemaSpatialReferenceDef.CompareUsedPrecisions).Value &&
					DatasetUtils.GetGeometryDef(
						(IReadOnlyFeatureClass) schemaSpatialReferenceDef.FeatureClass).HasM;

				_compareZTolerance =
					schemaSpatialReferenceDef.CompareTolerances &&
					DatasetUtils
						.GetGeometryDef(
							(IReadOnlyFeatureClass) schemaSpatialReferenceDef.FeatureClass).HasZ;
				_compareMTolerance =
					schemaSpatialReferenceDef.CompareTolerances && DatasetUtils.GetGeometryDef(
						(IReadOnlyFeatureClass) schemaSpatialReferenceDef
							.FeatureClass).HasM;
			}
			else
			{
				_compareXyPrecision = schemaSpatialReferenceDef.CompareXYPrecision;
				_compareZPrecision = schemaSpatialReferenceDef.CompareZPrecision;
				_compareMPrecision = schemaSpatialReferenceDef.CompareMPrecision;
				_compareZTolerance = schemaSpatialReferenceDef.CompareTolerances;
				_compareMTolerance = schemaSpatialReferenceDef.CompareTolerances;
			}
		}

		#endregion

		#region Overrides of QaSchemaTestBase

		public override int Execute()
		{
			ISpatialReference actualSpatialReference = GetSpatialReference(_featureClass);

			//const bool comparePrecisionAndTolerance = true;
			//const bool compareVerticalCoordinateSystems = true;

			bool equal = SpatialReferenceUtils.AreEqual(
				_expectedSpatialReference,
				actualSpatialReference,
				out bool coordinateSystemDifferent,
				out bool vcsDifferent,
				out bool xyPrecisionDifferent,
				out bool zPrecisionDifferent,
				out bool mPrecisionDifferent,
				out bool xyToleranceDifferent,
				out bool zToleranceDifferent,
				out bool mToleranceDifferent);

			bool xyDomainOriginDifferent = GetXYDomainOriginDifferent(actualSpatialReference);
			bool zDomainOriginDifferent = GetZDomainOriginDifferent(actualSpatialReference);
			bool mDomainOriginDifferent = GetMDomainDifferent(actualSpatialReference);
			equal &= ! xyDomainOriginDifferent && ! zDomainOriginDifferent &&
			         ! mDomainOriginDifferent;

			bool xyResolutionDifferent = GetXYResolutionDifferent(actualSpatialReference);
			bool zResolutionDifferent = GetZResolutionDifferent(actualSpatialReference);
			bool mResolutionDifferent = GetMResolutionDifferent(actualSpatialReference);
			equal &= ! xyResolutionDifferent && ! zResolutionDifferent && ! mResolutionDifferent;

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
					LocalizableStrings.QaSchemaSpatialReference_XYPrecisionDifferent,
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

			if (xyDomainOriginDifferent)
			{
				errorCount += Report(
					Codes[Code.DomainOriginDifferent_XY],
					LocalizableStrings.QaSchemaSpatialReference_XYDomainOriginDifferent,
					GetXYDomainOriginString(_expectedSpatialReference),
					GetXYDomainOriginString(actualSpatialReference));
			}

			if (zDomainOriginDifferent)
			{
				errorCount += Report(
					Codes[Code.DomainOriginDifferent_Z],
					LocalizableStrings.QaSchemaSpatialReference_ZDomainOriginDifferent,
					GetZDomainOriginString(_expectedSpatialReference),
					GetZDomainOriginString(actualSpatialReference));
			}

			if (mDomainOriginDifferent)
			{
				errorCount += Report(
					Codes[Code.DomainOriginDifferent_M],
					LocalizableStrings.QaSchemaSpatialReference_MDomainOriginDifferent,
					GetMDomainOriginString(_expectedSpatialReference),
					GetMDomainOriginString(actualSpatialReference));
			}

			if (xyResolutionDifferent)
			{
				errorCount += Report(
					Codes[Code.ResolutionDifferent_XY],
					LocalizableStrings.QaSchemaSpatialReference_XYResolutionDifferent,
					GetXYResolutionString(_expectedSpatialReference),
					GetXYResolutionString(actualSpatialReference));
			}

			if (zResolutionDifferent)
			{
				errorCount += Report(
					Codes[Code.ResolutionDifferent_Z],
					LocalizableStrings.QaSchemaSpatialReference_ZResolutionDifferent,
					GetZResolutionString(_expectedSpatialReference),
					GetZResolutionString(actualSpatialReference));
			}

			if (mResolutionDifferent)
			{
				errorCount += Report(
					Codes[Code.ResolutionDifferent_M],
					LocalizableStrings.QaSchemaSpatialReference_MResolutionDifferent,
					GetMResolutionString(_expectedSpatialReference),
					GetMResolutionString(actualSpatialReference));
			}

			return errorCount;
		}

		private bool GetXYDomainOriginDifferent(ISpatialReference compareSpatialReference)
		{
			if (!CompareXYDomainOrigin)
			{
				return false;
			}

			_expectedSpatialReference.GetDomain(out double exMin, out _,
			                                    out double eyMin, out _);
			compareSpatialReference.GetDomain(out double cxMin, out _,
			                                  out double cyMin, out _);
			return ! (exMin.Equals(cxMin) && eyMin.Equals(cyMin));
		}

		private bool GetXYResolutionDifferent(ISpatialReference compareSpatialReference)
		{
			if (!CompareXYResolution)
			{
				return false;
			}

			double eRes = SpatialReferenceUtils.GetXyResolution(_expectedSpatialReference);
			double cRes = SpatialReferenceUtils.GetXyResolution(compareSpatialReference);
			return !eRes.Equals(cRes);
		}


		private bool GetZDomainOriginDifferent(ISpatialReference compareSpatialReference)
		{
			if (!CompareZDomainOrigin)
			{
				return false;
			}

			_expectedSpatialReference.GetZDomain(out double ezMin, out _);
			compareSpatialReference.GetZDomain(out double czMin, out _);
			return ! ezMin.Equals(czMin);
		}

		private bool GetZResolutionDifferent(ISpatialReference compareSpatialReference)
		{
			if (!CompareZResolution)
			{
				return false;
			}

			double eRes = SpatialReferenceUtils.GetZResolution(_expectedSpatialReference);
			double cRes = SpatialReferenceUtils.GetZResolution(compareSpatialReference);
			return !eRes.Equals(cRes);
		}

		private bool GetMDomainDifferent(ISpatialReference compareSpatialReference)
		{
			if (!CompareMDomainOrigin)
			{
				return false;
			}

			_expectedSpatialReference.GetMDomain(out double emMin, out _);
			compareSpatialReference.GetMDomain(out double cmMin, out _);
			return !emMin.Equals(cmMin);
		}

		private bool GetMResolutionDifferent(ISpatialReference compareSpatialReference)
		{
			if (!CompareMResolution)
			{
				return false;
			}

			double eRes = SpatialReferenceUtils.GetMResolution(_expectedSpatialReference);
			double cRes = SpatialReferenceUtils.GetMResolution(compareSpatialReference);
			return !eRes.Equals(cRes);
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
			if (!spatialReference.HasMPrecision())
			{
				return LocalizableStrings.QaSchemaSpatialReference_NotDefined;
			}

			double mmin;
			double mmax;
			spatialReference.GetMDomain(out mmin, out mmax);

			var resolution = (ISpatialReferenceResolution)spatialReference;
			double mResolution = resolution.MResolution;

			return string.Format(LocalizableStrings.QaSchemaSpatialReference_MPrecision,
								 mmin, mmax, mResolution);
		}

		private static string GetMDomainOriginString(
			[NotNull] ISpatialReference spatialReference)
		{
			spatialReference.GetMDomain(out double origin, out _);
			return $"{origin}";
		}

		private static string GetMResolutionString([NotNull] ISpatialReference spatialReference)
		{
			var resolution = (ISpatialReferenceResolution) spatialReference;
			return $"{resolution.MResolution}";
		}

		[NotNull]
		private static string GetZPrecisionString(
			[NotNull] ISpatialReference spatialReference)
		{
			if (!spatialReference.HasZPrecision())
			{
				return LocalizableStrings.QaSchemaSpatialReference_NotDefined;
			}

			double zmin;
			double zmax;
			spatialReference.GetZDomain(out zmin, out zmax);

			var resolution = (ISpatialReferenceResolution)spatialReference;
			double zResolution = resolution.get_ZResolution(true);

			return string.Format(LocalizableStrings.QaSchemaSpatialReference_ZPrecision,
								 zmin, zmax, zResolution);
		}

		private static string GetZDomainOriginString(
			[NotNull] ISpatialReference spatialReference)
		{
			spatialReference.GetZDomain(out double origin, out _);
			return $"{origin}";
		}

		private static string GetZResolutionString([NotNull] ISpatialReference spatialReference)
		{
			var resolution = (ISpatialReferenceResolution)spatialReference;
			return $"{resolution.get_ZResolution(bStandardUnits: true)}";
		}

		[NotNull]
		private static string GetXyPrecisionString(
			[NotNull] ISpatialReference spatialReference)
		{
			if (!spatialReference.HasXYPrecision())
			{
				return LocalizableStrings.QaSchemaSpatialReference_NotDefined;
			}

			double xmin;
			double xmax;
			double ymin;
			double ymax;
			spatialReference.GetDomain(out xmin, out xmax, out ymin, out ymax);

			var resolution = (ISpatialReferenceResolution)spatialReference;
			double xyResolution = resolution.get_XYResolution(true);

			return string.Format(LocalizableStrings.QaSchemaSpatialReference_XYPrecision,
								 xmin, ymin, xmax, ymax, xyResolution);
		}

		private static string GetXYDomainOriginString(
			[NotNull] ISpatialReference spatialReference)
		{
			spatialReference.GetDomain(out double xOrigin, out _, out double yOrigin, out _);
			return $"{xOrigin}, {yOrigin}";
		}

		private static string GetXYResolutionString([NotNull] ISpatialReference spatialReference)
		{
			var resolution = (ISpatialReferenceResolution)spatialReference;
			return $"{resolution.get_XYResolution(bStandardUnits: true)}";
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
