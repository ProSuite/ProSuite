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
	/// Finds large polygons or polygon parts
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	public class QaMaxArea : QaAreaBase
	{
		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string AreaTooLarge = "AreaTooLarge";

			public Code() : base("MaxArea") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaMaxArea_0))]
		public QaMaxArea(
				[Doc(nameof(DocStrings.QaMaxArea_polygonClass))]
				IReadOnlyFeatureClass polygonClass,
				[Doc(nameof(DocStrings.QaMaxArea_limit))]
				double limit)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polygonClass, limit, false) { }

		[Doc(nameof(DocStrings.QaMaxArea_1))]
		public QaMaxArea(
			[Doc(nameof(DocStrings.QaMaxArea_polygonClass))]
			IReadOnlyFeatureClass polygonClass,
			[Doc(nameof(DocStrings.QaMaxArea_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaMaxArea_perPart))]
			bool perPart)
			: base(polygonClass, limit, perPart) { }

		[InternallyUsedTest]
		public QaMaxArea(
			[NotNull] QaMaxAreaDefinition definition)
			: this((IReadOnlyFeatureClass)definition.PolygonClass,
			       definition.Limit, definition.PerPart)
		{ }

		protected override int CheckArea(double area, IGeometry shape, IReadOnlyRow row)
		{
			return Math.Abs(area) <= Limit
				       ? 0
				       : ReportError(shape, area, ">", Codes[Code.AreaTooLarge], row);
		}
	}
}
