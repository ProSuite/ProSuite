using System;
using System.Collections.Generic;
using System.IO;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.PointCloud;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Checks the LAS files of a point cloud that cover the verified features: that the file is
	/// there and its header can be read at all, and that the Z range the header declares lies
	/// within the given limits.
	/// <para>
	/// Each LAS file is checked once, however many features and tiles it covers - what is being
	/// checked is the file, not the feature - and the issue geometry is the tile the file belongs
	/// to, which is where a reader can go looking.
	/// </para>
	/// </summary>
	[UsedImplicitly]
	[ZValuesTest]
	public class QaLasFileZRange : ContainerTest
	{
		[NotNull] private readonly PointCloudReference _pointCloud;
		private readonly double _minimumZ;
		private readonly double _maximumZ;

		[NotNull] private readonly HashSet<string> _checkedFiles =
			new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string NoLasFile = "NoLasFile";

			public const string InvalidLasHeader = "InvalidLasHeader";

			public const string ZRange_TooLow = "ZRange.TooLow";

			public const string ZRange_TooHigh = "ZRange.TooHigh";

			public Code() : base("LasFileZRange") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaLasFileZRange_0))]
		public QaLasFileZRange(
			[Doc(nameof(DocStrings.QaLasFileZRange_perimeterClass))] [NotNull]
			IReadOnlyFeatureClass perimeterClass,
			[Doc(nameof(DocStrings.QaLasFileZRange_pointCloud))] [NotNull]
			PointCloudReference pointCloud,
			[Doc(nameof(DocStrings.QaLasFileZRange_minimumZ))]
			double minimumZ,
			[Doc(nameof(DocStrings.QaLasFileZRange_maximumZ))]
			double maximumZ)
			: base(perimeterClass)
		{
			Assert.ArgumentNotNull(perimeterClass, nameof(perimeterClass));
			Assert.ArgumentNotNull(pointCloud, nameof(pointCloud));
			Assert.ArgumentCondition(minimumZ <= maximumZ,
			                         "minimumZ {0} is above maximumZ {1}", minimumZ, maximumZ);

			_pointCloud = pointCloud;
			_minimumZ = minimumZ;
			_maximumZ = maximumZ;
		}

		[InternallyUsedTest]
		public QaLasFileZRange([NotNull] QaLasFileZRangeDefinition definition)
			: this((IReadOnlyFeatureClass) definition.PerimeterClass,
			       (PointCloudReference) definition.PointCloud,
			       definition.MinimumZ, definition.MaximumZ) { }

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
			if (! (row is IReadOnlyFeature feature))
			{
				return NoError;
			}

			IGeometry shape = feature.Shape;

			if (shape == null || shape.IsEmpty)
			{
				return NoError;
			}

			var errorCount = 0;

			foreach ((string filePath, IEnvelope tileExtent) in
			         _pointCloud.GetFiles(shape.Envelope))
			{
				// The same file typically covers many features, and a tile many rows.
				if (! _checkedFiles.Add(filePath))
				{
					continue;
				}

				errorCount += CheckFile(filePath, tileExtent, row);
			}

			return errorCount;
		}

		private int CheckFile([NotNull] string filePath,
		                      [NotNull] IEnvelope tileExtent,
		                      [NotNull] IReadOnlyRow row)
		{
			IGeometry errorGeometry = CreateErrorGeometry(tileExtent);

			if (! LasHeaderInfo.TryRead(filePath, out LasHeaderInfo header, out string message))
			{
				string issueCode = IsMissing(filePath)
					                   ? Code.NoLasFile
					                   : Code.InvalidLasHeader;

				return ReportError($"{message}: {filePath}",
				                   InvolvedRowUtils.GetInvolvedRows(row),
				                   errorGeometry, Codes[issueCode], null,
				                   values: new object[] { filePath });
			}

			var errorCount = 0;

			if (header.MinZ < _minimumZ)
			{
				errorCount += ReportError(
					$"The LAS file declares a minimum Z below the limit " +
					$"({FormatComparison(header.MinZ, "<", _minimumZ, null)}): {filePath}",
					InvolvedRowUtils.GetInvolvedRows(row),
					errorGeometry, Codes[Code.ZRange_TooLow], null,
					values: new object[] { filePath, header.MinZ });
			}

			if (header.MaxZ > _maximumZ)
			{
				errorCount += ReportError(
					$"The LAS file declares a maximum Z above the limit " +
					$"({FormatComparison(header.MaxZ, ">", _maximumZ, null)}): {filePath}",
					InvolvedRowUtils.GetInvolvedRows(row),
					errorGeometry, Codes[Code.ZRange_TooHigh], null,
					values: new object[] { filePath, header.MaxZ });
			}

			return errorCount;
		}

		[NotNull]
		private IGeometry CreateErrorGeometry([NotNull] IEnvelope tileExtent)
		{
			IPolygon result = GeometryFactory.CreatePolygon(tileExtent);

			result.SpatialReference = _pointCloud.SpatialReference;

			return result;
		}

		private static bool IsMissing([NotNull] string filePath)
		{
			try
			{
				return ! File.Exists(filePath);
			}
			catch (Exception)
			{
				// An unusable path (too long, invalid characters, an unreachable share) is a
				// missing file as far as this test is concerned.
				return true;
			}
		}
	}
}
