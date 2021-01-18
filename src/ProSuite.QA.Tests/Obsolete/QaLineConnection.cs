using System;
using System.Collections.Generic;
using System.ComponentModel;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.SpatialRelations;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Check that only lines that follow certain rules are connected
	/// </summary>
	/// History: 15.12 GKAT Initial Coding
	[CLSCompliant(false)]
	[Obsolete("Use QaConnections")]
	public class QaLineConnection : QaSpatialRelationSelfBase
	{
		private List<QueryFilterHelper[]> _rules;

		/// <summary>
		/// Checks that touching lines of layers correspond to the rules
		/// </summary>
		/// <param name="featureClasses">involved layers</param>
		/// <param name="rules">list of rules. 
		/// In each string[]-Array must be exactly n rules,
		/// where n is the number of layers</param>
		/// <remarks>
		/// all layers must have the same spatial reference
		/// The rules are processed in ordered direction. If the involved features involve any rule, they fulfill the test
		/// </remarks>
		/// <example>
		/// Layers: A, B
		/// rules:
		/// {
		///   { A.ObjektArt IN (x,y), B.ObjektArt IN (u,v) }, // 
		///   { A.ObjektArt IN (z), null }, // no B-Feature must touch any A-Feature with ObjektArt == z
		///   { null, B.ObjektArt IN (t) }  // no A-Feature must touch any B-Feature with ObjektArt == t
		/// }
		/// </example>
		[Description(
			"Finds all touching lines in 'A' that do not correspond with the rules 'R'\n" +
			"Remark: the feature classes in 'A' must have the same spatial reference\n" +
			"The rules are processed in ordered direction. " +
			"If the involved features correspond to no rule, they are reported\n" +
			"Example: \n" +
			" Layers: A, B \n" +
			" rules: \n" +
			" { \n" +
			"   { A.ObjektArt IN (x,y), B.ObjektArt IN (u,v) }, // \n" +
			"   { A.ObjektArt IN (z), null }, // no B-Feature must touch any A-Feature with ObjektArt == z \n" +
			"   { null, B.ObjektArt IN (t) }  // no A-Feature must touch any B-Feature with ObjektArt == t \n" +
			" }"
		)]
		public QaLineConnection(
			[Description("A: involved line feature classes")]
			IList<IFeatureClass>
				featureClasses,
			[Description(
				"R: List of rules. " +
				"In each string[]-Array must be exactly n rules, " +
				"where n is the number of feature classes in 'A'")]
			IList<string[]> rules)
			: base(featureClasses, esriSpatialRelEnum.esriSpatialRelTouches)
		{
			Init(rules);
		}

		/// <summary>
		/// Checks that touching lines of table correspond to the rules
		/// </summary>
		/// <param name="featureClass">line feature class</param>
		/// <param name="rules">list of rules. </param>
		/// <remarks>
		/// The rules are processed in ordered direction. If the involved features involve any rule, they fulfill the test
		/// </remarks>
		/// <example>
		/// Layers: A, B
		/// rules:
		/// {
		///   A.ObjektArt IN (x,y), // Features with ObjektArt = (x or y) may touch Features with ObjecktArt = (x or y)
		///   A.ObjektArt IN (z), // Features with ObjektArt = (z) may touch Features with ObjecktArt = (z)
		/// }
		/// </example>
		[Description(
			"Finds all touching lines in 'A' that do not correspond with the rules 'R'\n" +
			"Remark: The rules are processed in ordered direction. " +
			"If the involved features correspond to no rule, they are reported\n" +
			"Example: \n" +
			" Layers: A, B \n" +
			" rules: \n" +
			" { \n" +
			"   A.ObjektArt IN (x,y), // Features with ObjektArt = (x or y) may touch Features with ObjecktArt = (x or y)\n" +
			"   A.ObjektArt IN (z), // Features with ObjektArt = (z) may touch Features with ObjecktArt = (z) \n" +
			" }"
		)]
		public QaLineConnection(
			[Description("A: involved line feature class")]
			IFeatureClass featureClass,
			[Description("list of rules")] IList<string> rules)
			: base(featureClass, esriSpatialRelEnum.esriSpatialRelTouches)
		{
			var tRules = new List<string[]>();
			foreach (string rule in rules)
			{
				tRules.Add(new[] {rule});
			}

			Init(tRules);
		}

		private void Init([NotNull] IEnumerable<string[]> rules)
		{
			int tableCount = InvolvedTables.Count;

			_rules = new List<QueryFilterHelper[]>();
			foreach (string[] rule in rules)
			{
				if (rule.Length != tableCount)
				{
					throw new ArgumentException(
						"Rules must have as many fields as this Test has layers");
				}

				var newRule = new QueryFilterHelper[tableCount];
				for (int tableIndex = 0; tableIndex < tableCount; tableIndex++)
				{
					if (rule[tableIndex] != null && rule[tableIndex].Trim() != "")
					{
						newRule[tableIndex] = new QueryFilterHelper(InvolvedTables[tableIndex],
						                                            rule[tableIndex],
						                                            GetSqlCaseSensitivity(
							                                            tableIndex));
					}
				}

				_rules.Add(newRule);
			}
		}

		protected override void ConfigureQueryFilter(int tableIndex,
		                                             IQueryFilter queryFilter)
		{
			base.ConfigureQueryFilter(tableIndex, queryFilter);
			ITable table = InvolvedTables[tableIndex];

			foreach (QueryFilterHelper[] rule in _rules)
			{
				if (rule[tableIndex] == null)
				{
					continue;
				}

				string subFields = rule[tableIndex].SubFields;

				if (string.IsNullOrEmpty(subFields))
				{
					continue;
				}

				foreach (string fieldName in ExpressionUtils.GetExpressionFieldNames(
					table, subFields))
				{
					queryFilter.AddField(fieldName); // .AddField checks for multiple entries !
				}
			}
		}

		protected override int FindErrors(IRow row1, int tableIndex1, IRow row2,
		                                  int tableIndex2)
		{
			int errorCount = 0;

			int iTbl0 = InvolvedTables.IndexOf(row1.Table);
			int iTbl1 = InvolvedTables.IndexOf(row2.Table);
			bool valid = false;

			foreach (QueryFilterHelper[] rule in _rules)
			{
				if (rule[iTbl0] == null || rule[iTbl1] == null)
				{
					continue;
				}

				if (rule[iTbl0].MatchesConstraint(row1) && rule[iTbl1].MatchesConstraint(row2))
				{
					valid = true;
					break;
				}
			}

			if (! valid)
			{
				var geom1 = (ITopologicalOperator2) ((IFeature) row1).ShapeCopy;
				var geom2 = (ITopologicalOperator2) ((IFeature) row2).ShapeCopy;

				//ESRI Bug (9.2 Service Pack 1) : Spatial references must be set as equal, even when equal FeatureClasses
				((IGeometry) geom2).SpatialReference = ((IGeometry) geom1).SpatialReference;
				IGeometry intersect = geom1.Intersect((IGeometry) geom2,
				                                      esriGeometryDimension.esriGeometry0Dimension);

				const string description = "Rows do not fulfill rules";
				ReportError(description,
				            ((IPointCollection) intersect).get_Point(0), row1, row2);
				errorCount++;
			}

			return errorCount;
		}
	}
}
