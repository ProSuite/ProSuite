using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Container.PolygonGrower;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Network;

namespace ProSuite.QA.Tests
{
	/*
	 * Beispiele:
	 * ----------
	 * 
	 * Punkt-Attributwert muss irgendeinem der Werte auf den Linien entsprechen (Beispiel Kanalnamen)
	 * 
	 * - LineFieldValuesConstraint: NoConstraint
	 * - PointFieldValuesConstraint: AllEqualAndMatchAnyLineValue
	 * 
	 * 
	 * Punkt-Attributwert muss dem häufigsten der Werte auf den Linien entsprechen (Beispiel Betriebsgruppe / Betriebsstatus).
	 * 
	 * - LineFieldValuesConstraint: NoConstraint
	 * - PointFieldValuesConstraint: AllEqualAndMatchMostFrequentLineValue
	 * 
	 * 
	 * Attributwerte auf den Linien müssen gleich sein, ausser ein bestimmter Punkttyp liegt auf der Verbindung (Beispiel Wechsel amtliche Nummer)
	 * 
	 * - LineFieldValuesConstraint: AllEqualOrValidPointExists
	 * - PointFieldValuesConstraint: NoConstraint
	 * - pointField = null
	 * 
	 * 
	 * TODO add option to ignore null values? Not sure if needed
	 * TODO add explicit tolerance? Or (optionally) get maximum tolerance from feature classes?
	 */

	[UsedImplicitly]
	[LinearNetworkTest]
	public class QaLineConnectionFieldValues : QaNetworkBase
	{
		private const string _expressionFieldName = "_FieldExpression";

		private readonly int _pointClassesMinIndex;
		private readonly int _pointClassesMaxIndex;
		private readonly IList<string> _lineFields;
		private readonly LineFieldValuesConstraint _lineFieldValuesConstraint;
		private readonly IList<IReadOnlyFeatureClass> _pointClasses;
		private readonly IList<string> _pointFields;
		private readonly PointFieldValuesConstraint _pointFieldValuesConstraint;
		private readonly IList<string> _allowedPointsExpressions;
		private readonly bool _usePointFields;

		private List<TableView> _allowedPointsTableViews;
		private List<TableView> _tableViews;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string ConstraintNotFulfilled_LineAndPoint =
				"ConstraintNotFulfilled.LineAndPoint";

			public const string ConstraintNotFulfilled_Point = "ConstraintNotFulfilled.Point";
			public const string ConstraintNotFulfilled_Line = "ConstraintNotFulfilled.Line";

