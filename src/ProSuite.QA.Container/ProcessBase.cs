using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.Geometry;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core.ParameterTypes;

namespace ProSuite.QA.Container
{
	/// <summary>
	/// Base class for tests
	/// </summary>
	public abstract class ProcessBase
	{
		private class TableProps
		{
			public string Constraint { get; set; }
			public bool UseCaseSensitiveSQL { get; set; }
			public bool QueriedOnly { get; set; }

			/// <summary>
			/// Currently un-used (replaced with combined filter-transformer)
			/// </summary>
			internal string RowFiltersExpression { get; set; }

			/// <summary>
			/// Currently un-used (replaced with filter-transformers)
			/// </summary>
			internal IReadOnlyList<IRowFilter> RowFilters { get; set; }
		}

		[NotNull]
		protected static IList<IReadOnlyTable> CastToTables(
			params IList<IReadOnlyFeatureClass>[] featureClasses)
		{
			int totalCount = featureClasses.Sum(list => list.Count);

			var union = new List<IReadOnlyTable>(totalCount);

			foreach (IList<IReadOnlyFeatureClass> list in featureClasses)
			{
				foreach (IReadOnlyFeatureClass featureClass in list)
				{
					Assert.NotNull(featureClass, "list entry is null");

					union.Add(featureClass);
				}
			}

			return union;
		}

		protected const AngleUnit DefaultAngleUnit = AngleUnit.Radiant;
		private IPolygon _areaOfInterest;

		private esriUnits _lengthUnit = esriUnits.esriUnknownUnits;
		private string _unitString;
		private readonly List<TableProps> _tableProps;
		private List<string> _customQueryFilterExpressions;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="TestBase"/> class.
		/// </summary>
		/// <param name="tables">The tables.</param>
		protected ProcessBase([NotNull] IEnumerable<IReadOnlyTable> tables)
		{
			Assert.ArgumentNotNull(tables, nameof(tables));

			InvolvedTables = new List<IReadOnlyTable>(tables);

			// _constraints and _useCaseSensitiveSQL must have the same number of elements as _involvedTables
			_tableProps = InvolvedTables.Select(t => new TableProps()).ToList();
		}

		#endregion

		public AngleUnit AngleUnit { get; set; } = DefaultAngleUnit;

		public esriUnits LinearUnits
		{
			get { return _lengthUnit; }
			set
			{
				_lengthUnit = value;
				_unitString = FormatUtils.GetUnitString(_lengthUnit);
			}
		}

		public double ReferenceScale { get; set; } = 1.0;

		/// <summary>
		/// Gets the List of the tables involved in the test
		/// </summary>
		public IList<IReadOnlyTable> InvolvedTables { get; }

		protected int AddInvolvedTable(IReadOnlyTable table, string constraint,
		                               bool sqlCaseSensitivity,
		                               bool queriedOnly = false)
		{
			InvolvedTables.Add(table);
			_tableProps.Add(new TableProps
			                {
				                Constraint = constraint,
				                UseCaseSensitiveSQL = sqlCaseSensitivity,
				                QueriedOnly = queriedOnly
			                });
			AddInvolvedTableCore(table, constraint, sqlCaseSensitivity);
			return InvolvedTables.Count - 1;
		}

		protected virtual void AddInvolvedTableCore(
			IReadOnlyTable table, string constraint, bool sqlCaseSensitivity) { }

		public void SetConstraint(int tableIndex, string constraint)
		{
			_tableProps[tableIndex].Constraint = constraint;
			SetConstraintCore(InvolvedTables[tableIndex], tableIndex, constraint);
		}

		public void SetSqlCaseSensitivity(int tableIndex, bool useCaseSensitiveQaSql)
		{
			_tableProps[tableIndex].UseCaseSensitiveSQL = useCaseSensitiveQaSql;
		}

		public void SetRowFilters(int tableIndex, [CanBeNull] string rowFiltersExpression,
		                          [CanBeNull] IReadOnlyList<IRowFilter> rowFilters)
		{
			_tableProps[tableIndex].RowFiltersExpression = rowFiltersExpression;
			_tableProps[tableIndex].RowFilters = rowFilters;
			SetRowFiltersCore(tableIndex, rowFiltersExpression, rowFilters);
		}

