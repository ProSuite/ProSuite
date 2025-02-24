using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Internal.Geometry;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.ChangeAlong
{
	public class ChangeAlongCurves
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly List<CutSubcurve> _reshapeSubcurves;

		public ChangeAlongCurves([NotNull] IEnumerable<CutSubcurve> subcurves,
		                         ReshapeAlongCurveUsability curveUsability)
		{
			CurveUsability = curveUsability;
			_reshapeSubcurves = new List<CutSubcurve>(subcurves);
		}

		public ReshapeAlongCurveUsability CurveUsability { get; private set; }

		public IList<CutSubcurve> PreSelectedSubcurves { get; } = new List<CutSubcurve>();

		public void Update([NotNull] ChangeAlongCurves newState)
		{
			CurveUsability = newState.CurveUsability;

			_reshapeSubcurves.Clear();
			_reshapeSubcurves.AddRange(newState.ReshapeCutSubcurves);

			TargetFeatures = newState.TargetFeatures;
		}

		public IReadOnlyCollection<CutSubcurve> ReshapeCutSubcurves =>
			_reshapeSubcurves.AsReadOnly();

		public bool HasSelectableCurves =>
			CurveUsability == ReshapeAlongCurveUsability.CanReshape ||
			(_reshapeSubcurves?.Any() ?? false);

		[CanBeNull]
		public IList<Feature> TargetFeatures { get; set; }

		/// <summary>
		/// Pre-selects the (yellow) subcurves matched by the predicate so that they can be used
		/// in combination with other pre-selected subcurves.
		/// </summary>
		/// <param name="useSubCurvePredicate"></param>
		public void PreSelectCurves([CanBeNull] Predicate<CutSubcurve> useSubCurvePredicate)
		{
			foreach (CutSubcurve cutSubcurve in _reshapeSubcurves)
			{
				PreSelect(cutSubcurve, useSubCurvePredicate, PreSelectedSubcurves);
			}
		}

		/// <summary>
		/// Returns all subcurves that either fulfil the specified predicate or, if
		/// <see cref="includeAllPreSelectedCandidates"/> is true, are contained in the list of
		/// pre-selected subcurves.
		/// </summary>
		/// <param name="subCurvePredicate"></param>
		/// <param name="includeAllPreSelectedCandidates"></param>
		/// <returns></returns>
		[NotNull]
		public List<CutSubcurve> GetSelectedReshapeCurves(
			[CanBeNull] Predicate<CutSubcurve> subCurvePredicate,
			bool includeAllPreSelectedCandidates)
		{
			var result = new List<CutSubcurve>();

			foreach (CutSubcurve cutSubcurve in _reshapeSubcurves)
			{
				if (includeAllPreSelectedCandidates &&
				    PreSelectedSubcurves.Contains(cutSubcurve) &&
				    cutSubcurve.IsReshapeMemberCandidate)
				{
					result.Add(cutSubcurve);
				}
				else if (subCurvePredicate == null || subCurvePredicate(cutSubcurve))
				{
					result.Add(cutSubcurve);
				}
			}

			return result;
		}

		public void LogTargetSelection()
		{
			string selectedTargetsMsg;
			var message = string.Empty;

			if (TargetFeatures == null || TargetFeatures.Count == 0)
			{
				selectedTargetsMsg = message;
			}
			else
			{
				string featuresDescription =
					StringUtils.Concatenate(
						TargetFeatures,
						target =>
							$"{DatasetUtils.GetAliasName(target.GetTable())} {target.GetObjectID()}",
						", ");

				if (TargetFeatures.Count == 1)
				{
					message = $"Selected target feature: {featuresDescription}";
				}
				else
				{
					message =
						$"{TargetFeatures.Count} selected target features: {featuresDescription}";
				}

				selectedTargetsMsg = message;
			}

			if (! string.IsNullOrEmpty(selectedTargetsMsg))
			{
				_msg.Info(selectedTargetsMsg);
			}
		}

		/// <summary>
		/// Adds/removes the cutSubcurve from the preSelectedCurveList if the predicate
		/// is fulfilled.
		/// </summary>
		/// <param name="cutSubcurve"></param>
		/// <param name="selectionPredicate"></param>
		/// <param name="preSelectedCurveList"></param>
		private static void PreSelect(
			[NotNull] CutSubcurve cutSubcurve,
			[CanBeNull] Predicate<CutSubcurve> selectionPredicate,
			[NotNull] ICollection<CutSubcurve> preSelectedCurveList)
		{
			if (selectionPredicate != null && ! selectionPredicate(cutSubcurve))
			{
				return;
			}

			if (cutSubcurve.CanReshape)
			{
				// green lines
				return;
			}

			if (! cutSubcurve.IsReshapeMemberCandidate)
			{
				// red lines
				return;
			}

			if (preSelectedCurveList.Contains(cutSubcurve))
			{
				preSelectedCurveList.Remove(cutSubcurve);
			}
			else
			{
				preSelectedCurveList.Add(cutSubcurve);
			}
		}

		public void ApplyZsToReshapeCurves([CanBeNull] IZSettingsModel zSettingsModel,
		                                   IList<Feature> sourceFeatures)
		{
			if (zSettingsModel == null)
			{
				return;
			}

			if (! sourceFeatures.Any(f => f.GetTable().GetDefinition().HasZ()))
			{
				return;
			}

			foreach (CutSubcurve reshapeSubcurve in _reshapeSubcurves)
			{
				GdbObjectReference? sourceRef = reshapeSubcurve.Source;

				if (sourceRef != null)
				{
					Feature feature =
						sourceFeatures.FirstOrDefault(f => sourceRef.Value.References(f));

					if (feature != null && feature.GetTable().GetDefinition().HasZ() == false)
					{
						continue;
					}
				}

				Polyline reshapePath = reshapeSubcurve.Path;

				if (! reshapePath.HasZ)
				{
					PolylineBuilder builder = new PolylineBuilder(reshapePath);
					builder.HasZ = true;
					reshapePath = builder.ToGeometry();
				}

				reshapeSubcurve.Path = (Polyline) zSettingsModel.ApplyUndefinedZs(reshapePath);
			}
		}
	}
}
