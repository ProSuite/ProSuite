using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProSuite.Commons.NamedValuesExpressions;
using ProSuite.Commons.Notifications;
using ProSuite.DomainModel.AO.Workflow;
using ProSuite.DomainModel.AO.Workflow.WorkspaceFilters;

namespace ProSuite.DomainModel.AO.Test.Workflow.WorkspaceFilters
{
	[TestFixture]
	public class WorkspaceFilterFactoryTest
	{
		[Test]
		public void CanCreate()
		{
			var expressions =
				new List<NamedValuesExpression>
				{
					new NamedValuesConjunctionExpression()
						.Add(new NamedValues("paths", new[] { @"C:\checkouts\*" }))
						.Add(new NamedValues("gdbtypes", new[] { "sde" }))
				};

			IWorkspaceFilter result =
				WorkspaceFilterFactory.TryCreate(expressions,
				                                 out NotificationCollection notifications);

			Console.WriteLine(NotificationUtils.Concatenate(notifications, Environment.NewLine));
			Assert.IsNotNull(result);
			Assert.AreEqual(0, notifications.Count);
		}

		[Test]
		public void CantCreateInvalid()
		{
			var expressions =
				new List<NamedValuesExpression>
				{
					new NamedValuesConjunctionExpression()
						.Add(new NamedValues("unknownCriterionName1", new[] { "value1", "value2" }))
						.Add(new NamedValues("unknownCriterionName2", new[] { "value2" }))
				};

			IWorkspaceFilter result =
				WorkspaceFilterFactory.TryCreate(expressions,
				                                 out NotificationCollection notifications);

			Console.WriteLine(NotificationUtils.Concatenate(notifications, Environment.NewLine));
			Assert.IsNull(result);
			Assert.AreEqual(2, notifications.Count);
		}
	}
}
