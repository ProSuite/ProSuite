using System.Collections.Generic;
using System.Linq;
using log4net;
using log4net.Core;
using log4net.Repository.Hierarchy;
using NUnit.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainModel.Core.Test.QA
{
	[TestFixture]
	public class QualitySpecificationTest
	{
		private readonly List<LoggingEvent> _loggingEvents = new List<LoggingEvent>();
		private ActionAppender _appender;
		private Hierarchy _repository;
		private bool _repositoryWasConfigured;
		private Level _previousRootLevel;

		[SetUp]
		public void SetUp()
		{
			_loggingEvents.Clear();
			_appender = new ActionAppender(e => _loggingEvents.Add(e));

			// Creating a logger creates the log4net repository that the appender is added to:
			Msg.ForCurrentClass();

			Log4NetUtils.AddRootAppender(_appender);

			// An unconfigured repository discards all messages:
			_repository = LogManager.GetAllRepositories()
			                        .OfType<Hierarchy>()
			                        .Single(h => h.Root.Appenders.Contains(_appender));

			_repositoryWasConfigured = _repository.Configured;
			_previousRootLevel = _repository.Root.Level;

			_repository.Configured = true;
			_repository.Root.Level = Level.Debug;
		}

		[TearDown]
		public void TearDown()
		{
			Log4NetUtils.RemoveRootAppender(_appender);

			_repository.Configured = _repositoryWasConfigured;
			_repository.Root.Level = _previousRootLevel;
		}

		[Test]
		public void DisableNonApplicableElements_selection_logs_one_info_line()
		{
			// QA server issue list, 20.10.: a "Verify Selection" of one object wrote one warning
			// per condition that does not involve the selected object's dataset (842 lines).
			// Not verifying these conditions is intended when only a selection is verified.
			var roads = new ErrorLineDataset("Roads");
			var rails = new ErrorLineDataset("Rails");

			QualitySpecification specification =
				CreateSpecification(("Roads condition", roads),
				                    ("Rails condition 1", rails),
				                    ("Rails condition 2", rails));

			IList<QualitySpecificationElement> disabled =
				specification.DisableNonApplicableElements(new[] { roads },
				                                            verifiesSelection: true);

			Assert.AreEqual(2, disabled.Count);
			Assert.AreEqual(0, GetMessages(Level.Warn).Count);

			List<string> infoMessages = GetMessages(Level.Info);
			Assert.AreEqual(1, infoMessages.Count);
			StringAssert.Contains("2 of 3", infoMessages[0]);

			// The names of the conditions are still available at debug level:
			List<string> debugMessages = GetMessages(Level.Debug);
			Assert.True(debugMessages.Any(m => m.Contains("Rails condition 1")));
			Assert.True(debugMessages.Any(m => m.Contains("Rails condition 2")));
		}

		[Test]
		public void DisableNonApplicableElements_without_selection_warns_per_condition()
		{
			// Without a selection, a condition that does not involve a verified dataset can
			// mean that the wrong project or workspace was chosen. Keep the warning.
			var roads = new ErrorLineDataset("Roads");
			var rails = new ErrorLineDataset("Rails");

			QualitySpecification specification =
				CreateSpecification(("Roads condition", roads),
				                    ("Rails condition", rails));

			IList<QualitySpecificationElement> disabled =
				specification.DisableNonApplicableElements(new[] { roads });

			Assert.AreEqual(1, disabled.Count);

			List<string> warnings = GetMessages(Level.Warn);
			Assert.AreEqual(1, warnings.Count);
			StringAssert.Contains("Rails condition", warnings[0]);
		}

		[NotNull]
		private List<string> GetMessages([NotNull] Level level)
		{
			return _loggingEvents.Where(e => e.Level == level)
			                     .Select(e => e.RenderedMessage)
			                     .ToList();
		}

		[NotNull]
		private static QualitySpecification CreateSpecification(
			params (string conditionName, Dataset dataset)[] conditions)
		{
			var specification = new QualitySpecification("Specification");

			var testDescriptor = new TestDescriptor(
				"TestDescriptor", new ClassDescriptor(typeof(QualitySpecificationTest)), 0);

			foreach ((string conditionName, Dataset dataset) in conditions)
			{
				var condition = new QualityCondition(conditionName, testDescriptor);

				condition.AddParameterValue(
					new DatasetTestParameterValue("featureClass", typeof(Dataset))
					{
						DatasetValue = dataset
					});

				specification.AddElement(condition);
			}

			return specification;
		}
	}
}
