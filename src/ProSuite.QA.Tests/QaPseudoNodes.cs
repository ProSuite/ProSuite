using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.PolygonGrower;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Core;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Network;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Check if there are pseudonodes across several line layers
	/// </summary>
	[CLSCompliant(false)]
	[UsedImplicitly]
	[InternallyUsedTest]
	public class QaPseudoNodes : QaNetworkBase
	{
		private readonly IList<IList<string>> _ignoreFields;
		private readonly IList<IFeatureClass> _polylineClasses;
		private Dictionary<ITable, Dictionary<int, esriFieldType>> _compareFieldsPerTable;

		private Dictionary<int, ISpatialFilter> _filters;
		private Dictionary<int, QueryFilterHelper> _helpers;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string PseudoNode = "PseudoNode";

			public Code() : base("PseudoNodes") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaPseudoNodes_0))]
		public QaPseudoNodes(
			[Doc(nameof(DocStrings.QaPseudoNodes_polylineClasses))] [NotNull]
			IList<IFeatureClass>
				polylineClasses,
			[Doc(nameof(DocStrings.QaPseudoNodes_ignoreFields_0))] [NotNull]
			IList<IList<string>>
				ignoreFields,
			[Doc(nameof(DocStrings.QaPseudoNodes_validPseudoNodes))] [NotNull]
			IList<IFeatureClass>
				validPseudoNodes)
			: base(CastToTables(polylineClasses, validPseudoNodes), false,
			       GetPolycurveClassIndices(polylineClasses, validPseudoNodes))
		{
			AssertEqualCount(polylineClasses, ignoreFields);

			_ignoreFields = ignoreFields;
			_polylineClasses = polylineClasses;
		}

		[Doc(nameof(DocStrings.QaPseudoNodes_1))]
		public QaPseudoNodes(
			[Doc(nameof(DocStrings.QaPseudoNodes_polylineClass))] [NotNull]
			IFeatureClass polylineClass,
			[Doc(nameof(DocStrings.QaPseudoNodes_ignoreFields_1))] [NotNull]
			string[] ignoreFields,
			[Doc(nameof(DocStrings.QaPseudoNodes_validPseudoNode))] [NotNull]
			IFeatureClass validPseudoNode)
			: base(
				CastToTables(polylineClass, validPseudoNode), false,
				GetPolycurveClassIndices(new[] {polylineClass},
				                         new[] {validPseudoNode}))
		{
			_ignoreFields = new IList<string>[] {ignoreFields};
			_polylineClasses = null;
		}

		[Doc(nameof(DocStrings.QaPseudoNodes_2))]
		public QaPseudoNodes(
				[Doc(nameof(DocStrings.QaPseudoNodes_polylineClasses))] [NotNull]
				IList<IFeatureClass>
					polylineClasses,
				[Doc(nameof(DocStrings.QaPseudoNodes_ignoreFields_0))] [NotNull]
				IList<IList<string>>
					ignoreFields)
			// ReSharper disable once PossiblyMistakenUseOfParamsMethod
			: base(CastToTables(polylineClasses), false)
		{
			AssertEqualCount(polylineClasses, ignoreFields);

			_ignoreFields = ignoreFields;
			_polylineClasses = polylineClasses;
		}

		[Doc(nameof(DocStrings.QaPseudoNodes_3))]
		public QaPseudoNodes(
			[Doc(nameof(DocStrings.QaPseudoNodes_polylineClass))] [NotNull]
			IFeatureClass polylineClass,
			[Doc(nameof(DocStrings.QaPseudoNodes_ignoreFields_1))] [NotNull]
			string[] ignoreFields)
			: base(polylineClass, false)
		{
			_ignoreFields = new IList<string>[] {ignoreFields};
			_polylineClasses = null;
		}

		[Doc(nameof(DocStrings.QaPseudoNodes_IgnoreLoopEndpoints))]
		[TestParameter(false)]
		public bool IgnoreLoopEndpoints { get; set; }

		[NotNull]
		private static IList<int> GetPolycurveClassIndices(
			[NotNull] IList<IFeatureClass> networkEdgeClasses,
			[NotNull] IList<IFeatureClass> exceptionClasses)
		{
			var result = new List<int>();

			int exceptionClassCount = exceptionClasses.Count;

			for (int exceptionClassIndex = 0;
			     exceptionClassIndex < exceptionClassCount;
			     exceptionClassIndex++)
			{
				IFeatureClass exceptionClass = exceptionClasses[exceptionClassIndex];

				if (exceptionClass.ShapeType != esriGeometryType.esriGeometryPoint)
				{
					int involvedTableIndex = exceptionClassIndex +
					                         networkEdgeClasses.Count;

					result.Add(involvedTableIndex);
				}
			}

			return result;
		}

		private static void AssertEqualCount(
			[NotNull] ICollection<IFeatureClass> polylineClasses,
			[NotNull] ICollection<IList<string>> ignoreFields)
		{
			Assert.ArgumentNotNull(polylineClasses, nameof(polylineClasses));
			Assert.ArgumentNotNull(ignoreFields, nameof(ignoreFields));

			Assert.ArgumentCondition(ignoreFields.Count == polylineClasses.Count,
			                         "Number of polylineClasses ({0}) != number of ignoreField lists ({1})",
			                         polylineClasses.Count, ignoreFields.Count);
		}

		protected override int CompleteTileCore(TileInfo args)
		{
			int errorCount = base.CompleteTileCore(args);

			if (ConnectedElementsList == null)
			{
				return errorCount;
			}

			foreach (List<NetElement> connectedRows in ConnectedElementsList)
			{
				errorCount += CheckRows(connectedRows, IgnoreLoopEndpoints);
			}

			return errorCount;
		}

		private int CheckRows([NotNull] IList<NetElement> connectedRows,
		                      bool ignoreLoopEndpoints)
		{
			IRow lineFeature1;
			IRow lineFeature2;
			if (! IsPseudoNode(connectedRows, ignoreLoopEndpoints,
			                   out lineFeature1, out lineFeature2))
			{
				return NoError;
			}

			Assert.NotNull(lineFeature1, "lineFeature1");
			Assert.NotNull(lineFeature2, "lineFeature2");

			//compare fields
			Dictionary<int, esriFieldType> compareFields =
				GetFieldIndexesToCompare(lineFeature1.Table);

			foreach (KeyValuePair<int, esriFieldType> pair in compareFields)
			{
				int fieldIndex = pair.Key;

				if (pair.Value == esriFieldType.esriFieldTypeGeometry)
				{
					// Do not compare the geometries by default
					continue;
				}

				if (! Equals(lineFeature1.Value[fieldIndex],
				             lineFeature2.Value[fieldIndex]))
				{
					// one of the relevant fields has a different value --> pseudo node is allowed
					return NoError;
				}
			}

			// Check if point lies on NON network feature
			IPoint netPoint = connectedRows[0].NetPoint;

			foreach (int nonNetworkClassIndex in NonNetworkClassIndexList)
			{
				ISpatialFilter relatedFilter = Filters[nonNetworkClassIndex];
				relatedFilter.Geometry = netPoint;

				QueryFilterHelper relatedFilterHelper = Helpers[nonNetworkClassIndex];

				IEnumerable<IRow> searchResult = Search(InvolvedTables[nonNetworkClassIndex],
				                                        relatedFilter,
				                                        relatedFilterHelper);

				if (searchResult.Any())
				{
					// Point lies on NON network feature --> the pseudo node is allowed
					return NoError;
				}
			}

			return ReportError("Pseudo Node", netPoint,
			                   Codes[Code.PseudoNode],
			                   TestUtils.GetShapeFieldName(lineFeature1),
			                   lineFeature1, lineFeature2);
		}

		private static bool IsPseudoNode([NotNull] IEnumerable<NetElement> connectedRows,
		                                 bool ignoreLoopEndpoints,
		                                 [CanBeNull] out IRow lineFeature1,
		                                 [CanBeNull] out IRow lineFeature2)
		{
			lineFeature1 = null;
			lineFeature2 = null;

			var connectedLines = new TableIndexRow[2];
			int lineRowIndex = 0;
			foreach (NetElement elem in connectedRows)
			{
				if (elem is NetPoint)
				{
					// valid pseudo node
					return false;
				}

				if (lineRowIndex >= 2)
				{
					// more than 2 lines
					return false;
				}

				connectedLines[lineRowIndex] = elem.Row;
				lineRowIndex++;
			}

			if (lineRowIndex != 2)
			{
				return false;
			}

			var line1 = connectedLines[0];
			var line2 = connectedLines[1];
			if (line1.TableIndex != line2.TableIndex)
			{
				// from different line feature class parameter
				return false;
			}

			if (ignoreLoopEndpoints)
			{
				if (line1.Row.OID == line2.Row.OID &&
				    line1.Row.Table == line2.Row.Table)
				{
					// same row -> don't report loop node
					return false;
				}
			}

			lineFeature1 = line1.Row;
			lineFeature2 = line2.Row;

			return true;
		}

		private Dictionary<int, ISpatialFilter> Filters
		{
			get
			{
				if (_filters == null)
				{
					InitFilters();
				}

				return _filters;
			}
		}

		private Dictionary<int, QueryFilterHelper> Helpers
		{
			get
			{
				if (_helpers == null)
				{
					InitFilters();
				}

				return _helpers;
			}
		}

		/// <summary>
		/// create a filter that gets the lines crossing the current row,
		/// with the same attribute constraints as the table
		/// </summary>
		private void InitFilters()
		{
			_filters = new Dictionary<int, ISpatialFilter>();
			_helpers = new Dictionary<int, QueryFilterHelper>();

			bool caseSensitive = GetSqlCaseSensitivity();

			foreach (int tableIndex in NonNetworkClassIndexList)
			{
				ITable nonNetworkClass = InvolvedTables[tableIndex];
				ISpatialFilter filter = new SpatialFilterClass();

				var helper = new QueryFilterHelper(nonNetworkClass,
				                                   GetConstraint(tableIndex),
				                                   caseSensitive);

				ConfigureQueryFilter(tableIndex, filter);

				esriGeometryType geometryType =
					((IFeatureClass) nonNetworkClass).ShapeType;

				switch (geometryType)
				{
					case esriGeometryType.esriGeometryPolyline:
						filter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
						break;

					case esriGeometryType.esriGeometryPolygon:
						filter.SpatialRel = esriSpatialRelEnum.esriSpatialRelTouches;
						break;

					default:
						throw new NotImplementedException("Unhandled geometry type " +
						                                  geometryType);
				}

				_filters.Add(tableIndex, filter);
				_helpers.Add(tableIndex, helper);
			}
		}

		private Dictionary<int, esriFieldType> GetFieldIndexesToCompare(
			[NotNull] ITable table)
		{
			if (_compareFieldsPerTable == null)
			{
				_compareFieldsPerTable =
					new Dictionary<ITable, Dictionary<int, esriFieldType>>();
			}

			Dictionary<int, esriFieldType> result;
			if (! _compareFieldsPerTable.TryGetValue(table, out result))
			{
				result = new Dictionary<int, esriFieldType>();
				int featureClassIndex = 0;
				if (_polylineClasses != null)
				{
					featureClassIndex = _polylineClasses.IndexOf((IFeatureClass) table);
				}

				IList<string> ignoreFieldNames = _ignoreFields[featureClassIndex];

				IFields fields = table.Fields;
				int fieldCount = fields.FieldCount;

				for (int fieldIndex = 0; fieldIndex < fieldCount; fieldIndex++)
				{
					IField field = fields.get_Field(fieldIndex);

					esriFieldType fieldType = field.Type;

					// Default ignore fields 
					if (! field.Editable ||
					    fieldType == esriFieldType.esriFieldTypeGeometry)
					{
						continue;
					}

					// Configured ignore fields 
					bool ignoreField = IsFieldIgnored(field.Name, ignoreFieldNames);

					if (! ignoreField)
					{
						result.Add(fieldIndex, fieldType);
					}
				}

				_compareFieldsPerTable.Add(table, result);
			}

			return result;
		}

		private static bool IsFieldIgnored([NotNull] string fieldName,
		                                   [NotNull] IEnumerable<string> ignoreFieldNames)
		{
			foreach (string ignoreFieldName in ignoreFieldNames)
			{
				if (ignoreFieldName != null &&
				    string.Equals(fieldName, ignoreFieldName,
				                  StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}
	}
}
