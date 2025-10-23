using System;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Check whether the x or y extent of features - or feature parts - exceeds a given limit.
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	public class QaExtent : QaExtentBase
	{
		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string ExtentLargerThanLimit = "ExtentLargerThanLimit";

			public Code() : base("Extent") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaExtent_0))]
		public QaExtent(
				[Doc(nameof(DocStrings.QaExtent_featureClass))]
				IReadOnlyFeatureClass featureClass,
				[Doc(nameof(DocStrings.QaExtent_limit))]
				double limit)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, limit, false) { }

		[Doc(nameof(DocStrings.QaExtent_1))]
		public QaExtent(
			[Doc(nameof(DocStrings.QaExtent_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaExtent_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaExtent_perPart))]
			bool perPart)
			: base(featureClass, limit)
		{
			NumberFormat = "N1";

			_perPart = perPart;
		}

		[InternallyUsedTest]
		public QaExtent(
		[NotNull] QaExtentDefinition definition)
			: this((IReadOnlyFeatureClass)definition.FeatureClass, definition.Limit, definition.PerPart)
		{
		}

		public override bool IsQueriedTable(int tableIndex)
		{
			return false;
		}

		public override bool RetestRowsPerIntersectedTile(int tableIndex)
		{
			return false;
		}

		protected override int ExecutePartForRow(IReadOnlyRow row, IGeometry geometry)
		{
			IEnvelope envelope = GetEnvelope(geometry);

			if (envelope.IsEmpty)
			{
				return NoError;
			}

			double max = Math.Max(envelope.Width, envelope.Height);

			if (max <= Limit)
			{
				return NoError;
			}

			string description = string.Format(
				"Extent {0}",
				FormatLengthComparison(max, ">", Limit, geometry.SpatialReference));

			IGeometry errorGeometry = GeometryUtils.GetHighLevelGeometry(geometry);

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(row),
				errorGeometry, Codes[Code.ExtentLargerThanLimit],
				TestUtils.GetShapeFieldName(row));
		}
	}
}
