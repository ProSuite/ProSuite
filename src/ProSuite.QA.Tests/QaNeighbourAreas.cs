using System;
using System.Collections.Generic;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Core;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Check if there are any two touching areas inside a layer
	/// that have certain properties
	/// </summary>
	[UsedImplicitly]
	[TopologyTest]
	public class QaNeighbourAreas : ContainerTest
	{
		private readonly string _constraint;
		private readonly IFeatureClass _polygonClass;
		private readonly bool _allowPointIntersection;
		private readonly List<int> _compareFieldIndexes;
		private MultiTableView _compareHelper;
		private QueryFilterHelper _selectHelper;
		private ISpatialFilter _spatialFilter;
		private string _comparedFieldsString;

		[CanBeNull] private ContainsPostProcessor _ignoreAreaProcessor;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string PolygonsTouch_AllFieldsEqual = "PolygonsTouch.AllFieldsEqual";

			public const string PolygonsTouch_NoFieldsToCompare =
				"PolygonsTouch.NoFieldsToCompare";

			public const string PolygonsTouch_ConstraintNotFulfilled =
				"PolygonsTouch.ConstraintNotFulfilled";

			public Code() : base("NeighbourAreas") { }
		}

		#endregion

		#region constructors

		[Doc(nameof(DocStrings.QaNeighbourAreas_0))]
		public QaNeighbourAreas(
				[Doc(nameof(DocStrings.QaNeighbourAreas_polygonClass))] [NotNull]
				IFeatureClass polygonClass,
				[Doc(nameof(DocStrings.QaNeighbourAreas_constraint))] [CanBeNull]
				string constraint)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polygonClass, constraint, false) { }

		[Doc(nameof(DocStrings.QaNeighbourAreas_1))]
		public QaNeighbourAreas(
			[Doc(nameof(DocStrings.QaNeighbourAreas_polygonClass))] [NotNull]
			IFeatureClass polygonClass,
			[Doc(nameof(DocStrings.QaNeighbourAreas_constraint))] [CanBeNull]
			string constraint,
			[Doc(nameof(DocStrings.QaNeighbourAreas_allowPointIntersection))]
			bool allowPointIntersection)
			: base((ITable) polygonClass)
		{
			_constraint = StringUtils.IsNotEmpty(constraint)
				              ? constraint
				              : null;
			_allowPointIntersection = allowPointIntersection;
		}

		[Doc(nameof(DocStrings.QaNeighbourAreas_2))]
		public QaNeighbourAreas(
			[Doc(nameof(DocStrings.QaNeighbourAreas_polygonClass))] [NotNull]
			IFeatureClass polygonClass,
			[Doc(nameof(DocStrings.QaNeighbourAreas_allowPointIntersection))]
			bool allowPointIntersection)
			: this(polygonClass, allowPointIntersection, new List<string>(),
			       FieldListType.IgnoredFields) { }

		[Doc(nameof(DocStrings.QaNeighbourAreas_3))]
		public QaNeighbourAreas(
			[Doc(nameof(DocStrings.QaNeighbourAreas_polygonClass))] [NotNull]
			IFeatureClass polygonClass,
			[Doc(nameof(DocStrings.QaNeighbourAreas_allowPointIntersection))]
			bool allowPointIntersection,
			[Doc(nameof(DocStrings.QaNeighbourAreas_fieldsString))] [CanBeNull]
			string fieldsString,
			[Doc(nameof(DocStrings.QaNeighbourAreas_fieldListType))]
			FieldListType fieldListType)
			: this(polygonClass, allowPointIntersection,
			       TestUtils.GetTokens(fieldsString),
			       fieldListType) { }

		[Doc(nameof(DocStrings.QaNeighbourAreas_4))]
		public QaNeighbourAreas(
			[Doc(nameof(DocStrings.QaNeighbourAreas_polygonClass))] [NotNull]
			IFeatureClass polygonClass,
			[Doc(nameof(DocStrings.QaNeighbourAreas_allowPointIntersection))]
			bool allowPointIntersection,
			[Doc(nameof(DocStrings.QaNeighbourAreas_fields))] [NotNull]
			IEnumerable<string> fields,
			[Doc(nameof(DocStrings.QaNeighbourAreas_fieldListType))]
			FieldListType fieldListType)
			: base((ITable) polygonClass)
		{
			Assert.ArgumentNotNull(polygonClass, nameof(polygonClass));
			Assert.ArgumentNotNull(fields, nameof(fields));

			_polygonClass = polygonClass;
			_allowPointIntersection = allowPointIntersection;

			_compareFieldIndexes = new List<int>(
				GetCompareFieldIndexes(polygonClass, fields, fieldListType));
		}

		#endregion

		[TestParameter]
		[Doc(nameof(DocStrings.QaNeighbourAreas_IgnoreArea))]
		public IFeatureClass IgnoreArea
		{
			get { return _ignoreAreaProcessor?.FeatureClass; }
			set
			{
				_ignoreAreaProcessor?.Dispose();
				_ignoreAreaProcessor =
					value != null ? new ContainsPostProcessor(this, value) : null;
			}
		}

		protected override int ExecuteCore(IRow row, int tableIndex)
		{
			if (_spatialFilter == null)
			{
				InitFilter();
				_spatialFilter = Assert.NotNull(_spatialFilter, "filter is null");
			}

			// configure filter to find crossing "row"
			IGeometry shape = ((IFeature) row).Shape;
			_spatialFilter.Geometry = shape;

			// optimize query if tests runs "directed"
			_selectHelper.MinimumOID = IgnoreUndirected
				                           ? row.OID
				                           : -1;

			var errorCount = 0;

			ITable table = InvolvedTables[0];
			foreach (IRow touchingRow in Search(table, _spatialFilter, _selectHelper))
			{
				errorCount += CheckRows(row, shape, (IFeature) touchingRow);
			}

			return errorCount;
		}

		private int CheckRows([NotNull] IRow row,
		                      [NotNull] IGeometry shape,
		                      [NotNull] IFeature touchingFeature)
		{
			if (_compareHelper != null)
			{
				if (_compareHelper.MatchesConstraint(row, touchingFeature))
				{
					return NoError;
				}
			}
			else if (_compareFieldIndexes != null)
			{
				foreach (int fieldIndex in _compareFieldIndexes)
				{
					if (! Equals(row.Value[fieldIndex], touchingFeature.Value[fieldIndex]))
					{
						// different field value -> area boundary is justified
						return NoError;
					}
				}
			}

			IGeometry errorGeometry = GetErrorGeometry(shape, touchingFeature);

			if (errorGeometry == null)
			{
				return NoError;
			}

			IssueCode issueCode;
			return ReportError(GetErrorDescription(row, touchingFeature, out issueCode),
			                   errorGeometry,
			                   issueCode, null,
			                   row, touchingFeature);
		}

		[NotNull]
		private string GetErrorDescription([NotNull] IRow row, [NotNull] IRow touchingRow,
		                                   out IssueCode issueCode)
		{
			if (_compareHelper != null)
			{
				issueCode = Codes[Code.PolygonsTouch_ConstraintNotFulfilled];
				return string.Format("Constraint '{0}' is not fulfilled: {1}",
				                     _constraint,
				                     _compareHelper.ToString(row, touchingRow));
			}

			const string defaultMessage = "Polygons touch (no fields to compare)";

			if (_compareFieldIndexes != null)
			{
				if (_comparedFieldsString == null)
				{
					_comparedFieldsString = GetCompareFieldsString(_polygonClass,
					                                               _compareFieldIndexes);
				}

				if (_comparedFieldsString.Length == 0)
				{
					issueCode = Codes[Code.PolygonsTouch_NoFieldsToCompare];
					return defaultMessage;
				}

				issueCode = Codes[Code.PolygonsTouch_AllFieldsEqual];
				return string.Format(
					"Polygons touch and have equal values for all compared fields ({0})",
					_comparedFieldsString);
			}

			issueCode = Codes[Code.PolygonsTouch_NoFieldsToCompare];
			return defaultMessage;
		}

		[CanBeNull]
		private IGeometry GetErrorGeometry([NotNull] IGeometry shape,
		                                   [NotNull] IFeature touchingFeature)
		{
			var shapeTopoOp = (ITopologicalOperator) shape;

			IGeometry touchingShape = touchingFeature.Shape;

			// try to get the linear intersection
			IGeometry result = shapeTopoOp.Intersect(
				touchingShape,
				esriGeometryDimension.esriGeometry1Dimension);

			if (result.IsEmpty)
			{
				if (_allowPointIntersection)
				{
					return null;
				}

				// no linear intersection, try to get point intersection
				result = shapeTopoOp.Intersect(touchingShape,
				                               esriGeometryDimension.esriGeometry0Dimension);
			}

			return result;
		}

		/// <summary>
		/// create a filter that gets the lines crossing the current row,
		/// with the same attribute constraints as the table
		/// </summary>
		private void InitFilter()
		{
			IList<ISpatialFilter> spatialFilters;
			IList<QueryFilterHelper> filterHelpers;

			// there is one table and hence one filter (see constructor)
			// Create copy of this filter and use it for quering crossing lines
			CopyFilters(out spatialFilters, out filterHelpers);

			_spatialFilter = spatialFilters[0];
			_selectHelper = filterHelpers[0];

			_spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelTouches;

			if (_constraint != null)
			{
				_compareHelper = TableViewFactory.Create(
					new[] {InvolvedTables[0], InvolvedTables[0]},
					new[] {"L", "R"},
					_constraint,
					GetSqlCaseSensitivity());
			}
		}

		[NotNull]
		private static string GetCompareFieldsString([NotNull] IObjectClass objectClass,
		                                             [NotNull] IEnumerable<int> fieldIndexes)
		{
			var sb = new StringBuilder();

			IFields fields = objectClass.Fields;

			foreach (int fieldIndex in fieldIndexes)
			{
				IField field = fields.Field[fieldIndex];

				string token = GetFieldNameToken(field);

				if (sb.Length == 0)
				{
					sb.Append(token);
				}
				else
				{
					sb.AppendFormat(", {0}", token);
				}
			}

			return sb.ToString();
		}

		[NotNull]
		private static string GetFieldNameToken([NotNull] IField field)
		{
			string aliasName = field.AliasName;
			string name = field.Name;

			return string.Equals(name, aliasName, StringComparison.InvariantCultureIgnoreCase)
				       ? aliasName
				       : $"{aliasName} [{name}]";
		}

		[NotNull]
		private static IEnumerable<int> GetCompareFieldIndexes(
			[NotNull] IObjectClass objectClass,
			[NotNull] IEnumerable<string> fieldNames,
			FieldListType fieldListType)
		{
			switch (fieldListType)
			{
				case FieldListType.RelevantFields:
					return GetCompareFieldIndexes(objectClass, fieldNames);

				case FieldListType.IgnoredFields:
					return GetCompareFieldIndexesFromIgnoredFieldNames(objectClass, fieldNames);

				default:
					throw new ArgumentOutOfRangeException(
						string.Format("Unsupported field list type: {0}", fieldListType));
			}
		}

		[NotNull]
		private static IEnumerable<int> GetCompareFieldIndexesFromIgnoredFieldNames(
			[NotNull] IObjectClass objectClass,
			[NotNull] IEnumerable<string> ignoredFieldNames)
		{
			var ignoredFields = new SimpleSet<string>(
				ignoredFieldNames, StringComparer.InvariantCultureIgnoreCase);

			var fieldIndex = 0;
			foreach (IField field in DatasetUtils.GetFields(objectClass))
			{
				if (field.Editable &&
				    IsSupportedCompareFieldType(field.Type) &&
				    ! ignoredFields.Contains(field.Name))
				{
					yield return fieldIndex;
				}

				fieldIndex++;
			}
		}

		[NotNull]
		private static IEnumerable<int> GetCompareFieldIndexes(
			[NotNull] IObjectClass objectClass,
			[NotNull] IEnumerable<string> fieldNames)
		{
			IFields fields = objectClass.Fields;
			foreach (string fieldName in fieldNames)
			{
				int index = objectClass.FindField(fieldName);
				Assert.ArgumentCondition(index >= 0,
				                         "Field {0} not found in {1}", fieldName,
				                         DatasetUtils.GetName(objectClass));

				IField field = fields.Field[index];

				esriFieldType fieldType = field.Type;
				Assert.ArgumentCondition(IsSupportedCompareFieldType(fieldType),
				                         "Type of field {0} is not supported for comparison: {1}",
				                         fieldName,
				                         fieldType);

				yield return index;
			}
		}

		private static bool IsSupportedCompareFieldType(esriFieldType fieldType)
		{
			switch (fieldType)
			{
				case esriFieldType.esriFieldTypeSmallInteger:
				case esriFieldType.esriFieldTypeInteger:
				case esriFieldType.esriFieldTypeSingle:
				case esriFieldType.esriFieldTypeDouble:
				case esriFieldType.esriFieldTypeString:
				case esriFieldType.esriFieldTypeDate:
				case esriFieldType.esriFieldTypeGUID:
				case esriFieldType.esriFieldTypeXML:
					return true;

				case esriFieldType.esriFieldTypeGeometry:
				case esriFieldType.esriFieldTypeBlob:
				case esriFieldType.esriFieldTypeRaster:
				case esriFieldType.esriFieldTypeOID:
				case esriFieldType.esriFieldTypeGlobalID:
					return false;

				default:
					throw new ArgumentOutOfRangeException(nameof(fieldType));
			}
		}
	}
}