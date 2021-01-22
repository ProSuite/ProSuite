using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Schema
{
	/// <summary>
	/// Base class for table (and feature class) schema tests
	/// </summary>
	[CLSCompliant(false)]
	public abstract class QaSchemaTestBase : NonContainerTest
	{
		private readonly ITable _table;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="QaSchemaTestBase"/> class.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <param name="referenceTable">The reference table.</param>
		protected QaSchemaTestBase([NotNull] ITable table,
		                           [CanBeNull] ITable referenceTable = null)
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
		protected QaSchemaTestBase([NotNull] ITable table,
		                           [CanBeNull] IEnumerable<ITable> referenceTables)
			: base(GetTables(table, referenceTables))
		{
			Assert.ArgumentNotNull(table, nameof(table));

			_table = table;
		}

		#endregion

		public override int Execute(IRow row)
		{
			return Execute();
		}

		public override int Execute(IEnumerable<IRow> selection)
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
			var featureClass = _table as IFeatureClass;
			return ((IGeoDataset) featureClass)?.SpatialReference;
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
		private static IEnumerable<ITable> GetTables([NotNull] ITable table,
		                                             [CanBeNull] ITable referenceTable)
		{
			return referenceTable == null
				       ? new[] {table}
				       : new[] {table, referenceTable};
		}

		[NotNull]
		private static IEnumerable<ITable> GetTables(
			[NotNull] ITable table,
			[CanBeNull] IEnumerable<ITable> referenceTables)
		{
			return referenceTables == null
				       ? new[] {table}
				       : Union(new[] {table}, new List<ITable>(referenceTables));
		}

		#endregion
	}
}
