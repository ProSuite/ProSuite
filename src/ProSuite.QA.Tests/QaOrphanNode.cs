using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.PolygonGrower;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Network;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Check if there are orphan nodes by consulting several line layers
	/// </summary>
	[UsedImplicitly]
	[LinearNetworkTest]
	public class QaOrphanNode : QaNetworkBase
	{
		private readonly OrphanErrorType _errorType;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string OrphanNode = "OrphanNode";
			public const string MissingNode = "MissingNode";

			public Code() : base("OrphanNode") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaOrphanNode_0))]
		public QaOrphanNode(
				[Doc(nameof(DocStrings.QaOrphanNode_pointClasses))]
				IList<IReadOnlyFeatureClass> pointClasses,
				[Doc(nameof(DocStrings.QaOrphanNode_polylineClasses))]
				IList<IReadOnlyFeatureClass> polylineClasses)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(pointClasses, polylineClasses, OrphanErrorType.Both) { }

		[Doc(nameof(DocStrings.QaOrphanNode_1))]
		public QaOrphanNode(
				[Doc(nameof(DocStrings.QaOrphanNode_pointClass))]
				IReadOnlyFeatureClass pointClass,
				[Doc(nameof(DocStrings.QaOrphanNode_polylineClass))]
				IReadOnlyFeatureClass polylineClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(pointClass, polylineClass, OrphanErrorType.Both) { }

		[Doc(nameof(DocStrings.QaOrphanNode_2))]
		public QaOrphanNode(
			[Doc(nameof(DocStrings.QaOrphanNode_pointClasses))]
			IList<IReadOnlyFeatureClass> pointClasses,
			[Doc(nameof(DocStrings.QaOrphanNode_polylineClasses))]
			IList<IReadOnlyFeatureClass> polylineClasses,
			[Doc(nameof(DocStrings.QaOrphanNode_errorType))]
			OrphanErrorType errorType)
			: base(CastToTables(pointClasses, polylineClasses), false)
		{
			_errorType = errorType;
		}

		[Doc(nameof(DocStrings.QaOrphanNode_3))]
		public QaOrphanNode(
			[Doc(nameof(DocStrings.QaOrphanNode_pointClass))]
			IReadOnlyFeatureClass pointClass,
			[Doc(nameof(DocStrings.QaOrphanNode_polylineClass))]
			IReadOnlyFeatureClass polylineClass,
			[Doc(nameof(DocStrings.QaOrphanNode_errorType))]
			OrphanErrorType errorType)
			: base(new[] {pointClass, polylineClass}, false)
		{
			_errorType = errorType;
		}

		protected override int CompleteTileCore(TileInfo args)
		{
			int errorCount = base.CompleteTileCore(args);
			if (ConnectedElementsList == null)
			{
				return errorCount;
			}

			errorCount += ConnectedElementsList.Sum(connectedRows => CheckRows(connectedRows));

			return errorCount;
		}

		private int CheckRows([NotNull] IList<NetElement> connectedRows)
		{
			Assert.ArgumentNotNull(connectedRows, nameof(connectedRows));

			switch (_errorType)
			{
				case OrphanErrorType.Both:
					int errorCount = CheckForEndPointWithoutPoint(connectedRows);
					errorCount += CheckForOrphanedPoint(connectedRows);

					return errorCount;

				case OrphanErrorType.EndPointWithoutPoint:
					return CheckForEndPointWithoutPoint(connectedRows);

				case OrphanErrorType.OrphanedPoint:
					return CheckForOrphanedPoint(connectedRows);

				default:
					throw new InvalidOperationException(
						string.Format("Invalid error type: {0}", _errorType));
			}
		}

		/// <summary>
		/// Determines whether [is orphan node] [the specified connected elements].
		/// </summary>
		/// <param name="connectedElements">The connected elements.</param>
		/// <returns>Returns  true if node is not connected to a line, returns false if node is connected to at least one line</returns>
		private int CheckForOrphanedPoint([NotNull] IList<NetElement> connectedElements)
		{
			Assert.ArgumentNotNull(connectedElements, nameof(connectedElements));

			if (connectedElements.Count != 1 || ! (connectedElements[0] is NetPoint))
			{
				return NoError;
			}

			var p = (NetPoint) connectedElements[0];
			const string description = "Orphan Node";

			IReadOnlyRow errorRow = p.Row.Row;

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(errorRow), p.NetPoint,
				Codes[Code.OrphanNode], TestUtils.GetShapeFieldName(errorRow));
		}

		/// <summary>
		/// Determines whether a line is connected to a point. 
		/// </summary>
		/// <param name="connectedElements">The connected elements.</param>
		/// <returns>Returns true if line has no connected points, returns true if line is connected to at least one point</returns>
		private int CheckForEndPointWithoutPoint(
			[NotNull] IList<NetElement> connectedElements)
		{
			Assert.ArgumentNotNull(connectedElements, nameof(connectedElements));

			foreach (NetElement elem in connectedElements)
			{
				if (elem is NetPoint)
				{
					return 0;
				}
			}

			const string description = "Missing Node";
			return ReportError(
				description,
				InvolvedRowUtils.GetInvolvedRows(connectedElements.Select(e => e.Row.Row)),
				connectedElements[0].NetPoint, Codes[Code.MissingNode], null);
		}
	}
}
