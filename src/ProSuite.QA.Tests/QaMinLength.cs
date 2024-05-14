using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Finds all lines that are too short
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	public class QaMinLength : QaLengthBase
	{
		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string LengthTooSmall = "LengthTooSmall";

			public Code() : base("MinimumLength") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaMinLength_0))]
		public QaMinLength(
			[Doc(nameof(DocStrings.QaMinLength_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaMinLength_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaMinLength_is3D))]
			bool is3D)
			: base(featureClass, limit, is3D) { }

		[Doc(nameof(DocStrings.QaMinLength_0))]
		public QaMinLength(
			[Doc(nameof(DocStrings.QaMinLength_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaMinLength_limit))]
			double limit)
			: base(featureClass, limit) { }

		[Doc(nameof(DocStrings.QaMinLength_0))]
		public QaMinLength(
			[Doc(nameof(DocStrings.QaMinLength_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaMinLength_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaMinLength_is3D))]
			bool is3D,
			[Doc(nameof(DocStrings.QaMinLength_perPart))]
			bool perPart)
			: base(featureClass, limit, is3D, perPart) { }

		[InternallyUsedTest]
		public QaMinLength(
			[NotNull] QaMinLengthDefinition definition)
			: this((IReadOnlyFeatureClass)definition.FeatureClass,
			       definition.Limit,
			       definition.Is3D,
			       definition.PerPart)
		{ }

		protected override int CheckLength(double length, ICurve curve, IReadOnlyRow row)
		{
			if (length >= Limit)
			{
				return 0;
			}

			string description = string.Format("Length {0}",
			                                   FormatLengthComparison(length, "<", Limit,
				                                   curve.SpatialReference));

			return ReportError(curve, description, Codes[Code.LengthTooSmall], row);
		}
	}
}
