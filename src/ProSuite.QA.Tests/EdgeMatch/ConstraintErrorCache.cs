using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.EdgeMatch
{
	internal class ConstraintErrorCache<TError, T>
		where TError : ConstraintError<T>, new()
		where T : EdgeMatchBorderConnection
	{
		[NotNull] private readonly List<TError> _attributeErrors =
			new List<TError>();

		[NotNull] private readonly ConstraintErrorComparer _comparer =
			new ConstraintErrorComparer();

		public void Clear()
		{
			_attributeErrors.Clear();
		}

		[NotNull]
		public IEnumerable<TError> GetSortedErrors()
		{
			_attributeErrors.Sort(_comparer);
			return _attributeErrors;
		}

		public void Add([NotNull] T borderConnection,
		                [NotNull] T neighborConnection,
		                [NotNull] IPolyline commonLine,
		                [CanBeNull] IssueCode code,
		                [NotNull] string constraintDescription,
		                [CanBeNull] string affectedComponents,
		                [CanBeNull] string textValue)
		{
			_attributeErrors.Add(new TError
			                     {
				                     BorderConnection = borderConnection,
				                     NeighborBorderConnection = neighborConnection,
				                     ConstraintDescription = constraintDescription,
				                     IssueCode = code,
				                     AffectedComponents = affectedComponents,
				                     ErrorLine = commonLine,
				                     TextValue = textValue
			                     });
		}

		public void Clear(WKSEnvelope tileWksBox, WKSEnvelope allWksBox)
		{
			var unhandledErrors = new List<TError>(_attributeErrors.Count);

			foreach (TError error in _attributeErrors)
			{
				if (
					! EdgeMatchUtils.VerifyHandled(error.BorderConnection.Feature,
					                               tileWksBox, allWksBox) ||
					! EdgeMatchUtils.VerifyHandled(error.NeighborBorderConnection.Feature,
					                               tileWksBox, allWksBox))
				{
					unhandledErrors.Add(error);
				}
			}

			_attributeErrors.Clear();
			_attributeErrors.AddRange(unhandledErrors);
		}

		public int Compare(TError error, TError commonError)
		{
			return _comparer.Compare(error, commonError, ignoreDescription: true);
		}

		private class ConstraintErrorComparer : IComparer<TError>
		{
			public int Compare(TError x, TError y)
			{
				return Compare(x, y, ignoreDescription: false);
			}

			public int Compare(TError x, TError y, bool ignoreDescription)
			{
				if (x == y)
				{
					return 0;
				}

				int d = x.BorderConnection.ClassIndex.CompareTo(y.BorderConnection.ClassIndex);
				if (d != 0)
				{
					return d;
				}

				d = x.BorderConnection.Feature.OID.CompareTo(y.BorderConnection.Feature.OID);
				if (d != 0)
				{
					return d;
				}

				d =
					x.NeighborBorderConnection.ClassIndex.CompareTo(
						y.NeighborBorderConnection.ClassIndex);
				if (d != 0)
				{
					return d;
				}

				d =
					x.NeighborBorderConnection.Feature.OID.CompareTo(
						y.NeighborBorderConnection.Feature.OID);
				if (d != 0)
				{
					return d;
				}

				d = string.Compare(x.IssueCode.ID, y.IssueCode.ID,
				                   StringComparison.Ordinal);
				if (d != 0)
				{
					return d;
				}

				d = string.Compare(x.AffectedComponents, y.AffectedComponents,
				                   StringComparison.Ordinal);
				if (d != 0)
				{
					return d;
				}

				// NOTE: don't compare TextValue here, as order may be inversed

				if (! ignoreDescription)
				{
					d = string.Compare(x.ConstraintDescription, y.ConstraintDescription,
					                   StringComparison.Ordinal);
					if (d != 0)
					{
						return d;
					}
				}

				return 0;
			}
		}
	}
}
