using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public interface IInvolvesTables
	{
		[NotNull]
		IList<ITable> InvolvedTables { get; }
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
		int Execute([NotNull] IEnumerable<IRow> selection);

		/// <summary>
		/// executes test for specified row
		/// </summary>
		/// <param name="row"></param>
		/// <returns></returns>
		int Execute([NotNull] IRow row);

		/// <summary>
		/// limits the execute area to area
		/// </summary>
		/// <param name="area"></param>
		void SetAreaOfInterest([CanBeNull] IPolygon area);
	}

	public interface IProcessorTest
	{
		[CanBeNull]
		IReadOnlyList<IPostProcessor> PostProcessors { get; }

		[CanBeNull]
		IReadOnlyList<IPreProcessor> GetPreProcessors(int tableIndex);
	}

	public interface IEditProcessorTest : IProcessorTest
	{
		void AddPostProcessor(IPostProcessor proc);

		void SetPreProcessors(int tableIndex,
		                      [CanBeNull] IReadOnlyList<IPreProcessor> preProcessors);
	}


	public interface IPreProcessor : IInvolvesTables
	{
		bool VerifyExecute(IRow row);
	}

	public interface ITableTransformer : IInvolvesTables
	{
		ITable GetTransformed();
	}

	public interface IDerivedTable
	{
		[NotNull]
		IList<ITable> InvolvedTables { get; }

		ISearchable DataContainer { get; set; }
	}

	public interface IPostProcessor : IInvolvesTables
	{
		void PostProcessError(QaErrorEventArgs args);
	}
}
