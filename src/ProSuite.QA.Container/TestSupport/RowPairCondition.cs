using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.QA.Container.TestSupport
{
	public abstract class RowPairCondition
	{
		private readonly bool _isDirected;
		private readonly bool _undefinedConditionIsFulfilled;
		private readonly bool _caseSensitive;
		private readonly bool _conciseMessage;

		private readonly IDictionary<TableIndexPair, MultiTableView> _constraintViews =
			new Dictionary<TableIndexPair, MultiTableView>();

		/// <summary>
		/// Initializes a new instance of the <see cref="RowPairCondition"/> class.
		/// </summary>
		/// <param name="condition">The condition.</param>
		/// <param name="isDirected">if set to <c>true</c> the condition is directed,
		/// i.e. row1/row2 only maps to G1/G2 and not also to G2/G1.</param>
		/// <param name="undefinedConditionIsFulfilled">if set to <c>true</c> an undefined condition is always fullfilled. 
		/// Otherwise, an undefined condition is never fulfilled</param>
		/// <param name="caseSensitive">Indicates if the condition should be case-sensitive with regard to field values and literals</param>
		protected RowPairCondition([CanBeNull] string condition,
		                           bool isDirected,
		                           bool undefinedConditionIsFulfilled = false,
		                           bool caseSensitive = false)
			: this(condition, isDirected, undefinedConditionIsFulfilled,
			       "G1", "G2", caseSensitive) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="RowPairCondition"/> class.
		/// </summary>
		/// <param name="condition">The condition.</param>
		/// <param name="isDirected">if set to <c>true</c> the condition is directed,
		/// i.e. row1/row2 only maps to G1/G2 and not also to G2/G1.</param>
		/// <param name="undefinedConditionIsFulfilled">if set to <c>true</c> an undefined condition is always fullfilled.
		/// Otherwise, an undefined condition is never fulfilled</param>
		/// <param name="row1Alias">The alias for row1.</param>
		/// <param name="row2Alias">The alias for row2.</param>
		/// <param name="caseSensitive">Indicates if the condition should be case-sensitive with regard to field values and literals</param>
		/// <param name="conciseMessage">Indicates if the message should be produced in the concisest possible format</param>
		protected RowPairCondition([CanBeNull] string condition,
		                           bool isDirected,
		                           bool undefinedConditionIsFulfilled,
		                           [NotNull] string row1Alias,
		                           [NotNull] string row2Alias,
		                           bool caseSensitive,
		                           bool conciseMessage = false)
		{
			Assert.ArgumentNotNullOrEmpty(row1Alias, nameof(row1Alias));
			Assert.ArgumentNotNullOrEmpty(row2Alias, nameof(row2Alias));

			Condition = StringUtils.IsNotEmpty(condition)
				            ? condition
				            : null;

			_isDirected = isDirected;
			_undefinedConditionIsFulfilled = undefinedConditionIsFulfilled;
			Row1Alias = row1Alias;
			Row2Alias = row2Alias;
			_caseSensitive = caseSensitive;
			_conciseMessage = conciseMessage;
		}

		[CanBeNull]
		public string Condition { get; }

		[NotNull]
		public string Row1Alias { get; }

		[NotNull]
		public string Row2Alias { get; }

		public bool IsFulfilled(
			[NotNull] IReadOnlyRow row1, int tableIndex1,
			[NotNull] IReadOnlyRow row2, int tableIndex2,
			[CanBeNull] IDictionary<string, object> overridingFieldValues = null)
		{
			if (Condition == null)
			{
				return _undefinedConditionIsFulfilled;
			}

			const bool returnEmptyConditionMessage = true;
			return IsFulfilled(row1, tableIndex1, row2, tableIndex2,
			                   out string _,
			                   returnEmptyConditionMessage,
			                   out IColumnNames _,
			                   overridingFieldValues);
		}

		public bool IsFulfilled([NotNull] IReadOnlyRow row1, int tableIndex1,
		                        [NotNull] IReadOnlyRow row2, int tableIndex2,
		                        [NotNull] out string conditionMessage)
		{
			return IsFulfilled(row1, tableIndex1, row2, tableIndex2, out conditionMessage, null);
		}

		public bool IsFulfilled([NotNull] IReadOnlyRow row1, int tableIndex1,
		                        [NotNull] IReadOnlyRow row2, int tableIndex2,
		                        [NotNull] out string conditionMessage,
		                        [CanBeNull] IDictionary<string, object> overridingFieldValues)
		{
			return IsFulfilled(row1, tableIndex1, row2, tableIndex2,
			                   out conditionMessage, out IColumnNames _,
			                   overridingFieldValues);
		}

		[ContractAnnotation(
			"=>true, conditionMessage:canbenull,errorColumnNames:canbenull; " +
			"=>false, conditionMessage:notnull,errorColumnNames:notnull")]
		public bool IsFulfilled([NotNull] IReadOnlyRow row1, int tableIndex1,
		                        [NotNull] IReadOnlyRow row2, int tableIndex2,
		                        [NotNull] out string conditionMessage,
		                        [CanBeNull] out IColumnNames errorColumnNames,
		                        [CanBeNull] IDictionary<string, object> overridingFieldValues =
			                        null)
		{
			if (Condition == null)
			{
				conditionMessage = string.Empty;
				errorColumnNames = null;
				return _undefinedConditionIsFulfilled;
			}

			const bool returnEmptyConditionMessage = false;
			return IsFulfilled(row1, tableIndex1,
			                   row2, tableIndex2,
			                   out conditionMessage,
			                   returnEmptyConditionMessage,
			                   out errorColumnNames,
			                   overridingFieldValues);
		}

		protected virtual void AddUnboundColumns([NotNull] Action<string, Type> addColumn,
		                                         [NotNull] IList<IReadOnlyTable> tables) { }

		[ContractAnnotation(
			"=>true, conditionMessage:canbenull,errorColumnNames:canbenull; " +
			"=>false, conditionMessage:notnull,errorColumnNames:notnull")]
		private bool IsFulfilled(
			[NotNull] IReadOnlyRow row1, int tableIndex1,
			[NotNull] IReadOnlyRow row2, int tableIndex2,
			[NotNull] out string conditionMessage,
			bool returnEmptyConditionMessage,
			[CanBeNull] out IColumnNames errorColumnNames,
			[CanBeNull] IDictionary<string, object> overridingFieldValues = null)
		{
			Assert.ArgumentNotNull(row1, nameof(row1));
			Assert.ArgumentNotNull(row2, nameof(row2));

			if (Condition == null)
			{
				// no condition
				conditionMessage = string.Empty;
				errorColumnNames = null;
				return _undefinedConditionIsFulfilled;
			}

			var view = GetTableView(row1, tableIndex1, row2, tableIndex2);

			if (view.MatchesConstraint(overridingFieldValues, row1, row2))
			{
				// the condition is fulfilled
				conditionMessage = string.Empty;
				errorColumnNames = null;
				return true;
			}

			if (_isDirected)
			{
				// the condition is to be checked only for (row1,row2)
				conditionMessage = returnEmptyConditionMessage
					                   ? string.Empty
					                   : view.ToString(_conciseMessage, row1, row2);
				errorColumnNames = view;
				return false;
			}

			// the condition is not directed -> maybe it succeeds for (row2,row1)
			var invertedView = GetTableView(row2, tableIndex2, row1, tableIndex1);

			if (invertedView.MatchesConstraint(SwapRowAliases(overridingFieldValues),
			                                   row2, row1))
			{
				// the condition is fulfilled
				conditionMessage = string.Empty;
				errorColumnNames = null;
				return true;
			}

			conditionMessage = returnEmptyConditionMessage
				                   ? string.Empty
				                   : invertedView.ToString(_conciseMessage, row2, row1);
			errorColumnNames = invertedView;
			return false;
		}

		[CanBeNull]
		private IDictionary<string, object> SwapRowAliases(
			[CanBeNull] IDictionary<string, object> fieldValues)
		{
			return fieldValues?.ToDictionary(pair => SwapRowAliases(pair.Key),
			                                 pair => pair.Value,
			                                 StringComparer.OrdinalIgnoreCase);
		}

		[NotNull]
		private string SwapRowAliases([NotNull] string fieldName)
		{
			string row1Prefix = Row1Alias + ".";
			string row2Prefix = Row2Alias + ".";

			// if field name starts with an alias, replace it with the other alias
			if (fieldName.StartsWith(row1Prefix))
			{
				return row2Prefix + fieldName.Substring(row1Prefix.Length);
			}

			if (fieldName.StartsWith(row2Prefix))
			{
				return row1Prefix + fieldName.Substring(row2Prefix.Length);
			}

			// field name does not start with alias, return as is
			return fieldName;
		}

		[NotNull]
		private MultiTableView GetTableView([NotNull] IReadOnlyRow row1, int tableIndex1,
		                                    [NotNull] IReadOnlyRow row2, int tableIndex2)
		{
			string condition = Assert.NotNull(Condition, "condition is not defined");

			var tableIndexPair = new TableIndexPair(tableIndex1, tableIndex2);

			MultiTableView view;
			if (! _constraintViews.TryGetValue(tableIndexPair, out view))
			{
				view = CreateConstraintView(row1, row2, condition, _caseSensitive);

				_constraintViews.Add(tableIndexPair, view);
			}

			return view;
		}

		[NotNull]
		private MultiTableView CreateConstraintView([NotNull] IReadOnlyRow row1,
		                                            [NotNull] IReadOnlyRow row2,
		                                            [NotNull] string constraint,
		                                            bool caseSensitive)
		{
			var table1 = row1.Table;
			var table2 = row2.Table;

			var result = TableViewFactory.Create(
				new[] {table1, table2},
				new[] {Row1Alias, Row2Alias},
				constraint,
				caseSensitive,
				AddUnboundColumns);

			return result;
		}
	}
}
