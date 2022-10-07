using System.Collections.Generic;
using System.Diagnostics;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DdxEditor.Content.QA.QSpec;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DdxEditor.Content.QA.QCon
{
	internal static class TableRows
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		public static List<QualityConditionWithTestParametersTableRow> GetQualityConditions(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
			[NotNull] QualitySpecification qualitySpecification,
			[CanBeNull] DdxModel model = null)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));

			Stopwatch stopWatch = _msg.DebugStartTiming();

			IQualityConditionRepository repository = modelBuilder.QualityConditions;

			// use set of conditions used in qualitySpecification
			var existingConditions = new HashSet<QualityCondition>();
			foreach (QualitySpecificationElement element in qualitySpecification.Elements)
			{
				existingConditions.Add(element.QualityCondition);
			}

			return modelBuilder.ReadOnlyTransaction(
				delegate
				{
					var testPropertiesById = new Dictionary<int, TestProperties>();

					IDictionary<int, int> qspecCountMap =
						repository.GetReferencingQualitySpecificationCount();

					IList<QualityCondition> qualityConditions =
						model == null
							? repository.GetAllNotInvolvingDeletedDatasets()
							: repository.Get(model);

					var result = new List<QualityConditionWithTestParametersTableRow>();

					// fetchParameterValues:true is expensive (also for subsequent calls)
					foreach (QualityCondition qc in qualityConditions)
					{
						// filtering based on condition.HasDeletedParameterValues() is expensive
						// as the parameters have to be fetched
						// --> provide repository method that excludes deleted dataset parameters?

						TestProperties testProperties;
						if (! testPropertiesById.TryGetValue(qc.TestDescriptor.Id,
						                                     out testProperties))
						{
							testProperties = new TestProperties(qc.TestDescriptor);
							testPropertiesById.Add(qc.TestDescriptor.Id, testProperties);
						}

						int refCount;
						if (! qspecCountMap.TryGetValue(qc.Id, out refCount))
						{
							refCount = 0;
						}

						var tableRow = new QualityConditionWithTestParametersTableRow(
							               qc,
							               testProperties.Signature,
							               testProperties.Description, refCount)
						               {
							               Selectable = ! existingConditions.Contains(qc)
						               };

						result.Add(tableRow);
					}

					_msg.DebugStopTiming(stopWatch, "Read {0} quality conditions", result.Count);
					return result;
				});
		}
	}
}
