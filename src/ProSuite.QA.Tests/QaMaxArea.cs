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
	/// Finds large polygons or polygon parts
	/// </summary>
	[CLSCompliant(false)]
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

		[Doc("QaMaxArea_0")]
		public QaMaxArea(
				[Doc("QaMaxArea_polygonClass")] IFeatureClass polygonClass,
				[Doc("QaMaxArea_limit")] double limit)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polygonClass, limit, false) { }

		[Doc("QaMaxArea_1")]
		public QaMaxArea(
			[Doc("QaMaxArea_polygonClass")] IFeatureClass polygonClass,
			[Doc("QaMaxArea_limit")] double limit,
			[Doc("QaMaxArea_perPart")] bool perPart)
			: base(polygonClass, limit, perPart) { }

		protected override int CheckArea(double area, IGeometry shape, IRow row)
		{
			return Math.Abs(area) <= Limit
				       ? 0
				       : ReportError(shape, area, ">", Codes[Code.AreaTooLarge], row);
		}
	}
}