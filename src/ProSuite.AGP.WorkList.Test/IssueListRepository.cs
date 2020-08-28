using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.AGP.Storage;
using System.Collections.Generic;
using System.Linq;

namespace ProSuite.AGP.WorkList.Test
{
	public class IssueListRepository : IRepository<IssueItem>
	{
		private readonly IRepository<IssueItem> _issuesRepository;
		private readonly IRepository<GdbRowIdentity> _issuesStateRepository;

		public IssueListRepository(IRepository<IssueItem> issuesRepository, IRepository<GdbRowIdentity> statesRepository)
		{
			_issuesRepository = issuesRepository;
			_issuesStateRepository = statesRepository;
		}

		// TODO algr: queryfilter?
		public IQueryable<IssueItem> GetAll()
		{
			var issues = _issuesRepository.GetAll();
			var states = _issuesStateRepository.GetAll();
			if (states.Any())
			{
				// sync states with issues 
				foreach (var state in states)
				{
					var visitedIssue = issues.FirstOrDefault(i => i.Proxy == state);
					if (visitedIssue != null)
					{
						visitedIssue.Status = WorkItemStatus.Done;
					}
				}
			}
			return issues;
		}

		public void Add(IssueItem item)
		{
			// not necessary?
		}

		public void Update(IssueItem item)
		{
			//_issuesStateRepository.Update(item.Proxy);

			var existingItemState = _issuesStateRepository.GetAll().FirstOrDefault(i => (i==item.Proxy));

			// only one state is possible?
			if (item.Status == WorkItemStatus.Done && existingItemState == null)
			{
					_issuesStateRepository.Add(item.Proxy);
			}
		}
	

		public void Delete(IssueItem item)
		{
			// not necessary?
		}

		public void SaveChanges()
		{
			// save 
			_issuesStateRepository.SaveChanges();
		}
	}
}