		protected void CopyFilters([NotNull] out IList<IFeatureClassFilter> spatialFilters,
		                           [NotNull] out IList<QueryFilterHelper> filterHelpers)
		{
			int tableCount = InvolvedTables.Count;

			spatialFilters = new IFeatureClassFilter[tableCount];
			filterHelpers = new QueryFilterHelper[tableCount];

			for (var tableIndex = 0; tableIndex < tableCount; tableIndex++)
			{
				IReadOnlyTable table = InvolvedTables[tableIndex];

				filterHelpers[tableIndex] = new QueryFilterHelper(table,
					GetConstraint(tableIndex),
					GetSqlCaseSensitivity(tableIndex));
				spatialFilters[tableIndex] = new AoFeatureClassFilter();

				ConfigureQueryFilter(tableIndex, spatialFilters[tableIndex]);
			}
		}

		protected void AddCustomQueryFilterExpression([CanBeNull] string expression)
		{
			if (! string.IsNullOrWhiteSpace(expression))
			{
				_customQueryFilterExpressions = _customQueryFilterExpressions ?? new List<string>();
				_customQueryFilterExpressions.Add(
					expression.Replace(".", " ").Replace(",", " ").Replace(":", " "));
			}
		}
		/// <summary>
		/// Adapts IQueryFilter so that it conforms to the needs of the test
		/// </summary>
		protected virtual void ConfigureQueryFilter(int tableIndex,
		                                            [NotNull] ITableFilter filter)
		{
			Assert.ArgumentNotNull(filter, nameof(filter));

			IReadOnlyTable table = InvolvedTables[tableIndex];
			string constraint = GetConstraint(tableIndex);

			// NOTE: Contrary to the documentation the field is not added if queryFilter.SubFields == "*" which is the default!
			string subFields =
				GdbQueryUtils.AppendToFieldList(filter.SubFields, table.OIDFieldName);

			var featureClass = table as IReadOnlyFeatureClass;

			// add shape field
			if (featureClass != null)
			{
				subFields = GdbQueryUtils.AppendToFieldList(subFields, featureClass.ShapeFieldName);
			}

			// add subtype field
			var subtypes = table as ISubtypes;
			if (subtypes != null)
			{
				if (subtypes.HasSubtype)
				{
					subFields =
						GdbQueryUtils.AppendToFieldList(subFields, subtypes.SubtypeFieldName);
				}
			}

			// add where clause fields
			if (constraint != null)
			{
				foreach (
					string fieldName in
					ExpressionUtils.GetExpressionFieldNames(table, constraint))
				{
					subFields = GdbQueryUtils.AppendToFieldList(subFields, fieldName);
				}
			}

			if (_customQueryFilterExpressions?.Count > 0)
			{
				foreach (string filterExpression in _customQueryFilterExpressions)
				{
					foreach (
						string fieldName in
						ExpressionUtils.GetExpressionFieldNames(table, filterExpression))
					{
						subFields = GdbQueryUtils.AppendToFieldList(subFields, fieldName);
					}

				}
			}

			filter.SubFields = subFields;
			filter.WhereClause = constraint;
		}

		protected virtual void SetConstraintCore(IReadOnlyTable table, int tableIndex,
		                                         string constraint) { }

		protected virtual void SetRowFiltersCore(
			int tableIndex, [CanBeNull] string rowFiltersExpression,
			[CanBeNull] IReadOnlyList<IRowFilter> rowFilters) { }

		public void SetAreaOfInterest(IPolygon areaOfInterest)
		{
			if (areaOfInterest == null)
			{
				_areaOfInterest = null;
				return;
			}

			GeometryUtils.EnsureSpatialReference(areaOfInterest, GetSpatialReference(),
			                                     out _areaOfInterest);

			((ISpatialIndex) _areaOfInterest).AllowIndexing = true;
		}

		/// <summary>
		/// Limits the area where the test will be performed
		/// if area is null, no spatial precondition exists
		/// Remark: This should be a logic constraint of the test and not
		/// a limitation to perform a test only for a part.
		/// Use Execute(Extent) if you want only part of the data tested
		/// </summary>
		[CanBeNull]
		protected internal IPolygon AreaOfInterest => _areaOfInterest;

		protected string NumberFormat { get; set; } = "N2";

