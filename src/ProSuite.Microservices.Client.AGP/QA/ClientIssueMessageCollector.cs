using System;
using System.Collections.Generic;
using ProSuite.DomainModel.Core.QA;
using ProSuite.Microservices.Client.QA;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared;

namespace ProSuite.Microservices.Client.AGP.QA
{
	public class ClientIssueMessageCollector : IClientIssueMessageCollector
	{
		private readonly List<IssueMsg> _issueMessages;

		public ClientIssueMessageCollector()
		{
			_issueMessages = new List<IssueMsg>();
		}

		/// <summary>
		/// <inheritdoc cref="IClientIssueMessageCollector"/>
		/// </summary>
		public ErrorDeletionInPerimeter ErrorDeletionInPerimeter { get; set; }

		/// <summary>
		/// <inheritdoc cref="IClientIssueMessageCollector"/>
		/// </summary>
		public bool HasIssues => _issueMessages.Count > 0;

		/// <summary>
		/// <inheritdoc cref="IClientIssueMessageCollector"/>
		/// </summary>
		public int SaveIssues(IEnumerable<int> verifiedConditionIds)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// <inheritdoc cref="IClientIssueMessageCollector"/>
		/// </summary>
		public void SetVerifiedPerimeter(ShapeMsg perimeterMsg) { }

		/// <summary>
		/// <inheritdoc cref="IClientIssueMessageCollector"/>
		/// </summary>
		public void AddIssueMessage(IssueMsg issueMsg)
		{
			_issueMessages.Add(issueMsg);
		}

		/// <summary>
		/// <inheritdoc cref="IClientIssueMessageCollector"/>
		/// </summary>
		public void AddObsoleteException(GdbObjRefMsg gdbObjRefMsg) { }
	}
}
