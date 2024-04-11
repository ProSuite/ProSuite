using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
	public class QaExtent : ContainerTest
	{
		private readonly double _limit;
		private readonly bool _perPart;
		private readonly IEnvelope _envelopeTemplate = new EnvelopeClass();

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
			: base(featureClass)
		{
			_limit = limit;
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

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			var feature = (IReadOnlyFeature) row;

			IGeometry shape = feature.Shape;

			if (! _perPart)
			{
				return ExecutePart(row, shape);
			}

			int errorCount = 0;

			foreach (IGeometry part in GetParts(shape))
			{
				errorCount += ExecutePart(row, part);

				if (part != shape)
				{
					// the part is some sub-component of the feature, either 
					// a ring, path or a connected-component polygon
					// -> release it to avoid pushing the VM allocation up
					Marshal.ReleaseComObject(part);
				}
			}

			return errorCount;
		}

		[NotNull]
		private static IEnumerable<IGeometry> GetParts([NotNull] IGeometry shape)
		{
			var polygon = shape as IPolygon;
			if (polygon != null)
			{
				foreach (
					IGeometry part in
					TestUtils.GetParts(polygon, PolygonPartType.ExteriorRing))
				{
					yield return part;
				}
			}
			else
			{
				foreach (IGeometry part in GeometryUtils.GetParts((IGeometryCollection) shape))
				{
					yield return part;
				}
			}
		}

		private int ExecutePart([NotNull] IReadOnlyRow row, [NotNull] IGeometry geometry)
		{
			IEnvelope envelope = GetEnvelope(geometry);

			if (envelope.IsEmpty)
			{
				return NoError;
			}

			double max = Math.Max(envelope.Width, envelope.Height);

			if (max <= _limit)
			{
				return NoError;
			}

			string description = string.Format("Extent {0}",
			                                   FormatLengthComparison(
				                                   max, ">", _limit,
				                                   geometry.SpatialReference));
			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(row),
				geometry, Codes[Code.ExtentLargerThanLimit],
				TestUtils.GetShapeFieldName(row));
		}

		[NotNull]
		private IEnvelope GetEnvelope([NotNull] IGeometry geometry)
		{
			try
			{
				geometry.QueryEnvelope(_envelopeTemplate);
			}
			catch (COMException)
			{
				// empty geometry?
				_envelopeTemplate.SetEmpty();
			}

			return _envelopeTemplate;
		}
	}
}
