using System;
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
	/// Finds small polygons or polygon parts
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	public class QaMinArea : QaAreaBase
	{
		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string AreaTooSmall = "AreaTooSmall";

			public Code() : base("MinArea") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaMinArea_0))]
		public QaMinArea(
				[Doc(nameof(DocStrings.QaMinArea_polygonClass))]
				IReadOnlyFeatureClass polygonClass,
				[Doc(nameof(DocStrings.QaMinArea_limit))]
				double limit)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polygonClass, limit, false) { }

		[Doc(nameof(DocStrings.QaMinArea_1))]
		public QaMinArea(
			[Doc(nameof(DocStrings.QaMinArea_polygonClass))]
			IReadOnlyFeatureClass polygonClass,
			[Doc(nameof(DocStrings.QaMinArea_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaMinArea_perPart))]
			bool perPart)
			: base(polygonClass, limit, perPart) { }

		[InternallyUsedTest]
		public QaMinArea(
			[NotNull] QaMinAreaDefinition definition)
			: this((IReadOnlyFeatureClass)definition.PolygonClass,
			       definition.Limit, definition.PerPart)
		{ }

		protected override int CheckArea(double area, IGeometry shape, IReadOnlyRow row)
		{
			return Math.Abs(area) >= Limit
				       ? 0
				       : ReportError(shape, area, "<", Codes[Code.AreaTooSmall], row);
		}
	}
}
