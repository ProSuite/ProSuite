using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.DomainModel.AO.Workflow.WorkspaceFilters
{
	public class WorkspacePathMatchCriterion : IWorkspaceMatchCriterion
	{
		private readonly List<string> _pathPatterns;
		private readonly Dictionary<string, Regex> _regexes;

		public WorkspacePathMatchCriterion([NotNull] IEnumerable<string> pathPatterns)
		{
			Assert.ArgumentNotNull(pathPatterns, nameof(pathPatterns));

			_pathPatterns = new List<string>(pathPatterns);

			const RegexOptions options = RegexOptions.Singleline |
			                             RegexOptions.Compiled |
			                             RegexOptions.IgnoreCase;

			_regexes = new Dictionary<string, Regex>(StringComparer.OrdinalIgnoreCase);

			const bool matchCompleteString = true;
			foreach (string pattern in _pathPatterns)
			{
				if (! _regexes.ContainsKey(pattern))
				{
					string expression =
						RegexUtils.GetWildcardExpression(pattern, matchCompleteString);

					_regexes.Add(pattern, new Regex(expression, options));
				}
			}
		}

		public bool IsSatisfied(IWorkspace workspace, out string reason)
		{
			string path = workspace.PathName;
			bool hasPath = StringUtils.IsNotEmpty(path);

			if (! hasPath)
			{
				if (_pathPatterns.Count == 0)
				{
					reason =
						"Workspace has no file system path, but the list of parent directories is empty";
					return true;
				}

				reason =
					"Workspace has no file system path, but there are parent directory restrictions";
				return false;
			}

			string fullPath;
			try
			{
				fullPath = Path.GetFullPath(path);
			}
			catch (Exception ex)
			{
				reason = string.Format("Unable to get full path: {0}", ex.Message);
				return false;
			}

			foreach (string pattern in _pathPatterns)
			{
				Regex regex = _regexes[pattern];

				if (regex.IsMatch(fullPath))
				{
					reason = string.Format("Workspace path {0} matches {1}", fullPath, pattern);
					return true;
				}
			}

			reason = "Workspace path does not match any of the defined patterns";
			return false;
		}
	}
}
