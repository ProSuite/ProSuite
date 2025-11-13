using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[ProximityTest]
	public class QaMpVertexNotNearFaceDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef MultiPatchClass { get; }
		public IList<IFeatureClassSchemaDef> VertexClasses { get; }
		public double MinimumDistanceAbove { get; }
		public double MinimumDistanceBelow { get; }

		public enum OffsetMethod
		{
			Vertical,
			Perpendicular
		}

		private const bool _defaultVerifyWithinFeature = false;
		private const bool _defaultReportNonCoplanarity = false;
		private const bool _defaultIgnoreNonCoplanarFaces = false;
		private const double _defaultPlaneCoincidence = -1;
		private const OffsetMethod _defaultCheckMethod = OffsetMethod.Vertical;

		private double _minimumSlopeDegrees;
		private double _minimumSlopeTan2;

		[Doc(nameof(DocStrings.QaMpVertexNotNearFace_0))]
		public QaMpVertexNotNearFaceDefinition(
			[Doc(nameof(DocStrings.QaMpVertexNotNearFace_multiPatchClass))] [NotNull]
			IFeatureClassSchemaDef multiPatchClass,
			[Doc(nameof(DocStrings.QaMpVertexNotNearFace_vertexClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> vertexClasses,
			[Doc(nameof(DocStrings.QaMpVertexNotNearFace_minimumDistanceAbove))]
			double minimumDistanceAbove,
			[Doc(nameof(DocStrings.QaMpVertexNotNearFace_minimumDistanceBelow))]
			double minimumDistanceBelow)
			: base(
				CastToTables((IList<IFeatureClassSchemaDef>)Union(new[] { multiPatchClass }, vertexClasses)
					             .Cast<IFeatureClassSchemaDef>()))
		{
			MultiPatchClass = multiPatchClass;
			VertexClasses = vertexClasses;
			MinimumDistanceAbove = minimumDistanceAbove;
			MinimumDistanceBelow = minimumDistanceBelow;
		}

		[Doc(nameof(DocStrings.QaMpVertexNotNearFace_CoplanarityTolerance))]
		[TestParameter]
		public double CoplanarityTolerance { get; set; }

		[Doc(nameof(DocStrings.QaMpVertexNotNearFace_ReportNonCoplanarity))]
		[TestParameter(_defaultReportNonCoplanarity)]
		public bool ReportNonCoplanarity { get; set; }

		[Doc(nameof(DocStrings.QaMpVertexNotNearFace_IgnoreNonCoplanarFaces))]
		[TestParameter(_defaultIgnoreNonCoplanarFaces)]
		public bool IgnoreNonCoplanarFaces { get; set; }

		[Doc(nameof(DocStrings.QaMpVertexNotNearFace_VerifyWithinFeature))]
		[TestParameter(_defaultVerifyWithinFeature)]
		public bool VerifyWithinFeature { get; set; }

		[Doc(nameof(DocStrings.QaMpVertexNotNearFace_PointCoincidence))]
		[TestParameter]
		public double PointCoincidence { get; set; }

		[Doc(nameof(DocStrings.QaMpVertexNotNearFace_EdgeCoincidence))]
		[TestParameter]
		public double EdgeCoincidence { get; set; }

		[Doc(nameof(DocStrings.QaMpVertexNotNearFace_PlaneCoincidence))]
		[TestParameter(_defaultPlaneCoincidence)]
		public double PlaneCoincidence { get; set; }

		[Doc(nameof(DocStrings.QaMpVertexNotNearFace_MinimumSlopeDegrees))]
		[TestParameter]
		public double MinimumSlopeDegrees
		{
			get { return _minimumSlopeDegrees; }
			set
			{
				_minimumSlopeDegrees = value;
				double radians = MathUtils.ToRadians(value);
				double tan = Math.Tan(radians);
				_minimumSlopeTan2 = tan * tan;
			}
		}
	}
}
