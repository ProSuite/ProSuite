using System;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Check whether the x and y extent of features - or feature parts - are below a given limit.
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	public class QaMinExtent : QaExtentBase
	{
		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string ExtentSmallerThanLimit = "ExtentSmallerThanLimit";

			public Code() : base("MinExtent") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaMinExtent_0))]
		public QaMinExtent(
			[Doc(nameof(DocStrings.QaExtent_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaMinExtent_limit))]
			double limit)
			: base(featureClass, limit) { }

		[TestParameter]
		[Doc(nameof(DocStrings.QaExtent_perPart))]
		public bool PerPart
		{
			get => UsePerPart;
			set => UsePerPart = value;
		}

		protected override int ExecutePartForRow(IReadOnlyRow row, IGeometry geometry)
		{
			IEnvelope envelope = GetEnvelope(geometry);

			if (envelope.IsEmpty)
			{
				return NoError;
			}

			double max = Math.Max(envelope.Width, envelope.Height);

			if (max >= Limit)
			{
				return NoError;
			}

			string description = string.Format(
				"Extent {0}",
				FormatLengthComparison(max, "<", Limit, geometry.SpatialReference));

			IGeometry errorGeometry = GeometryUtils.GetHighLevelGeometry(geometry);

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(row),
				errorGeometry, Codes[Code.ExtentSmallerThanLimit],
				TestUtils.GetShapeFieldName(row));
		}
	}
}
