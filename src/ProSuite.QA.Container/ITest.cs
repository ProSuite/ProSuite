using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	[Obsolete("remove")]
	public class IRow
	{ }
	[Obsolete("remove")]
	public class IFeature
	{ }

	[Obsolete("remove")]
	public class ITable
	{ }

	[Obsolete("remove")]
	public class IFeatureClass
	{ }
	[Obsolete("remove")]
	public class IGeoDataset
	{ }

	public interface IReadOnlyTable
	{
		IFields Fields { get; }
		int FindField(string fieldName);
		bool HasOID { get; }
		string OIDFieldName { get; }
		string Name { get; }
		IReadOnlyRow GetRow(int oid);
		IEnumerable<IReadOnlyRow> EnumRows(IQueryFilter filter, bool recycle);
	}
	public interface IReadOnlyGeoDataset
	{ }

	public interface IReadOnlyFeatureClass : IReadOnlyTable, IReadOnlyGeoDataset
	{
		string ShapeFieldName { get; }
	}

	public interface IReadOnlyRow
	{
		bool HasOID { get; }
		int OID { get; }
		object get_Value(int Index);
		IReadOnlyTable Table { get; }
	}
	public interface IReadOnlyFeature : IReadOnlyRow
	{
		IGeometry Shape { get; }
		IGeometry ShapeCopy { get; }
		IEnvelope Extent { get; }
	}

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

	public interface IHasSearchDistance
	{
		double SearchDistance { get; }
	}

	public interface ITransformedValue
	{
		[NotNull]
		IList<IReadOnlyTable> InvolvedTables { get; }

		ISearchable DataContainer { get; set; }
	}

	public interface ITransformedTable
	{
		void SetKnownTransformedRows([CanBeNull] IEnumerable<IReadOnlyRow> knownRows);

		bool NoCaching { get; }
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
