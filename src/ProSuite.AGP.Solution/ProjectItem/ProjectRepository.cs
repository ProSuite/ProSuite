using ArcGIS.Desktop.Core;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.ProjectItem
{
	public class ProjectRepository
	{
		private static ProjectRepository _repository;
		public static ProjectRepository Current => _repository ?? (_repository = new ProjectRepository());

		public IEnumerable<string> GetProjectFileItems(ProjectItemType itemType)
		{
			ProjectFileRepository fileRepo = InitRepository(itemType);
			return fileRepo?.GetAll() ?? new List<string>();
		}

		public bool AddProjectFileItems(ProjectItemType itemType, [NotNull] IEnumerable<string> filesPath)
		{
			ProjectFileRepository fileRepo = InitRepository(itemType);
			if (fileRepo != null)
			{
				foreach (var path in filesPath)
				{
					fileRepo.Add(path);
				}
				return true;
			}
			return false;
		}

		public bool DeleteProjectFileItems(ProjectItemType itemType, [NotNull] IEnumerable<string> filesPath)
		{
			ProjectFileRepository fileRepo = InitRepository(itemType);
			if (fileRepo != null)
			{
				foreach (var path in filesPath)
				{
					fileRepo.Delete(path);
				}
				return true;
			}
			return false;
		}

		private ProjectFileRepository InitRepository(ProjectItemType itemType)
		{
			ProjectFileRepository repo;
			switch (itemType)
			{
				case ProjectItemType.WorkListDefinition:
					repo = new ProjectWorkListFileRepository(Project.Current);
					break;
				default:
					repo = new ProjectFileRepository(Project.Current);
					break;
			}
			return repo;
		}
	}
}