		[NotNull]
		protected static IList<IReadOnlyTable> Union([NotNull] IList<IReadOnlyTable> tables0,
		                                             [NotNull] IList<IReadOnlyTable> tables1)
		{
			var union = new List<IReadOnlyTable>(tables0.Count + tables1.Count);

			union.AddRange(tables0);
			union.AddRange(tables1);

			return union;
		}

		[NotNull]
		protected static IList<IReadOnlyFeatureClass> Union(
			[NotNull] IList<IReadOnlyFeatureClass> featureClasses0,
			[NotNull] IList<IReadOnlyFeatureClass> featureClasses1)
		{
			Assert.ArgumentNotNull(featureClasses0, nameof(featureClasses0));
			Assert.ArgumentNotNull(featureClasses1, nameof(featureClasses1));

			var union =
				new List<IReadOnlyFeatureClass>(featureClasses0.Count + featureClasses1.Count);

			union.AddRange(featureClasses0);
			union.AddRange(featureClasses1);

			return union;
		}

		[NotNull]
		public static IList<IReadOnlyTable> CastToTables(
			params IReadOnlyFeatureClass[] featureClasses)
		{
			return CastToTables((IEnumerable<IReadOnlyFeatureClass>) featureClasses);
		}

		[NotNull]
		public static IList<IReadOnlyTable> CastToTables(
			[NotNull] IEnumerable<IReadOnlyFeatureClass> featureClasses)
		{
			Assert.ArgumentNotNull(featureClasses, nameof(featureClasses));

			return featureClasses.Cast<IReadOnlyTable>().ToList();
		}

		[CanBeNull]
		protected abstract ISpatialReference GetSpatialReference();

		[NotNull]
		protected string FormatAngle(double radians, string format)
		{
			return FormatUtils.FormatAngle(format, radians, AngleUnit);
		}

		[NotNull]
		protected string FormatLength(double length, ISpatialReference sr)
		{
			double f = FormatUtils.GetLengthUnitFactor(sr, _lengthUnit, ReferenceScale);

			return FormatLength(f * length, NumberFormat);
		}

		protected string FormatArea(double area, ISpatialReference sr)
		{
			double f = FormatUtils.GetLengthUnitFactor(sr, _lengthUnit, ReferenceScale);

			return FormatArea(f * f * area, NumberFormat);
		}

		[NotNull]
		protected string FormatLengthComparison(double value0, string compare,
		                                        double value1,
		                                        ISpatialReference sr)
		{
			string result = FormatLengthComparison(value0, compare, value1, sr,
			                                       "{0} {1} {2}");
			return result;
		}

		[NotNull]
		protected string FormatLengthComparison(double value0, string compare,
		                                        double value1,
		                                        ISpatialReference sr,
		                                        string expressionFormat)
		{
			esriUnits lengthUnit = _lengthUnit;
			double referenceScale = ReferenceScale;
			string numberFormat = NumberFormat;
			double f = FormatUtils.GetLengthUnitFactor(sr, lengthUnit, referenceScale);

			double v0 = f * value0;
			double v1 = f * value1;

			string compareFormat = FormatUtils.CompareFormat(v0, compare, v1, numberFormat);
			string result = string.Format(expressionFormat,
			                              FormatLength(v0, compareFormat),
			                              compare,
			                              FormatLength(v1, compareFormat));

			return result;
		}

		[NotNull]
		protected string FormatAreaComparison(double value0, string compare, double value1,
		                                      ISpatialReference sr)
		{
			double f = FormatUtils.GetLengthUnitFactor(sr, _lengthUnit, ReferenceScale);
			double v0 = f * f * value0;
			double v1 = f * f * value1;

			string format = FormatUtils.CompareFormat(v0, compare, v1, NumberFormat);

			return string.Format("{0} {1} {2}",
			                     FormatArea(v0, format), compare,
			                     FormatArea(v1, format));
		}

		/// <summary>
		/// Ensures that the string expression "{0} {1} {2}" with v0, compare, v1
		/// is a correct string expression without rounding errors
		/// </summary>
		/// <param name="v0"></param>
		/// <param name="compare">a comparison operator</param>
		/// <param name="v1"></param>
		/// <param name="initFormat">initial numerical format for v0 and v1</param>
		/// <returns>"string.Format({0} {1} {2}, v0, compare, v1)" </returns>
		[NotNull]
		protected string FormatComparison(double v0, string compare, double v1,
		                                  string initFormat)
		{
			return FormatComparison(v0, compare, v1, initFormat, "{0} {1} {2}");
		}