			public Code() : base("LineConnectionFieldValues") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaLineConnectionFieldValues_0))]
		public QaLineConnectionFieldValues(
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineClass))] [NotNull]
			IReadOnlyFeatureClass lineClass,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineField))] [NotNull]
			string lineField,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineFieldValuesConstraint))]
			LineFieldValuesConstraint lineFieldValuesConstraint,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_pointClass))] [NotNull]
			IReadOnlyFeatureClass pointClass,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_pointField))] [CanBeNull]
			string pointField,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_pointFieldValuesConstraint))]
			PointFieldValuesConstraint pointFieldValuesConstraint)
			: this(new[] {lineClass}, new[] {lineField}, lineFieldValuesConstraint,
			       pointClass, pointField, pointFieldValuesConstraint) { }

		[Doc(nameof(DocStrings.QaLineConnectionFieldValues_1))]
		public QaLineConnectionFieldValues(
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineClasses))] [NotNull]
			IList<IReadOnlyFeatureClass>
				lineClasses,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineFields))] [NotNull]
			IList<string> lineFields,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineFieldValuesConstraint))]
			LineFieldValuesConstraint lineFieldValuesConstraint,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_pointClass))] [NotNull]
			IReadOnlyFeatureClass pointClass,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_pointField))] [CanBeNull]
			string pointField,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_pointFieldValuesConstraint))]
			PointFieldValuesConstraint pointFieldValuesConstraint)
			: this(
				lineClasses, lineFields, lineFieldValuesConstraint, pointClass, pointField,
				// ReSharper disable once IntroduceOptionalParameters.Global
				pointFieldValuesConstraint, null) { }

		[Doc(nameof(DocStrings.QaLineConnectionFieldValues_2))]
		public QaLineConnectionFieldValues(
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineClass))] [NotNull]
			IReadOnlyFeatureClass lineClass,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineField))] [NotNull]
			string lineField,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineFieldValuesConstraint))]
			LineFieldValuesConstraint lineFieldValuesConstraint,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_pointClass))] [NotNull]
			IReadOnlyFeatureClass pointClass)
			: this(new[] {lineClass}, new[] {lineField}, lineFieldValuesConstraint,
			       pointClass, null, PointFieldValuesConstraint.NoConstraint) { }

		[Doc(nameof(DocStrings.QaLineConnectionFieldValues_3))]
		public QaLineConnectionFieldValues(
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineClasses))] [NotNull]
			IList<IReadOnlyFeatureClass>
				lineClasses,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineFields))] [NotNull]
			IList<string> lineFields,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineFieldValuesConstraint))]
			LineFieldValuesConstraint lineFieldValuesConstraint,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_pointClass))] [NotNull]
			IReadOnlyFeatureClass pointClass,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_pointField))] [CanBeNull]
			string pointField,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_pointFieldValuesConstraint))]
			PointFieldValuesConstraint pointFieldValuesConstraint,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_allowedPointsExpression))]
			[CanBeNull]
			string
				allowedPointsExpression)
			: this(lineClasses, lineFields, lineFieldValuesConstraint, new[] {pointClass},
			       string.IsNullOrEmpty(pointField) ? null : new[] {pointField},
			       pointFieldValuesConstraint,
			       string.IsNullOrEmpty(allowedPointsExpression)
				       ? null
				       : new[] {allowedPointsExpression}) { }

		[Doc(nameof(DocStrings.QaLineConnectionFieldValues_4))]
		public QaLineConnectionFieldValues(
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineClasses))] [NotNull]
			IList<IReadOnlyFeatureClass> lineClasses,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineFields))] [NotNull]
			IList<string> lineFields,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_lineFieldValuesConstraint))]
			LineFieldValuesConstraint lineFieldValuesConstraint,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_pointClasses))] [NotNull]
			IList<IReadOnlyFeatureClass> pointClasses,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_pointFields))] [CanBeNull]
			IList<string> pointFields,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_pointFieldValuesConstraint))]
			PointFieldValuesConstraint pointFieldValuesConstraint,
			[Doc(nameof(DocStrings.QaLineConnectionFieldValues_allowedPointsExpressions))]
			[CanBeNull] IList<string> allowedPointsExpressions)
			: base(CastToTables(lineClasses, pointClasses),
			       tolerance: 0,
			       includeBorderNodes: false,
			       nonNetworkClassIndexList: null)
		{
			Assert.ArgumentNotNull(lineClasses, nameof(lineClasses));
			Assert.ArgumentNotNull(lineFields, nameof(lineFields));
			Assert.ArgumentNotNull(pointClasses, nameof(pointClasses));
			Assert.ArgumentCondition(
				lineFields.Count == 1 || lineFields.Count == lineClasses.Count,
				"lineFields must be either a single field that exists in all lineClasses, or one field per lineClass");

			Assert.ArgumentCondition(
				pointFields == null || pointFields.Count == 0 || pointFields.Count == 1 ||
				pointFields.Count == pointClasses.Count,
				"pointFields must be either null, a single field that exists in all pointClasses, or one field per pointClass");

			Assert.ArgumentCondition(
				allowedPointsExpressions == null || allowedPointsExpressions.Count == 0 ||
				allowedPointsExpressions.Count == 1 ||
				allowedPointsExpressions.Count == pointClasses.Count,
				"allowedPointsExpressions must be either null, a single expression that exists in all pointClasses, or one expression per pointClass");

			_lineFields = lineFields.ToList();
			_lineFieldValuesConstraint = lineFieldValuesConstraint;
			_pointClasses = pointClasses;
			_pointFields = pointFields != null && pointFields.Count > 0 ? pointFields : null;
			_pointFieldValuesConstraint = pointFieldValuesConstraint;
			_allowedPointsExpressions = allowedPointsExpressions != null &&
			                            allowedPointsExpressions.Count > 0
				                            ? allowedPointsExpressions
				                            : null;

			_pointClassesMinIndex = lineClasses.Count;
			_pointClassesMaxIndex = _pointClassesMinIndex + pointClasses.Count;
			_usePointFields = _pointFields != null;
		}

		[InternallyUsedTest]
		public QaLineConnectionFieldValues(QaLineConnectionFieldValuesDefinition definition)
			: this(definition.LineClasses.Cast<IReadOnlyFeatureClass>().ToList(),
				   definition.LineFields,
				   definition.LineFieldValuesConstraint,
				   definition.PointClasses.Cast<IReadOnlyFeatureClass>().ToList(),
				   definition.PointFields,
				   definition.PointFieldValuesConstraint,
				   definition.AllowedPointsExpressions)
		{ }

		[NotNull]
		private List<TableView> CreateTableViews()
		{
			var result = new List<TableView>(_pointClassesMaxIndex);

			for (var lineClassIndex = 0;
			     lineClassIndex < _pointClassesMinIndex;
			     lineClassIndex++)
			{
				string lineFieldName = _lineFields.Count == 1
					                       ? _lineFields[0]
					                       : _lineFields[lineClassIndex];
				IReadOnlyTable lineClass = InvolvedTables[lineClassIndex];

				result.Add(GetTableView(lineClass, lineFieldName,
				                        GetSqlCaseSensitivity(lineClassIndex)));
			}

			if (_usePointFields)
			{
				for (int tableIndex = _pointClassesMinIndex;
				     tableIndex < _pointClassesMaxIndex;
				     tableIndex++)
				{
					int pointClassIndex = tableIndex - _pointClassesMinIndex;
					string pointFieldName = _pointFields.Count == 1
						                        ? _pointFields[0]
						                        : _pointFields[pointClassIndex];

					TableView pointClassFilterHelper = GetTableView(
						_pointClasses[pointClassIndex],
						pointFieldName, GetSqlCaseSensitivity(tableIndex));
					result.Add(pointClassFilterHelper);
				}
			}

			return result;
		}

		protected override int CompleteTileCore(TileInfo args)
		{
			if (ConnectedElementsList == null)
			{
				return NoError;
			}

			int errorCount = base.CompleteTileCore(args);

			foreach (List<NetElement> connectedElements in Assert.NotNull(
				         ConnectedElementsList))
			{
				errorCount += CheckRows(connectedElements);
			}

			return errorCount;
		}

		[NotNull]
		private static TableView GetTableView([NotNull] IReadOnlyTable table,
		                                      [CanBeNull] string fieldName,
		                                      bool caseSensitive)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			if (fieldName != null)
			{
				// parse/remove/use case sensitivty hint
				bool? caseSensitivityOverride;
				fieldName = ExpressionUtils.ParseCaseSensitivityHint(fieldName,
					out caseSensitivityOverride);
				if (caseSensitivityOverride != null)
				{
					caseSensitive = caseSensitivityOverride.Value;
				}
			}

			const bool useAsConstraint = false;
			TableView result = TableViewFactory.Create(table, fieldName, useAsConstraint,
			                                           caseSensitive);

			result.AddColumn(QaConnections.StartsIn, typeof(bool));

			DataColumn fieldColumn = result.AddColumn(_expressionFieldName, typeof(object));

			fieldColumn.Expression = fieldName;

			return result;
		}

		private int CheckRows([NotNull] IList<NetElement> connectedElements)
		{
			if (_tableViews == null)
			{
				_tableViews = CreateTableViews();
			}

			foreach (TableView tableView in _tableViews)
			{
				tableView.ClearRows();
			}

			IList<NetElement> elementsToCheck;
			if (_allowedPointsExpressions != null)
			{
				if (_allowedPointsTableViews == null)
				{
					const bool useAsConstraint = true;
					var allowedPointsTableViews = new List<TableView>(_pointClasses.Count);

					for (var pointClassIndex = 0;
					     pointClassIndex < _pointClasses.Count;
					     pointClassIndex++)
					{
						string allowPointsExpression = _allowedPointsExpressions.Count == 1
							                               ? _allowedPointsExpressions[0]
							                               : _allowedPointsExpressions[
								                               pointClassIndex];
						TableView allowedPointsTableView = TableViewFactory.Create(
							_pointClasses[pointClassIndex],
							allowPointsExpression, useAsConstraint,
							GetSqlCaseSensitivity(pointClassIndex + _pointClassesMinIndex));
						allowedPointsTableViews.Add(allowedPointsTableView);
					}

					_allowedPointsTableViews = allowedPointsTableViews;
				}

				// if there are points at the connection, and they are all allowed junctions, 
				// then no further checks are needed
				if (HasPointsAndAllAreAllowed(connectedElements, out elementsToCheck,
				                              _allowedPointsTableViews))
				{
					return NoError;
				}
			}
			else
			{
				elementsToCheck = connectedElements;
			}

			// if constraint is based on most frequent line value, or if line values must be unique: *all* field values are needed
			bool getAllLineFieldValues =
				_pointFieldValuesConstraint ==
				PointFieldValuesConstraint.AllEqualAndMatchMostFrequentLineValue ||
				_lineFieldValuesConstraint == LineFieldValuesConstraint.UniqueOrValidPointExists;

			List<object> distinctPointFieldValues;
			List<object> lineFieldValues;
			int pointCount;
			List<IReadOnlyRow> connectedRows = GetConnectedRows(elementsToCheck,
			                                                    getAllLineFieldValues,
			                                                    out lineFieldValues,
			                                                    out distinctPointFieldValues,
			                                                    out pointCount);

			string pointMessage;
			bool pointConstraintValid = IsPointConstraintFulfilled(
				distinctPointFieldValues, lineFieldValues, out pointMessage);

			bool validPointExists = pointCount > 0 && pointConstraintValid;

			bool isLineFieldValuesListDistinct = ! getAllLineFieldValues;
			string lineMessage;
			bool lineConstraintValid = IsLineConstraintFulfilled(
				lineFieldValues, isLineFieldValuesListDistinct, validPointExists,
				out lineMessage);

			if (pointConstraintValid && lineConstraintValid)
			{
				return NoError;
			}

			IssueCode issueCode = GetIssueCode(pointConstraintValid, lineConstraintValid);

			string description = GetErrorDescription(pointConstraintValid, lineConstraintValid,
			                                         pointMessage, lineMessage);

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(connectedRows),
				elementsToCheck[0].NetPoint, issueCode, null);
		}

		private bool HasPointsAndAllAreAllowed(
			[NotNull] IEnumerable<NetElement> connectedElements,
			[NotNull] out IList<NetElement> elementsToCheck,
			[NotNull] IList<TableView> allowedPointsTableViews)
		{
			var hasPoints = false;
			var allPointsAllowed = true;

			elementsToCheck = new List<NetElement>();
			foreach (NetElement connectedElement in connectedElements)
			{
				TableIndexRow row = connectedElement.Row;
				int tableIndex = row.TableIndex;

				if (tableIndex < _pointClassesMinIndex)
				{
					elementsToCheck.Add(connectedElement);
					continue;
				}

				hasPoints = true; // found a point
				int pointClassIndex = row.TableIndex - _pointClassesMinIndex;
				TableView allowedPointsTableView = allowedPointsTableViews[pointClassIndex];
				allowedPointsTableView.ClearRows();
				allowedPointsTableView.Add(row.Row);

				if (! allowedPointsTableView.MatchesConstraint(row.Row))
				{
					elementsToCheck.Add(connectedElement);
					allPointsAllowed = false; // found an unallowed point
				}
			}

			// if there are any points, then they are all allowed by here.
			return hasPoints && allPointsAllowed;
		}

		[NotNull]
		private static string GetErrorDescription(bool pointConstraintValid,
		                                          bool lineConstraintValid,
		                                          [NotNull] string pointMessage,
		                                          [NotNull] string lineMessage)
		{
			var sb = new StringBuilder();

			if (! pointConstraintValid)
			{
				sb.Append(pointMessage);
			}

			if (! lineConstraintValid)
			{
				if (sb.Length > 0)
				{
					sb.Append("; ");
				}

				sb.Append(lineMessage);
			}

			return sb.ToString();
		}

		[CanBeNull]
		private static IssueCode GetIssueCode(bool pointConstraintValid,
		                                      bool lineConstraintValid)
		{
			if (! pointConstraintValid && ! lineConstraintValid)
			{
				return Codes[Code.ConstraintNotFulfilled_LineAndPoint];
			}

			if (! pointConstraintValid)
			{
				return Codes[Code.ConstraintNotFulfilled_Point];
			}

			if (! lineConstraintValid)
			{
				return Codes[Code.ConstraintNotFulfilled_Line];
			}

			return null;
		}

		private bool IsLineConstraintFulfilled(
			[NotNull] ICollection<object> lineFieldValues,
			bool isLineFieldValuesListDistinct,
			bool validPointExists,
			[NotNull] out string message)
		{
			message = string.Empty;

			ICollection<object> distinctValues;
			switch (_lineFieldValuesConstraint)
			{
				case LineFieldValuesConstraint.NoConstraint:
					return true;

				case LineFieldValuesConstraint.AllEqual:

					if (HasMultipleDistinctValues(lineFieldValues, isLineFieldValuesListDistinct,
					                              out distinctValues))
					{
						message = GetDistinctLineFieldValuesMessage(distinctValues);
						return false;
					}

					return true;

				case LineFieldValuesConstraint.AllEqualOrValidPointExists:
					if (validPointExists)
					{
						return true;
					}

					if (HasMultipleDistinctValues(lineFieldValues, isLineFieldValuesListDistinct,
					                              out distinctValues))
					{
						message = GetDistinctLineFieldValuesButNoValidPointMessage(distinctValues);
						return false;
					}

					return true;

				case LineFieldValuesConstraint.UniqueOrValidPointExists:
					if (validPointExists)
					{
						return true;
					}

					ICollection<object> nonUniqueValues;
					if (HasNonUniqueValues(lineFieldValues, isLineFieldValuesListDistinct,
					                       out nonUniqueValues))
					{
						message =
							GetNonUniqueLineFieldValuesButNoValidPointMessage(nonUniqueValues);
						return false;
					}

					return true;

				case LineFieldValuesConstraint.AtLeastTwoDistinctValuesIfValidPointExists:
					if (! validPointExists)
					{
						return true;
					}

					if (HasMultipleDistinctValues(lineFieldValues, isLineFieldValuesListDistinct,
					                              out distinctValues))
					{
						// there is more than one distinct line field value
						return true;
					}

					// there is a valid point, but not at least 2 distinct line field values
					message = GetNoDistinctLineFieldValuesMessage(distinctValues);
					return false;

				default:
					throw new NotSupportedException(
						$"line constraint not supported: {_lineFieldValuesConstraint}");
			}
		}

		private static bool HasNonUniqueValues([NotNull] ICollection<object> values,
		                                       bool isDistinctCollection,
		                                       [NotNull] out ICollection<object> nonUniqueValues)
		{
			Assert.False(isDistinctCollection, "collection must be non-distinct");

			nonUniqueValues = new List<object>();

			if (values.Count < 2)
			{
				return false;
			}

			var set = new HashSet<object>();
			foreach (object value in values)
			{
				bool added = set.Add(value);

				if (! added)
				{
					// value already exists
					nonUniqueValues.Add(value);
				}
			}

			return nonUniqueValues.Count > 0;
		}

		private static bool HasMultipleDistinctValues(
			[NotNull] ICollection<object> values,
			bool isDistinctCollection,
			[NotNull] out ICollection<object> distinctValues)
		{
			if (values.Count < 2)
			{
				// one value, or none at all
				distinctValues = values;
				return false;
			}

			if (isDistinctCollection)
			{
				// the list already contains distinct values, and there is
				// more than one value --> the list has distinct values
				distinctValues = values;
				return true;
			}

			// more than one value, but the list may contain duplicates
			var set = new HashSet<object>(values);

			distinctValues = set;
			return set.Count > 1;
		}

		private static string GetNonUniqueLineFieldValuesButNoValidPointMessage(
			[NotNull] IEnumerable<object> nonUniqueFieldValues)
		{
			return string.Format("Connected lines have duplicate field values ({0}), " +
			                     "and no valid point exists at connection",
			                     Concatenate(nonUniqueFieldValues));
		}

		[NotNull]
		private static string GetDistinctLineFieldValuesButNoValidPointMessage(
			[NotNull] IEnumerable<object> fieldValues)
		{
			return string.Format("Field values of connected lines are not equal ({0}), " +
			                     "and no valid point exists at connection",
			                     Concatenate(fieldValues));
		}

		[NotNull]
		private static string GetDistinctLineFieldValuesMessage(
			[NotNull] IEnumerable<object> fieldValues)
		{
			return string.Format("Field values of connected lines are not equal: {0}",
			                     Concatenate(fieldValues));
		}

		[NotNull]
		private static string GetDistinctPointFieldValuesMessage(
			[NotNull] IEnumerable<object> fieldValues)
		{
			return string.Format("Field values of connected points are not equal: {0}",
			                     Concatenate(fieldValues));
		}

		[NotNull]
		private static string GetNoDistinctLineFieldValuesMessage(
			[NotNull] ICollection<object> fieldValues)
		{
			// for LineFieldValuesConstraint.AtLeastTwoDistinctValuesIfValidPointExists:
			// there is a valid point, but not at least 2 distinct line field values
			if (fieldValues.Count == 0)
			{
				return "There are no distinct line values (no relevant connected lines)";
			}

			return string.Format(
				"All relevant connected lines have the same field value ({0})",
				Concatenate(fieldValues));
		}

		private bool IsPointConstraintFulfilled([NotNull] IList<object> pointFieldValues,
		                                        [NotNull] IList<object> lineFieldValues,
		                                        [NotNull] out string message)
		{
			message = string.Empty;

			switch (_pointFieldValuesConstraint)
			{
				case PointFieldValuesConstraint.NoConstraint:
					return true;

				case PointFieldValuesConstraint.AllEqualAndMatchAnyLineValue:
					if (pointFieldValues.Count > 1)
					{
						message = GetDistinctPointFieldValuesMessage(pointFieldValues);
						return false;
					}

					if (pointFieldValues.Count == 0)
					{
						// TODO what now? valid or not?
						return true;
					}

					if (lineFieldValues.Count == 0)
					{
						// unconnected point, valid
						return true;
					}

					if (lineFieldValues.Contains(pointFieldValues[0]))
					{
						return true;
					}

					message = string.Format(
						"Point field value '{0}' does not match any of the corresponding field values of connected lines ({1})",
						pointFieldValues[0], StringUtils.ConcatenateSorted(lineFieldValues, ", "));
					return false;

				case PointFieldValuesConstraint.AllEqualAndMatchMostFrequentLineValue:
					if (pointFieldValues.Count > 1)
					{
						message = GetDistinctPointFieldValuesMessage(pointFieldValues);
						return false;
					}

					if (pointFieldValues.Count == 0)
					{
						// TODO what now? valid or not?
						return true;
					}

					if (lineFieldValues.Count == 0)
					{
						// unconnected point, valid
						return true;
					}

					object mostFrequentLineFieldValue;
					bool hasSingleMostFrequentValue;
					if (MatchesMostFrequentValue(pointFieldValues[0], lineFieldValues,
					                             out mostFrequentLineFieldValue,
					                             out hasSingleMostFrequentValue))
					{
						return true;
					}

					if (hasSingleMostFrequentValue)
					{
						message =
							string.Format(
								"Point field Value '{0}' does not match any of the most frequent values of the connected lines",
								pointFieldValues[0]);
					}
					else
					{
						message =
							string.Format(
								"Point field value '{0}' does not match the most frequent value '{1}' of the connected lines",
								pointFieldValues[0], mostFrequentLineFieldValue);
					}

					return false;

				default:
					throw new NotSupportedException(
						$"point constraint not supported: {_pointFieldValuesConstraint}");
			}
		}

		private static bool MatchesMostFrequentValue(
			[CanBeNull] object value,
			[NotNull] IEnumerable<object> values,
			[CanBeNull] out object mostFrequentValue,
			out bool hasSingleMostFrequentValue)
		{
			var result = false;
			mostFrequentValue = null;
			var first = true;
			hasSingleMostFrequentValue = true;

			foreach (object frequentValue in CollectionUtils.GetMostFrequentValues(values))
			{
				if (! first)
				{
					hasSingleMostFrequentValue = false;
				}

				first = false;

				mostFrequentValue = frequentValue;

				if (Equals(frequentValue, value))
				{
					result = true;
				}
			}

			return result;
		}

		[NotNull]
		private List<IReadOnlyRow> GetConnectedRows(
			[NotNull] ICollection<NetElement> connectedElements,
			bool getAllLineFieldValues,
			[NotNull] out List<object> lineFieldValues,
			[NotNull] out List<object> distinctPointFieldValues,
			out int pointCount)
		{
			int connectedElementsCount = connectedElements.Count;
			var result = new List<IReadOnlyRow>(connectedElementsCount);

			lineFieldValues = new List<object>();
			distinctPointFieldValues = new List<object>();

			pointCount = 0;
			foreach (NetElement netElement in connectedElements)
			{
				TableIndexRow row = netElement.Row;
				result.Add(row.Row);

				int tableIndex = row.TableIndex;

				bool isPoint = tableIndex >= _pointClassesMinIndex;

				if (isPoint)
				{
					pointCount++;

					if (! _usePointFields)
					{
						continue;
					}
				}

				bool getDistinctValues = isPoint || ! getAllLineFieldValues;

				AddToValues(netElement, tableIndex, getDistinctValues,
				            isPoint
					            ? distinctPointFieldValues
					            : lineFieldValues);
			}

			return result;
		}

		private void AddToValues([NotNull] NetElement netElement,
		                         int tableIndex,
		                         bool addDistinctValues,
		                         [NotNull] ICollection<object> list)
		{
			object value = GetFieldExpressionValue(netElement, tableIndex);

			if (addDistinctValues && list.Contains(value))
			{
				return;
			}

			list.Add(value);
		}

		[NotNull]
		private static string Concatenate([NotNull] IEnumerable<object> fieldValues)
		{
			return StringUtils.ConcatenateSorted(fieldValues, ",");
		}

		[CanBeNull]
		private object GetFieldExpressionValue([NotNull] NetElement netElement,
		                                       int tableIndex)
		{
			IReadOnlyRow row = netElement.Row.Row;
			TableView tableView = _tableViews[tableIndex];

			DataRow helperRow = tableView.Add(row);
			Assert.NotNull(helperRow, "no row returned");

			var directedRow = netElement as DirectedRow;
			if (directedRow != null)
			{
				helperRow[QaConnections.StartsIn] = ! directedRow.IsBackward;
			}

			return helperRow[_expressionFieldName];
		}
	}
}
