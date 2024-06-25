using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	public class QaRowCount : NonContainerTest
	{
		private readonly IReadOnlyTable _table;
		private readonly int _minimumRowCount;
		private readonly int _maximumRowCount;

		private readonly IList<IReadOnlyTable> _referenceTables;
		private readonly OffsetSpecification _minimumValueOffset;
		private readonly OffsetSpecification _maximumValueOffset;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string TooFewRows = "TooFewRows";
			public const string TooManyRows = "TooManyRows";

			public Code() : base("RowCount") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaRowCount_0))]
		public QaRowCount(
			[Doc(nameof(DocStrings.QaRowCount_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaRowCount_minimumRowCount))]
			int minimumRowCount,
			[Doc(nameof(DocStrings.QaRowCount_maximumRowCount))]
			int maximumRowCount)
			: base(new[] {table})
		{
			Assert.ArgumentNotNull(table, nameof(table));

			_table = table;
			_minimumRowCount = minimumRowCount;
			_maximumRowCount = maximumRowCount;
		}

		[Doc(nameof(DocStrings.QaRowCount_1))]
		public QaRowCount(
			[Doc(nameof(DocStrings.QaRowCount_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaRowCount_referenceTables))] [NotNull]
			IList<IReadOnlyTable> referenceTables,
			[Doc(nameof(DocStrings.QaRowCount_minimumValueOffset))] [CanBeNull]
			string minimumValueOffset,
			[Doc(nameof(DocStrings.QaRowCount_maximumValueOffset))] [CanBeNull]
			string maximumValueOffset)
			: base(Union(new[] {table}, referenceTables))
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(referenceTables, nameof(referenceTables));

			_table = table;
			_referenceTables = new List<IReadOnlyTable>(referenceTables);

			CultureInfo formatProvider = CultureInfo.InvariantCulture;
			_minimumValueOffset = OffsetSpecification.Parse(minimumValueOffset, formatProvider);
			_maximumValueOffset = OffsetSpecification.Parse(maximumValueOffset, formatProvider);

			Assert.ArgumentCondition(
				_minimumValueOffset != null || _maximumValueOffset != null,
				"At least one offset must be specified");
		}

		/// <summary>
		/// Constructor using Definition. Must always be the last constructor!
		/// </summary>
		/// <param name="rowCountDef"></param>
		[InternallyUsedTest]
		public QaRowCount([NotNull] QaRowCountDefinition rowCountDef)
			: base(rowCountDef.InvolvedTables.Cast<IReadOnlyTable>().ToList())
		{
			_table = (IReadOnlyTable) rowCountDef.Table;

			bool hasReferenceTables = rowCountDef.ReferenceTables?.Count > 0;

			if (hasReferenceTables)
			{
				_referenceTables = rowCountDef.ReferenceTables.Cast<IReadOnlyTable>().ToList();
				_minimumValueOffset = rowCountDef.MinimumValueOffset;
				_maximumValueOffset = rowCountDef.MaximumValueOffset;
			}
			else
			{
				_minimumRowCount = rowCountDef.MinimumRowCount;
				_maximumRowCount = rowCountDef.MaximumRowCount;
			}
		}

		public override int Execute()
		{
			return ExecuteGeometry(null);
		}

		public override int Execute(IEnvelope boundingBox)
		{
			return ExecuteGeometry(boundingBox);
		}

		public override int Execute(IPolygon area)
		{
			return ExecuteGeometry(area);
		}

		public override int Execute(IEnumerable<IReadOnlyRow> selectedRows)
		{
			// TODO what to do with selection?
			return NoError;
		}

		public override int Execute(IReadOnlyRow row)
		{
			// TODO what to do with individual row?
			return NoError;
		}

		protected override ISpatialReference GetSpatialReference()
		{
			var featureClass = _table as IReadOnlyFeatureClass;

			return featureClass?.SpatialReference;
		}

		private int ExecuteGeometry([CanBeNull] IGeometry geometry)
		{
			const int verifiedTableIndex = 0;

			long rowCount = GetRowCount(_table, verifiedTableIndex, geometry);

			if (_referenceTables == null)
			{
				return ReportErrors(rowCount, _minimumRowCount, _maximumRowCount);
			}

			long referenceRowCount = GetReferenceRowCount(geometry);

			return ReportErrors(rowCount, referenceRowCount,
			                    _minimumValueOffset,
			                    _maximumValueOffset);
		}

		private long GetReferenceRowCount([CanBeNull] IGeometry geometry)
		{
			Assert.NotNull(_referenceTables, "_referenceTables");

			long result = 0;

			var referenceTableIndex = 1;
			foreach (IReadOnlyTable referenceTable in _referenceTables)
			{
				result += GetRowCount(referenceTable, referenceTableIndex, geometry);

				referenceTableIndex++;
			}

			return result;
		}

		private long GetRowCount([NotNull] IReadOnlyTable table,
		                         int tableIndex,
		                         [CanBeNull] IGeometry geometry)
		{
			var featureClass = table as IReadOnlyFeatureClass;

			IGeometry searchGeometry = featureClass == null
				                           ? null
				                           : geometry;

			ITableFilter filter = CreateQueryFilter(table, tableIndex, searchGeometry);

			return table.RowCount(filter);
		}

		private int ReportErrors(long rowCount, int minimumRowCount, int maximumRowCount)
		{
			if (rowCount < minimumRowCount)
			{
				string description = string.Format(
					"Row count is less than minimum value: {0:N0} < {1:N0}",
					rowCount, minimumRowCount);

				return ReportError(description, _table, Codes[Code.TooFewRows], null);
			}

			if (maximumRowCount >= 0 && rowCount > maximumRowCount)
			{
				string description = string.Format(
					"Row count is greater than maximum value: {0:N0} > {1:N0}",
					rowCount, maximumRowCount);

				return ReportError(description, _table, Codes[Code.TooManyRows], null);
			}

			return NoError;
		}

		private int ReportErrors(long rowCount,
		                         long referenceRowCount,
		                         [CanBeNull] OffsetSpecification minimumValueOffset,
		                         [CanBeNull] OffsetSpecification maximumValueOffset)
		{
			if (minimumValueOffset != null)
			{
				var minimumRowCount =
					(int) Math.Ceiling(minimumValueOffset.ApplyTo(referenceRowCount));

				if (rowCount < minimumRowCount)
				{
					string description = string.Format(
						"Row count is less than minimum value: {0:N0} < {1:N0} (reference row count: {2:N0})",
						rowCount, minimumRowCount, referenceRowCount);

					return ReportError(description, _table, Codes[Code.TooFewRows], null);
				}
			}

			if (maximumValueOffset != null)
			{
				var maximumRowCount =
					(int) Math.Floor(maximumValueOffset.ApplyTo(referenceRowCount));

				if (rowCount > maximumRowCount)
				{
					string description = string.Format(
						"Row count is greater than maximum value: {0:N0} > {1:N0} (reference row count: {2:N0})",
						rowCount, maximumRowCount, referenceRowCount);

					return ReportError(description, _table, Codes[Code.TooManyRows], null);
				}
			}

			return NoError;
		}

		[NotNull]
		private ITableFilter CreateQueryFilter([NotNull] IReadOnlyTable table,
		                                       int tableIndex,
		                                       [CanBeNull] IGeometry geometry)
		{
			return TestUtils.CreateFilter(geometry, AreaOfInterest,
			                              GetConstraint(tableIndex),
			                              table, null);
		}
	}
}