		/// <summary>
		/// Ensures that the string expression "{0} {1} {2}" with v0, compare, v1
		/// is a correct string expression without rounding errors and
		/// returns string.Format('expressionString', v0, compare, v1)
		/// </summary>
		/// <param name="v0"></param>
		/// <param name="compare"></param>
		/// <param name="v1"></param>
		/// <param name="initFormat">initial numerical format for v0 and v1</param>
		/// <param name="expressionString"></param>
		/// <returns>string.Format('expressionString', v0, compare, v1)</returns>
		[NotNull]
		protected string FormatComparison(double v0, string compare, double v1,
		                                  string initFormat,
		                                  string expressionString)
		{
			if (initFormat == null)
			{
				initFormat = NumberFormat;
			}

			return FormatUtils.FormatComparison(initFormat, v0, v1, compare,
			                                    expressionString);
		}

		[NotNull]
		private string FormatArea(double area, string format)
		{
			string v = FormatUtils.GetValueString(area, format);

			return ! string.IsNullOrEmpty(_unitString)
				       ? string.Format("{0} {1}2", v, _unitString)
				       : v;
		}

		[NotNull]
		private string FormatLength(double value, string format)
		{
			string s = string.Format("{0} {1}", FormatUtils.GetValueString(value, format),
			                         _unitString);
			return s;
		}

		protected void AssertValidInvolvedTableIndex(int tableIndex)
		{
			Assert.ArgumentCondition(
				tableIndex >= 0 && tableIndex < InvolvedTables.Count,
				"Index {0} out of range", tableIndex);
		}

		/// <summary>
		/// Gets the Constraint (WhereClause) defined for table (involved in the test)
		/// </summary>
		/// <param name="table"></param>
		/// <returns></returns>
		/// <remarks>A table may be involved more than once for a test (in different roles). In 
		/// this case, the constraint of the <b>first</b> occurrence of the table in the list of involved tables
		/// is returned. This method should therefore only be used if it is known that the 
		/// table occurs only once in the list (e.g. if a test can only use one table)</remarks>
		[CanBeNull]
		protected string GetConstraint([NotNull] IReadOnlyTable table)
		{
			int tableIndex = InvolvedTables.IndexOf(table);

			return _tableProps[tableIndex].Constraint;
		}

		[CanBeNull]
		public string GetConstraint(int tableIndex)
		{
			return _tableProps[tableIndex].Constraint;
		}

		public bool GetSqlCaseSensitivity(int tableIndex)
		{
			return _tableProps[tableIndex].UseCaseSensitiveSQL;
		}

		public bool GetQueriedOnly(int tableIndex)
		{
			return _tableProps[tableIndex].QueriedOnly;
		}

		public bool GetSqlCaseSensitivity([NotNull] IReadOnlyTable table)
		{
			int tableIndex = InvolvedTables.IndexOf(table);

			return _tableProps[tableIndex].UseCaseSensitiveSQL;
		}

		public bool GetSqlCaseSensitivity()
		{
			return _tableProps.Any(value => value.UseCaseSensitiveSQL);
		}

		public bool GetSqlCaseSensitivity([NotNull] params int[] tableIndexes)
		{
			return GetSqlCaseSensitivity((IEnumerable<int>) tableIndexes);
		}

		public bool GetSqlCaseSensitivity([NotNull] params IReadOnlyTable[] tables)
		{
			return GetSqlCaseSensitivity((IEnumerable<IReadOnlyTable>) tables);
		}

		public bool GetSqlCaseSensitivity([NotNull] IEnumerable<IReadOnlyTable> tables)
		{
			Assert.ArgumentNotNull(tables, nameof(tables));

			foreach (IReadOnlyTable table in tables)
			{
				int tableIndex = InvolvedTables.IndexOf(table);

				if (_tableProps[tableIndex].UseCaseSensitiveSQL)
				{
					return true;
				}
			}

			return false;
		}

		public bool GetSqlCaseSensitivity([NotNull] IEnumerable<int> tableIndexes)
		{
			Assert.ArgumentNotNull(tableIndexes, nameof(tableIndexes));

			foreach (int tableIndex in tableIndexes)
			{
				if (_tableProps[tableIndex].UseCaseSensitiveSQL)
				{
					return true;
				}
			}

			return false;
		}
	}
}
