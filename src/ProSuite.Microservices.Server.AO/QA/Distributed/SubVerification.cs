using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.SpatialIndex;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.QA.Container;

namespace ProSuite.Microservices.Server.AO.QA.Distributed
{
	/// <summary>
	/// Encapsulates the definition and state of a sub-verification that runs on a different client.
	/// </summary>
	public class SubVerification
	{
		public SubVerification([NotNull] VerificationRequest subRequest,
		                       [NotNull] QualityConditionGroup qualityConditionGroup)
		{
			Assert.ArgumentNotNull(subRequest, nameof(subRequest));
			Assert.ArgumentNotNull(qualityConditionGroup, nameof(qualityConditionGroup));

			SubRequest = subRequest;
			SubResponse = new SubResponse();
			QualityConditionGroup = qualityConditionGroup;
		}

		public VerificationRequest SubRequest { get; }
		public SubResponse SubResponse { get; }
		public EnvelopeXY TileEnvelope { get; set; }
		public QualityConditionGroup QualityConditionGroup { get; }
		public bool Completed { get; set; }
		public int Id { get; set; }

		public long? InvolvedBaseRowsCount { get; set; }

		public int FailureCount { get; set; }

		public int IssueCount { get; set; }
		public int FilteredIssueCount { get; set; }

		private Dictionary<int, QualityCondition> _idConditions;

		private Dictionary<int, QualityCondition> GetIdConditions()
		{
			Dictionary<int, QualityCondition> idConditions =
				new Dictionary<int, QualityCondition>();
			foreach (QualityCondition qualityCondition in QualityConditionGroup
			                                              .QualityConditions.Keys)
			{
				idConditions[qualityCondition.Id] = qualityCondition;
			}

			return idConditions;
		}

		public ITest GetFirstTest(int conditionId)
		{
			QualityCondition qc = GetQualityCondition(conditionId);

			ITest test = QualityConditionGroup.QualityConditions[qc].First();
			return test;
		}

		public QualityCondition GetQualityCondition(int conditionId)
		{
			_idConditions = _idConditions ?? GetIdConditions();

			return _idConditions[conditionId];
		}

		public bool ContainsCondition(int conditionId)
		{
			_idConditions = _idConditions ?? GetIdConditions();
			return _idConditions.ContainsKey(conditionId);
		}

		internal bool IsFullyProcessed(IssueKey issue, [NotNull] BoxTree<SubVerification> boxTree)
		{
			_idConditions = _idConditions ?? GetIdConditions();
			if (! _idConditions.ContainsKey(issue.ConditionId))
			{
				return false;
			}

			if (! (issue.QaError.InvolvedExtent is WKSEnvelope b))
			{
				return false;
			}

			Box searchBox = new Box(new Pnt2D(b.XMin, b.YMin), new Pnt2D(b.XMax, b.YMax));
			foreach (BoxTree<SubVerification>.TileEntry entry in boxTree.Search(searchBox))
			{
				if (entry.Value.Completed == false)
				{
					return false;
				}
			}

			// TODO: Check extent of issue with processed area
			return true;
		}

		#region Overrides of Object

		public override string ToString()
		{
			return
				$"{QualityConditionGroup.ExecType} sub-verification with {QualityConditionGroup.QualityConditions.Count} " +
				$"condition(s) in envelope {TileEnvelope}";
		}

		#endregion
	}
}
