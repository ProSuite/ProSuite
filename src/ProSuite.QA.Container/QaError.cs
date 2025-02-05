using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.IssueCodes;

namespace ProSuite.QA.Container
{
	/// <summary>
	/// An error found by a test
	/// </summary>
	public class QaError
	{
		private readonly QaErrorGeometry _errorGeometry;
		private WKSEnvelope? _involvedExtent;

		#region Constructors

		public QaError([NotNull] ITest test,
		               [NotNull] string description,
		               [NotNull] IEnumerable<InvolvedRow> involvedRows,
		               [CanBeNull] IGeometry geometry,
		               [CanBeNull] IssueCode issueCode,
		               [CanBeNull] string affectedComponent,
		               bool assertionFailed = false,
		               [CanBeNull] IEnumerable<object> values = null)
		{
			Assert.ArgumentNotNull(test, nameof(test));
			Assert.ArgumentNotNullOrEmpty(description, nameof(description));
			Assert.ArgumentNotNull(involvedRows, nameof(involvedRows));

			Test = test;
			InvolvedRows = new List<InvolvedRow>(involvedRows);

			_errorGeometry = new QaErrorGeometry(geometry);

			IssueCode = issueCode;
			AffectedComponent = affectedComponent;
			Description = description;
			AssertionFailed = assertionFailed;
			Values = values?.ToList();

			Duplicate = false;
		}

		#endregion

		[NotNull]
		public ITest Test { get; }

		[NotNull]
		public IList<InvolvedRow> InvolvedRows { get; }

		/// <summary>
		/// unreduced geometry,
		/// will throw InvalidOperation when the error is completely processed by the TestContainer and TestContainer.KeepErrorGeometry == false
		/// </summary>
		[CanBeNull]
		public IGeometry Geometry => _errorGeometry.Geometry;

		[CanBeNull]
		public IssueCode IssueCode { get; }

		[CanBeNull]
		public string AffectedComponent { get; }

		[NotNull]
		public string Description { get; }

		public bool Duplicate { get; set; }

		public bool AssertionFailed { get; }

		[CanBeNull]
		public WKSEnvelope? InvolvedExtent =>
			_involvedExtent ??
			(_involvedExtent =
				 GetInvolvedExtent(InvolvedRows) ?? _errorGeometry?.GetEnvelope());

		[CanBeNull]
		public IList<object> Values { get; }

		public int CompareEnvelope(QaError other)
		{
			return _errorGeometry.CompareEnvelope(other._errorGeometry);
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			foreach (InvolvedRow involvedRow in InvolvedRows)
			{
				if (sb.Length > 0)
				{
					sb.Append("; ");
				}

				sb.AppendFormat("{0},{1}", involvedRow.TableName, involvedRow.OID);
			}

			sb.AppendFormat(": {0}", Description);

			if (IssueCode != null)
			{
				sb.AppendFormat(" [{0}]", IssueCode.ID);
			}

			if (! string.IsNullOrEmpty(AffectedComponent))
			{
				sb.AppendFormat(" {{{0}}}", AffectedComponent);
			}

			return sb.ToString();
		}

		public void ReduceGeometry()
		{
			_errorGeometry.ReduceGeometry();
		}

		[CanBeNull]
		private static WKSEnvelope? GetInvolvedExtent(
			[CanBeNull] IEnumerable<InvolvedRow> involvedRows)
		{
			if (involvedRows == null)
			{
				return null;
			}

			IEnvelope involvedExtent = null;
			foreach (InvolvedRow involvedRow in involvedRows)
			{
				// TODO: implement correct logic. Remark: involved rows know only table / OID
				if (involvedRow is IReadOnlyFeature testedFeature)
				{
					IEnvelope shapeEnvelope = null;
					try
					{
						// This fails if the Shape field was not in the SubFields:
						shapeEnvelope = testedFeature.Shape.Envelope;
					}
					catch (Exception)
					{
						continue;
					}

					if (involvedExtent == null)
					{
						involvedExtent = GeometryFactory.Clone(shapeEnvelope);
					}
					else
					{
						involvedExtent.Union(GeometryFactory.Clone(shapeEnvelope));
					}
				}
			}

			if (involvedExtent == null)
			{
				return null;
			}

			involvedExtent.QueryWKSCoords(out WKSEnvelope wksExtent);
			return wksExtent;
		}

		public bool IsProcessed(double xMax, double yMax)
		{
			if (! _errorGeometry.IsProcessed(xMax, yMax))
			{
				return false;
			}

			if (InvolvedExtent == null)
			{
				return true;
			}

			return InvolvedExtent.Value.XMax < xMax &&
			       InvolvedExtent.Value.YMax < yMax;
		}

		/// <summary>
		/// Sets the error geometry in the spatial reference of the target (error) feature classes
		/// of the model referenced by the verification context.
		/// </summary>
		/// <param name="projectedGeometry"></param>
		public void SetGeometryInModelSpatialReference(IGeometry projectedGeometry)
		{
			_errorGeometry.SetGeometryInModelSpatialReference(projectedGeometry);
		}

		public IGeometry GetGeometryInModelSpatialRef()
		{
			return _errorGeometry.GetGeometryInModelSpatialRef();
		}
	}
}
