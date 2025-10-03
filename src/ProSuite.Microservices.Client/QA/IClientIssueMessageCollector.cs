using System.Collections.Generic;
using System.Threading.Tasks;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Client.QA
{
	/// <summary>
	/// Maintains the list of issues found during a specific verification run in the background
	/// in order to save them if desired.
	/// </summary>
	public interface IClientIssueMessageCollector
	{
		bool HasIssues { get; }

		/// <summary>
		/// Add an issue found in the background.
		/// </summary>
		/// <param name="issueMsg"></param>
		void AddIssueMessage([NotNull] IssueMsg issueMsg);

		/// <summary>
		/// Add an obsolete exception (allowed error) that was found in the background.
		/// </summary>
		/// <param name="gdbObjRefMsg"></param>
		void AddObsoleteException([NotNull] GdbObjRefMsg gdbObjRefMsg);

		/// <summary>
		/// Update the actually verified perimeter by the background process.
		/// NOTE: This method can be called on an MTA thread!
		/// </summary>
		/// <param name="perimeterMsg"></param>
		void SetVerifiedPerimeter([CanBeNull] ShapeMsg perimeterMsg);

		/// <summary>
		/// Determines whether all or only the verified issues are deleted before
		/// the new issues are stored.
		/// </summary>
		ErrorDeletionInPerimeter ErrorDeletionInPerimeter { get; set; }

		/// <summary>
		/// Saves the found issues and deletes the obsolete exceptions.
		/// </summary>
		/// <param name="verifiedConditionIds"></param>
		/// <returns></returns>
		int SaveIssues([NotNull] IEnumerable<int> verifiedConditionIds);

		/// <summary>
		/// Saves the found issues and deletes the obsolete exceptions.
		/// </summary>
		/// <param name="verifiedConditionIds"></param>
		/// <returns></returns>
		Task<int> SaveIssuesAsync([NotNull] IList<int> verifiedConditionIds);
	}
}
