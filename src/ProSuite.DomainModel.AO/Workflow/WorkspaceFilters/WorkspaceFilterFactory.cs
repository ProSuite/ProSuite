using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.NamedValuesExpressions;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Text;

namespace ProSuite.DomainModel.AO.Workflow.WorkspaceFilters
{
	public static class WorkspaceFilterFactory
	{
		private const string _criterionNamePaths = "paths";
		private const string _criterionNameGdbTypes = "gdbtypes";

		[NotNull] private static readonly List<WorkspaceDbTypeInfo> _workspaceDbTypeInfos =
			new List<WorkspaceDbTypeInfo>
			{
				new WorkspaceDbTypeInfo("sde", WorkspaceDbType.ArcSDE),
				new WorkspaceDbTypeInfo("sde-db2", WorkspaceDbType.ArcSDEDB2),
				new WorkspaceDbTypeInfo("sde-informix", WorkspaceDbType.ArcSDEInformix),
				new WorkspaceDbTypeInfo("sde-oracle", WorkspaceDbType.ArcSDEOracle),
				new WorkspaceDbTypeInfo("sde-postgresql", WorkspaceDbType.ArcSDEPostgreSQL),
				new WorkspaceDbTypeInfo("sde-sqlserver", WorkspaceDbType.ArcSDESqlServer),
				new WorkspaceDbTypeInfo("fgdb", WorkspaceDbType.FileGeodatabase),
				new WorkspaceDbTypeInfo("pgdb", WorkspaceDbType.PersonalGeodatabase),
				new WorkspaceDbTypeInfo("mgdb", WorkspaceDbType.MobileGeodatabase)
			};

		[NotNull] private static readonly Dictionary<string, WorkspaceDbTypeInfo>
			_workspaceDbTypeInfosByName = _workspaceDbTypeInfos.ToDictionary(
				info => info.Name, StringComparer.OrdinalIgnoreCase);

		[CanBeNull]
		public static IWorkspaceFilter TryCreate(
			[NotNull] IEnumerable<NamedValuesExpression> expressions,
			[NotNull] out NotificationCollection notifications)
		{
			Assert.ArgumentNotNull(expressions, nameof(expressions));

			notifications = new NotificationCollection();

			IEnumerable<IWorkspaceMatchCriterion> workspaceMatchCriteria;
			bool success = TryGetCriteria(expressions,
			                              notifications,
			                              out workspaceMatchCriteria);

			return success
				       ? new WorkspaceFilter(workspaceMatchCriteria)
				       : null;
		}

		private static bool TryGetCriteria(
			[NotNull] IEnumerable<NamedValuesExpression> expressions,
			[NotNull] NotificationCollection notifications,
			[NotNull] out IEnumerable<IWorkspaceMatchCriterion> criteria)
		{
			var list = new List<IWorkspaceMatchCriterion>();

			var anyFailure = false;
			foreach (NamedValuesExpression expression in expressions)
			{
				IWorkspaceMatchCriterion criterion = TryCreate(expression, notifications);
				if (criterion == null)
				{
					anyFailure = true;
				}
				else
				{
					list.Add(criterion);
				}
			}

			criteria = list;
			return ! anyFailure;
		}

		[CanBeNull]
		private static IWorkspaceMatchCriterion TryCreate(
			[NotNull] NamedValuesExpression expression,
			[NotNull] NotificationCollection notifications)
		{
			Assert.ArgumentNotNull(expression, nameof(expression));
			Assert.ArgumentNotNull(notifications, nameof(notifications));

			var simpleExpression = expression as SimpleNamedValuesExpression;
			if (simpleExpression != null)
			{
				return TryCreate(simpleExpression.NamedValues, notifications);
			}

			var conjunction = expression as NamedValuesConjunctionExpression;
			if (conjunction != null)
			{
				return TryCreate(conjunction, notifications);
			}

			throw new ArgumentException(
				string.Format("Unsupported expression type: {0}", expression),
				nameof(expression));
		}

		[CanBeNull]
		private static IWorkspaceMatchCriterion TryCreate(
			[NotNull] NamedValuesConjunctionExpression conjunction,
			[NotNull] NotificationCollection notifications)
		{
			var result = new WorkspaceMatchCriterionConjunction();

			var anyFailure = false;

			foreach (NamedValues namedValues in conjunction.NamedValuesCollection)
			{
				IWorkspaceMatchCriterion criterion = TryCreate(namedValues, notifications);
				if (criterion != null)
				{
					result.Add(criterion);
				}
				else
				{
					anyFailure = true;
				}
			}

			return anyFailure
				       ? null
				       : result;
		}

		[CanBeNull]
		private static IWorkspaceMatchCriterion TryCreate(
			[NotNull] NamedValues namedValues,
			[NotNull] NotificationCollection notifications)
		{
			switch (namedValues.Name.ToLower(CultureInfo.InvariantCulture))
			{
				case _criterionNamePaths:
					return new WorkspacePathMatchCriterion(namedValues.Values);

				case _criterionNameGdbTypes:
					IEnumerable<WorkspaceDbTypeInfo> workspaceDbTypeInfos;
					return TryGetWorkspaceDbTypes(namedValues.Values, notifications,
					                              out workspaceDbTypeInfos)
						       ? new WorkspaceDbTypeMatchCriterion(workspaceDbTypeInfos)
						       : null;

				default:
					notifications.Add("Unknown criterion name: {0}; supported criterion names: {1}",
					                  namedValues.Name,
					                  StringUtils.Concatenate(
						                  new[]
						                  {
							                  _criterionNamePaths,
							                  _criterionNameGdbTypes
						                  },
						                  ","));
					return null;
			}
		}

		private static bool TryGetWorkspaceDbTypes(
			[NotNull] IEnumerable<string> values,
			[NotNull] NotificationCollection notifications,
			[NotNull] out IEnumerable<WorkspaceDbTypeInfo> workspaceDbTypeInfos)
		{
			var result = new List<WorkspaceDbTypeInfo>();

			var anyFailure = false;
			foreach (string value in values)
			{
				WorkspaceDbTypeInfo info;
				if (_workspaceDbTypeInfosByName.TryGetValue(value, out info))
				{
					result.Add(info);
					continue;
				}

				notifications.Add("Unsupported gdb type value: {0}; supported values: {1}",
				                  value, ConcatenateGdbTypeNames());
				anyFailure = true;
			}

			if (anyFailure)
			{
				workspaceDbTypeInfos = new List<WorkspaceDbTypeInfo>();
				return false;
			}

			workspaceDbTypeInfos = result;
			return true;
		}

		[NotNull]
		private static string ConcatenateGdbTypeNames()
		{
			List<WorkspaceDbTypeInfo> filteredWorkspaceInfos = _workspaceDbTypeInfos;

#if !NET48
			filteredWorkspaceInfos = _workspaceDbTypeInfos.Where(ti => ti.Name != "pgdb").ToList();
#endif

			return StringUtils.Concatenate(filteredWorkspaceInfos, info => info.Name, ",");
		}
	}
}
