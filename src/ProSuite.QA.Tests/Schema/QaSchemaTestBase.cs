using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;

namespace ProSuite.QA.Tests.Schema
{
	/// <summary>
	/// Base class for table (and feature class) schema tests
	/// </summary>
	public abstract class QaSchemaTestBase : NonContainerTest
	{
		private readonly IReadOnlyTable _table;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="QaSchemaTestBase"/> class.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <param name="referenceTable">The reference table.</param>
		protected QaSchemaTestBase([NotNull] IReadOnlyTable table,
		                           [CanBeNull] IReadOnlyTable referenceTable = null)
			: base(GetTables(table, referenceTable))
		{
			Assert.ArgumentNotNull(table, nameof(table));

			_table = table;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="QaSchemaTestBase"/> class.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <param name="referenceTables">The reference tables.</param>
		protected QaSchemaTestBase([NotNull] IReadOnlyTable table,
		                           [CanBeNull] IEnumerable<IReadOnlyTable> referenceTables)
			: base(GetTables(table, referenceTables))
		{
			Assert.ArgumentNotNull(table, nameof(table));

			_table = table;
		}

		#endregion

		public override int Execute(IReadOnlyRow row)
		{
			return Execute();
		}

		public override int Execute(IEnumerable<IReadOnlyRow> selection)
		{
			return Execute();
		}

		public override int Execute(IPolygon area)
		{
			return Execute();
		}

		public override int Execute(IEnvelope boundingBox)
		{
			return Execute();
		}

		#region Non-public

		protected override ISpatialReference GetSpatialReference()
		{
			var featureClass = _table as IReadOnlyFeatureClass;
			return featureClass?.SpatialReference;
		}

		/// <summary>
		/// Reports a schema error.
		/// </summary>
		/// <param name="issueCode">The issue code.</param>
		/// <param name="format">A composite format string.</param>
		/// <param name="args">An <see cref="object"></see> array containing zero or more objects to format.</param>
		/// <returns></returns>
		[StringFormatMethod("format")]
		protected int ReportSchemaError([CanBeNull] IssueCode issueCode,
		                                [NotNull] string format,
		                                params object[] args)
		{
			Assert.ArgumentNotNullOrEmpty(format, nameof(format));

			return ReportError(string.Format(format, args),
			                   _table, issueCode, null);
		}

		/// <summary>
		/// Reports a schema error.
		/// </summary>
		/// <param name="issueCode">The issue code.</param>
		/// <param name="affectedComponent">The affected component.</param>
		/// <param name="format">A composite format string.</param>
		/// <param name="args">An <see cref="object"></see> array containing zero or more objects to format.</param>
		/// <returns></returns>
		[StringFormatMethod("format")]
		protected int ReportSchemaPropertyError(
			[CanBeNull] IssueCode issueCode,
			[NotNull] string affectedComponent,
			[NotNull] string format,
			params object[] args)
		{
			return ReportSchemaPropertyError(issueCode, affectedComponent, null, format,
			                                 args);
		}

		/// <summary>
		/// Reports a schema error.
		/// </summary>
		/// <param name="issueCode">The issue code.</param>
		/// <param name="affectedComponent">The affected component.</param>
		/// <param name="values">The values.</param>
		/// <param name="format">A composite format string.</param>
		/// <param name="args">An <see cref="object"></see> array containing zero or more objects to format.</param>
		/// <returns></returns>
		[StringFormatMethod("format")]
		protected int ReportSchemaPropertyError(
			[CanBeNull] IssueCode issueCode,
			[NotNull] string affectedComponent,
			[CanBeNull] IEnumerable<object> values,
			[NotNull] string format,
			params object[] args)
		{
			Assert.ArgumentNotNullOrEmpty(affectedComponent, nameof(affectedComponent));
			Assert.ArgumentNotNullOrEmpty(format, nameof(format));

			return ReportError(string.Format(format, args),
			                   _table, issueCode, affectedComponent,
			                   values);
		}

		[NotNull]
		protected static IEnumerable<IReadOnlyTable> GetTables([NotNull] IReadOnlyTable table,
		                                                       [CanBeNull]
		                                                       IReadOnlyTable referenceTable)
		{
			return referenceTable == null
				       ? new[] { table }
				       : new[] { table, referenceTable };
		}

		[NotNull]
		private static IEnumerable<IReadOnlyTable> GetTables(
			[NotNull] IReadOnlyTable table,
			[CanBeNull] IEnumerable<IReadOnlyTable> referenceTables)
		{
			var tables = new List<IReadOnlyTable> { table };

			if (referenceTables == null)
			{
				return tables;
			}

			foreach (IReadOnlyTable referenceTable in referenceTables)
			{
				if (referenceTable != null)
				{
					tables.Add(referenceTable);
				}
			}

			return tables;
		}

		#endregion
	}
}
