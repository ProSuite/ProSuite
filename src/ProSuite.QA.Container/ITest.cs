using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public interface IInvolvesTables
	{
		[NotNull]
		IList<IReadOnlyTable> InvolvedTables { get; }

		/// <summary>
		/// limits the data to execute corresponding to condition
		/// </summary>
		/// <param name="tableIndex"></param>
		/// <param name="condition"></param>
		void SetConstraint(int tableIndex, [CanBeNull] string condition);

		void SetSqlCaseSensitivity(int tableIndex, bool useCaseSensitiveQaSql);
	}

	public interface ITest : IInvolvesTables
	{
		/// <summary>
		/// thrown before a test on a row is performed
		/// </summary>
		event EventHandler<RowEventArgs> TestingRow;

		/// <summary>
		/// thrown if test detects a mistake
		/// </summary>
		event EventHandler<QaErrorEventArgs> QaError;

		/// <summary>
		/// Executes test over entire table
		/// </summary>
		/// <returns></returns>
		int Execute();

		/// <summary>
		/// executes test for objects within or cutting boundingBox
		/// </summary>
		/// <param name="boundingBox"></param>
		/// <returns></returns>
		int Execute([NotNull] IEnvelope boundingBox);

		/// <summary>
		/// executes test for objects within or cutting area
		/// </summary>
		/// <param name="area"></param>
		/// <returns></returns>
		int Execute([NotNull] IPolygon area);

		/// <summary>
		/// executes test for objects within selection
		/// </summary>
		/// <param name="selection"></param>
		/// <returns></returns>
		int Execute([NotNull] IEnumerable<IReadOnlyRow> selection);

		/// <summary>
		/// executes test for specified row
		/// </summary>
		/// <param name="row"></param>
		/// <returns></returns>
		int Execute([NotNull] IReadOnlyRow row);

		/// <summary>
		/// limits the execute area to area
		/// </summary>
		/// <param name="area"></param>
		void SetAreaOfInterest([CanBeNull] IPolygon area);
	}

	public interface IFilterTest
	{
		[CanBeNull]
		IReadOnlyList<IIssueFilter> IssueFilters { get; }

		[CanBeNull]
		IReadOnlyList<IRowFilter> GetRowFilters(int tableIndex);
	}

	public interface IFilterEditTest : IFilterTest
	{
		void SetIssueFilters([CanBeNull] string expression, IList<IIssueFilter> issueFilters);

		void SetRowFilters(int tableIndex, [CanBeNull] string expression,
		                   [CanBeNull] IReadOnlyList<IRowFilter> rowFilters);
	}

	public interface ITableTransformer : IInvolvesTables
	{
		object GetTransformed();

		string TransformerName { get; set; }
	}

	public interface ITableTransformer<out T> : ITableTransformer
	{
		new T GetTransformed();
	}

	public interface ITableTransformerFieldSettings : ITableTransformer
	{
		/// <summary>
		/// Whether all field names in the output table should be fully qualified using the
		/// SourceTableName.FieldName convention. This only applies to transformers with several
		/// input tables and might not be supported by all transformers.
		/// </summary>
		bool FullyQualifyFieldNames { get; set; }
	}

	public interface IHasSearchDistance
	{
		double SearchDistance { get; }
	}

	// This is used to manage the caching of the transformed output features produced by implementors.
	// Rename to CacheableTransformedTable?
	public interface ITransformedTable
	{
		void SetKnownTransformedRows([CanBeNull] IEnumerable<IReadOnlyRow> knownRows);

		// TODO: What are the rules and restrictions if a transformer wants to use caching?
		bool NoCaching { get; }
		bool IgnoreOverlappingCachedRows { get; }
	}

	public interface INamedFilter : IInvolvesTables
	{
		string Name { get; set; }
	}

	public interface IRowFilter : INamedFilter
	{
		bool VerifyExecute(IReadOnlyRow row);
	}

	public interface IIssueFilter : INamedFilter
	{
		bool Check(QaErrorEventArgs args);
	}
}
