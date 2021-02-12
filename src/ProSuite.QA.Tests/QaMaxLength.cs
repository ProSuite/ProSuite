using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Reports lines or polygon boundaries that are longer than a limit.
	/// </summary>
	[CLSCompliant(false)]
	[UsedImplicitly]
	[GeometryTest]
	public class QaMaxLength : QaLengthBase
	{
		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string LengthTooLarge = "LengthTooLarge";

			public Code() : base("MaximumLength") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaMaxLength_0))]
		public QaMaxLength(
			[Doc(nameof(DocStrings.QaMaxLength_featureClass))] IFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaMaxLength_limit))] double limit,
			[Doc(nameof(DocStrings.QaMaxLength_is3D))] bool is3D)
			: base(featureClass, limit, is3D) { }

		[Doc(nameof(DocStrings.QaMaxLength_0))]
		public QaMaxLength(
			[Doc(nameof(DocStrings.QaMaxLength_featureClass))] IFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaMaxLength_limit))] double limit)
			: base(featureClass, limit) { }

		[Doc(nameof(DocStrings.QaMaxLength_0))]
		public QaMaxLength(
			[Doc(nameof(DocStrings.QaMaxLength_featureClass))] IFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaMaxLength_limit))] double limit,
			[Doc(nameof(DocStrings.QaMaxLength_is3D))] bool is3D,
			[Doc(nameof(DocStrings.QaMaxLength_perPart))] bool perPart)
			: base(featureClass, limit, is3D, perPart) { }

		protected override int CheckLength(double length, ICurve curve, IRow row)
		{
			if (length <= Limit)
			{
				return 0;
			}

			string description = string.Format("Length {0}",
			                                   FormatLengthComparison(length, ">", Limit,
			                                                          curve.SpatialReference));

			return ReportError(curve, description, Codes[Code.LengthTooLarge], row);
		}
	}
}
