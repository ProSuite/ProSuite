using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Microservices.Client.AGP.GeometryProcessing.ChangeAlong
{
	public class ChangeAlongCurves
	{
		private ReshapeAlongCurveUsability _curveUsability;
		private readonly List<CutSubcurve> _reshapeSubcurves;

		public ChangeAlongCurves([NotNull] IEnumerable<CutSubcurve> subcurves,
		                         ReshapeAlongCurveUsability curveUsability)
		{
			_curveUsability = curveUsability;
			_reshapeSubcurves = new List<CutSubcurve>(subcurves);
		}

		public IList<CutSubcurve> PreSelectedSubcurves { get; } = new List<CutSubcurve>();

		public void Update([NotNull] ChangeAlongCurves newState)
		{
			_curveUsability = newState._curveUsability;

			_reshapeSubcurves.Clear();
			_reshapeSubcurves.AddRange(newState.ReshapeCutSubcurves);

			TargetFeatures = newState.TargetFeatures;
		}

		public IReadOnlyCollection<CutSubcurve> ReshapeCutSubcurves =>
			_reshapeSubcurves.AsReadOnly();

		public bool HasSelectableCurves =>
			_curveUsability == ReshapeAlongCurveUsability.CanReshape ||
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
	}
}
