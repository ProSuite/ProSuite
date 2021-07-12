using System;
using System.Threading;
using ArcGIS.Core.Data;
using NUnit.Framework;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.AGP.Hosting;

namespace ProSuite.AGP.WorkList.Test
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class IssueItemTest
	{
		private const string _emptyIssuesGdb =
			@"C:\git\ProSuite\src\ProSuite.AGP.WorkList.Test\TestData\dev\OSM_Full_20200821_153100\issues.gdb";

		private readonly string _issuePointsFeatureClassName = "IssuePoints";
		private Geodatabase _geodatabase;
		private FeatureClass _issuePoints;

		[SetUp]
		public void SetUp()
		{
			// http://stackoverflow.com/questions/8245926/the-current-synchronizationcontext-may-not-be-used-as-a-taskscheduler
			SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

			_geodatabase =
				new Geodatabase(
					new FileGeodatabaseConnectionPath(new Uri(_emptyIssuesGdb, UriKind.Absolute)));

			// todo daro: test IssueRows as well!
			_issuePoints = _geodatabase.OpenDataset<FeatureClass>(_issuePointsFeatureClassName);
		}

		[TearDown]
		public void TearDown()
		{
			_geodatabase?.Dispose();
			_issuePoints?.Dispose();
		}

		[OneTimeSetUp]
		public void SetupFixture()
		{
			// Host must be initialized on an STA thread:
			CoreHostProxy.Initialize();
		}

		[Test]
		public void Can_instantiate_IssueItem()
		{
			var definition =
				_geodatabase.GetDefinition<FeatureClassDefinition>(_issuePointsFeatureClassName);

			var reader = new AttributeReader(definition,
			                                 Attributes.IssueCode,
			                                 Attributes.IssueCodeDescription,
			                                 Attributes.InvolvedObjects,
			                                 Attributes.QualityConditionName,
			                                 Attributes.TestName,
			                                 Attributes.TestDescription,
			                                 Attributes.TestType,
			                                 Attributes.IssueSeverity,
			                                 Attributes.IsStopCondition,
			                                 Attributes.Category,
			                                 Attributes.AffectedComponent,
			                                 Attributes.Url,
			                                 Attributes.DoubleValue1,
			                                 Attributes.DoubleValue2,
			                                 Attributes.TextValue,
			                                 Attributes.IssueAssignment,
			                                 Attributes.QualityConditionUuid,
			                                 Attributes.QualityConditionVersionUuid,
			                                 Attributes.ExceptionStatus,
			                                 Attributes.ExceptionNotes,
			                                 Attributes.ExceptionCategory,
			                                 Attributes.ExceptionOrigin,
			                                 Attributes.ExceptionDefinedDate,
			                                 Attributes.ExceptionLastRevisionDate,
			                                 Attributes.ExceptionRetirementDate,
			                                 Attributes.ExceptionShapeMatchCriterion);

			foreach (Feature feature in GdbQueryUtils.GetRows<Feature>(_issuePoints))
			{
				var item = new IssueItem(0, feature);
				break;
			}
		}
	}
}
