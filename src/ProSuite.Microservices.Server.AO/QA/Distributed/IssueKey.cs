using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.QA;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.QA.Container;

namespace ProSuite.Microservices.Server.AO.QA.Distributed
{
	internal class IssueKey
	{
		private readonly ITest _test;
		private readonly ISpatialReference _issueSpatialReference;

		private IssueMsg _issueMsg;

		public IssueKey([NotNull] IssueMsg issueMsg,
		                [NotNull] ITest test,
		                [CanBeNull] ISpatialReference issueSpatialReference)
		{
			_issueMsg = issueMsg;
			ConditionId = _issueMsg.ConditionId;
			Description = _issueMsg.Description;

			_test = test;
			_issueSpatialReference = issueSpatialReference;
		}

		public int ConditionId { get; }
		public string Description { get; }
		public List<InvolvedRow> InvolvedRows => EnsureInvolvedRows();
		private List<InvolvedRow> _involvedRows;

		private List<InvolvedRow> EnsureInvolvedRows()
		{
			if (_involvedRows != null)
			{
				return _involvedRows;
			}

			_involvedRows = GetSortedInvolvedRows(_issueMsg.InvolvedTables);
			TryClearIssueMsg();
			return _involvedRows;
		}

		public QaError QaError => EnsureQaError();

		public bool Filtered { get; set; }

		private QaError _qaError;

		private QaError EnsureQaError()
		{
			if (_qaError != null)
			{
				return _qaError;
			}

			_qaError = GetQaError();
			TryClearIssueMsg();
			return _qaError;
		}

		public bool EnsureKeyData()
		{
			EnsureInvolvedRows();
			EnsureQaError();
			return TryClearIssueMsg();
		}

		private bool TryClearIssueMsg()
		{
			if (_issueMsg == null)
			{
				return false;
			}

			if (_qaError != null && _involvedRows != null)
			{
				_issueMsg = null;
				return true;
			}

			return false;
		}

		private List<InvolvedRow> GetSortedInvolvedRows(IList<InvolvedTableMsg> involvedTables)
		{
			InvolvedRows involvedRows = new InvolvedRows();
			foreach (InvolvedTableMsg involvedTable in involvedTables)
			{
				foreach (long oid in involvedTable.ObjectIds)
				{
					involvedRows.Add(
						new InvolvedRow(involvedTable.TableName, oid));
				}
			}

			TestUtils.SortInvolvedRows(involvedRows);
			return involvedRows;
		}

		private List<InvolvedRow> GetSortedInvolvedRows(string legacyInvolvedRows)
		{
			InvolvedRows involvedRows = RowParser.Parse(legacyInvolvedRows);
			TestUtils.SortInvolvedRows(involvedRows);
			return involvedRows;
		}

		private QaError GetQaError()
		{
			QaError error = new QaError(
				_test, _issueMsg.Description, InvolvedRows,
				ProtobufGeometryUtils.FromShapeMsg(_issueMsg.IssueGeometry,
				                                   _issueSpatialReference), null, null);
			error.ReduceGeometry();
			return error;
		}
	}
}
