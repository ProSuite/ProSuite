using System.Collections.Generic;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Core;
using System;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Check if there is always exactly one outgoing vertex
	/// </summary>
	[UsedImplicitly]
	[LinearNetworkTest]
	public class QaFlowLogicDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> PolylineClasses { get; set; }

		private IList<string> allowMultipleOutgoingLines;

		public IList<string> FlipExpressions { get; set; }
		public bool AllowMultipleOutgoingLines { get; set; }

		//public object PolylineClasses;

		//public IList<string> allowMultipleOutgoingLines { get; }

		private List<string> _flipExpressions;
		private readonly bool _allowMultipleOutgoingLines;
		public bool flipExpressions;
		private static ITableSchemaDef featureClass;

		[Doc(nameof(DocStrings.QaFlowLogic_0))]
		public QaFlowLogicDefinition(
			[Doc(nameof(DocStrings.QaFlowLogic_polylineClass))]
			IFeatureClassSchemaDef polylineClass)
			: this(new[] { polylineClass }) { }

		[Doc(nameof(DocStrings.QaFlowLogic_1))]
		public QaFlowLogicDefinition(
				[Doc(nameof(DocStrings.QaFlowLogic_polylineClasses))]
				IList<IFeatureClassSchemaDef> polylineClasses)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polylineClasses, null, false) { }

		[Doc(nameof(DocStrings.QaFlowLogic_2))]
		public QaFlowLogicDefinition(
				[Doc(nameof(DocStrings.QaFlowLogic_polylineClasses))]
				IList<IFeatureClassSchemaDef> polylineClasses,
				[Doc(nameof(DocStrings.QaFlowLogic_flipExpressions))]
				IList<string> flipExpressions)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polylineClasses, flipExpressions, false) { }

		[Doc(nameof(DocStrings.QaFlowLogic_2))]
		public QaFlowLogicDefinition(
			[Doc(nameof(DocStrings.QaFlowLogic_polylineClasses))]
			IList<IFeatureClassSchemaDef> polylineClasses,
			[Doc(nameof(DocStrings.QaFlowLogic_flipExpressions))]
			IList<string> flipExpressions,
			[Doc(nameof(DocStrings.QaFlowLogic_allowMultipleOutgoingLines))]
			bool allowMultipleOutgoingLines)
			:base(featureClass)

		{
			Assert.ArgumentNotNull(polylineClasses, nameof(polylineClasses));
			Assert.ArgumentCondition(flipExpressions == null ||
									 flipExpressions.Count <= 1 ||
									 flipExpressions.Count == polylineClasses.Count,
									 "The number of flip expressions must be either 0, 1 " +
									 "(-> same flip expression used for all feature classes) " +
									 "or else, equal to the number of feature classes)");

			if (flipExpressions != null && flipExpressions.Count > 0)
			{
				_flipExpressions = new List<string>();

				for (int tableIndex = 0; tableIndex < polylineClasses.Count; tableIndex++)
				{
					_flipExpressions.Add(flipExpressions.Count == 1
											 ? flipExpressions[0]
											 : flipExpressions[tableIndex]);
				}
			}

			//_allowMultipleOutgoingLines = allowMultipleOutgoingLines;
			PolylineClasses = polylineClasses;
			this.allowMultipleOutgoingLines = flipExpressions;
		}

		public IList<string> Clockwise { get; set; }

		private static object CastToTables(IEnumerable<IFeatureClassSchemaDef> polylineClasses)
		{
			throw new NotImplementedException();
		}
	}
}
