using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
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
		[Obsolete]
		public IEnvelope InvolvedExtent => GetInvolvedExtent(InvolvedRows);

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
		[Obsolete]
		private static IEnvelope GetInvolvedExtent(
			[CanBeNull] IEnumerable<InvolvedRow> involvedRows)
		{
			if (involvedRows == null)
			{
				return null;
			}

			IEnvelope involvedExtent = null;
			foreach (IReadOnlyRow testedRow in involvedRows)
			{
				if (testedRow is IReadOnlyFeature testedFeature)
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

			return involvedExtent;
		}

		public bool IsProcessed(double xMax, double yMax)
		{
			return _errorGeometry.IsProcessed(xMax, yMax);
		}
	}
}
