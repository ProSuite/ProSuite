using ProSuite.Commons.Geom;

namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	public interface IReshapeAlongOptions
	{
		bool AdjustMode { get; set; }
		double AdjustTolerance { get; set; }
		bool AdjustExcludeReshapableCurves { get; set; }
		bool ShowAdjustToleranceBuffer { get; set; }

		bool BufferTarget { get; set; }
		double TargetBufferDistance { get; set; }
		bool EnforceMinimumBufferSegmentLength { get; set; }
		double MinimumBufferSegmentLength { get; set; }

		bool ExcludeReshapeLinesOutsideTolerance { get; set; }
		double ExcludeReshapeLinesTolerance { get; set; }
		bool ShowExcludeReshapeLinesToleranceBuffer { get; set; }
		bool ExcludeReshapeLinesOutsideSource { get; set; }
		bool ExcludeReshapeLinesResultingInOverlaps { get; set; }

		bool ShowTargetFillSymbol { get; set; }
		bool PreviewDifferenceAreas { get; set; }

		bool ClipLinesOnVisibleExtent { get; set; }
		bool RecalculateLinesOnExtentChange { get; set; }
		bool HideReshapeLinesBeyondMaxScale { get; set; }
		double MaxReshapeLineDisplayScaleDenominator { get; set; }

		TargetFeatureSelection TargetSelectionType { get; set; }

		bool InsertVerticesOnTargets { get; set; }
		bool UseMinimalTolerance { get; set; }

		bool MultipleSourcesTreatIndividually { get; set; }
		bool MultipleSourcesTreatAsUnion { get; set; }

		bool AdjustModeTrySourceLineExtension { get; set; }
		double AdjustModeMaxSourceLineProlongationFactor { get; set; }
		double MaximumInteractiveAdjustTolerance { get; set; }

		ChangeAlongZSource ZSource { get; set; }

		bool DontShowDialog { get; set; }
		bool AutoApplyFormChanges { get; set; }

		IFlexibleSettingProvider<ChangeAlongZSource> GetZSourceOptionProvider();

		IReshapeAlongOptions Clone();

		void RevertToDefaults();

		string GetLocalOverridesMessage();
	}
}
