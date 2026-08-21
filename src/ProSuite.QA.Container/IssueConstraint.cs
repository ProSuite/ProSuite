using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	/// <summary>
	/// A constraint expression evaluated on an issue (a <see cref="QaError"/>): on the
	/// issue's own attributes (issue code, description, affected component, reported
	/// values) and on all properties of the issue geometry that
	/// <see cref="GeometryConstraint"/> offers.
	/// </summary>
	public class IssueConstraint
	{
		[NotNull] private readonly DataView _constraintView;

		[NotNull] private readonly List<ColumnHandler<QaError, IssueCache>> _columnHandlers;

		public IssueConstraint([NotNull] string constraint)
		{
			Assert.ArgumentNotNullOrEmpty(constraint, nameof(constraint));

			Constraint = constraint;

			var dataTable = new DataTable("table") { CaseSensitive = false };

			_columnHandlers = ConstraintUtils.AddColumns(dataTable, constraint,
			                                             GetAvailableColumnHandlers());
			_constraintView = new DataView(dataTable) { RowFilter = constraint };
		}

		[NotNull]
		public string Constraint { get; }

		public bool IsFulfilled([NotNull] QaError qaError)
		{
			Assert.ArgumentNotNull(qaError, nameof(qaError));

			DataTable dataTable = _constraintView.Table;
			dataTable.Clear();

			DataRow row = dataTable.NewRow();

			var issueCache = new IssueCache(qaError);

			var index = 0;
			foreach (ColumnHandler<QaError, IssueCache> columnHandler in _columnHandlers)
			{
				row[index] = columnHandler.GetValue(qaError, issueCache);
				index++;
			}

			dataTable.Rows.Add(row);

			return _constraintView.Count == 1;
		}

		[NotNull]
		public string FormatValues([NotNull] QaError qaError,
		                           [NotNull] IFormatProvider formatProvider)
		{
			Assert.ArgumentNotNull(qaError, nameof(qaError));
			Assert.ArgumentNotNull(formatProvider, nameof(formatProvider));

			var sb = new StringBuilder();

			var issueCache = new IssueCache(qaError);

			foreach (ColumnHandler<QaError, IssueCache> columnHandler in
			         _columnHandlers.OrderBy(c => c.ColumnName))
			{
				if (sb.Length > 0)
				{
					sb.AppendFormat("; ");
				}

				sb.AppendFormat(formatProvider,
				                "{0}={1}",
				                columnHandler.ColumnName,
				                columnHandler.FormatValue(qaError, formatProvider, issueCache));
			}

			return sb.ToString();
		}

		/// <remarks>
		/// The columns used by an expression are determined by a case-insensitive substring
		/// search for the column name (see <see cref="ConstraintUtils"/>). Therefore no
		/// column name may be contained in any other column name - including the geometry
		/// column names of <see cref="GeometryConstraint"/>.
		/// </remarks>
		[NotNull]
		private static IEnumerable<ColumnHandler<QaError, IssueCache>>
			GetAvailableColumnHandlers()
		{
			// the issue's own attributes:
			yield return Get("$IssueCode", typeof(string), GetIssueCode);
			yield return Get("$Description", typeof(string), GetDescription);
			yield return Get("$AffectedComponent", typeof(string), GetAffectedComponent);
			yield return Get("$Value1", typeof(double), GetDoubleValue1);
			yield return Get("$Value2", typeof(double), GetDoubleValue2);
			yield return Get("$TextValue", typeof(string), GetTextValue);

			// the geometry properties, unchanged, evaluated on the issue geometry:
			foreach (ColumnHandler<IGeometry, GeometryConstraint.PropertyCache> handler in
			         GeometryConstraint.GetAvailableColumnHandlers())
			{
				yield return handler.Adapt<QaError, IssueCache>(
					qaError => qaError.Geometry,
					issueCache => issueCache.GeometryProperties);
			}
		}

		[NotNull]
		private static ColumnHandler<QaError, IssueCache> Get(
			[NotNull] string columnName,
			[NotNull] Type type,
			[NotNull] Func<QaError, IssueCache, object> valueFunction,
			[CanBeNull] string valueFormat = null)
		{
			return new ColumnHandler<QaError, IssueCache>(columnName, type, valueFunction,
			                                              valueFormat);
		}

		[NotNull]
		private static object GetIssueCode(QaError qaError, IssueCache issueCache)
		{
			return NullToDBNull(qaError.IssueCode?.ID);
		}

		[NotNull]
		private static object GetDescription(QaError qaError, IssueCache issueCache)
		{
			return NullToDBNull(qaError.Description);
		}

		[NotNull]
		private static object GetAffectedComponent(QaError qaError, IssueCache issueCache)
		{
			return NullToDBNull(qaError.AffectedComponent);
		}

		[NotNull]
		private static object GetDoubleValue1(QaError qaError, IssueCache issueCache)
		{
			return issueCache.DoubleValue1 ?? (object) DBNull.Value;
		}

		[NotNull]
		private static object GetDoubleValue2(QaError qaError, IssueCache issueCache)
		{
			return issueCache.DoubleValue2 ?? (object) DBNull.Value;
		}

		[NotNull]
		private static object GetTextValue(QaError qaError, IssueCache issueCache)
		{
			return NullToDBNull(issueCache.TextValue);
		}

		[NotNull]
		private static object NullToDBNull([CanBeNull] string value)
		{
			return value ?? (object) DBNull.Value;
		}

		/// <summary>
		/// The per-issue cache: the geometry properties and the extracted values are
		/// calculated at most once per issue, and only if the expression uses them.
		/// </summary>
		internal class IssueCache
		{
			[NotNull] private readonly QaError _qaError;

			[CanBeNull] private GeometryConstraint.PropertyCache _geometryProperties;

			private bool _valuesExtracted;
			private double? _doubleValue1;
			private double? _doubleValue2;
			private string _textValue;

			public IssueCache([NotNull] QaError qaError)
			{
				_qaError = qaError;
			}

			[NotNull]
			public GeometryConstraint.PropertyCache GeometryProperties =>
				_geometryProperties ??
				(_geometryProperties =
					 new GeometryConstraint.PropertyCache(_qaError.Geometry));

			public double? DoubleValue1
			{
				get
				{
					EnsureValues();
					return _doubleValue1;
				}
			}

			public double? DoubleValue2
			{
				get
				{
					EnsureValues();
					return _doubleValue2;
				}
			}

			[CanBeNull]
			public string TextValue
			{
				get
				{
					EnsureValues();
					return _textValue;
				}
			}

			private void EnsureValues()
			{
				if (_valuesExtracted)
				{
					return;
				}

				QaErrorUtils.GetValues(_qaError.Values,
				                       out _doubleValue1,
				                       out _doubleValue2,
				                       out _textValue);

				_valuesExtracted = true;
			}
		}
	}
}
