using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.QA.DependencyGraph
{
	public static class DependencyGraphUtils
	{
		[NotNull]
		public static DatasetDependencyGraph GetGraph(
			[NotNull] QualitySpecification qualitySpecification,
			bool exportBidirectionalDependenciesAsUndirectedEdges = false,
			bool includeSelfDependencies = false)
		{
			return GetGraph(new[] {qualitySpecification},
			                exportBidirectionalDependenciesAsUndirectedEdges,
			                includeSelfDependencies);
		}

		[NotNull]
		public static DatasetDependencyGraph GetGraph(
			[NotNull] IEnumerable<QualitySpecification> qualitySpecifications,
			bool exportBidirectionalDependenciesAsUndirectedEdges = false,
			bool includeSelfDependencies = false)
		{
			Assert.ArgumentNotNull(qualitySpecifications, nameof(qualitySpecifications));

			QualitySpecification union = Union(qualitySpecifications);

			var result = new DatasetDependencyGraph(GetDatasets(union), union.Name);

			foreach (QualitySpecificationElement element in union.Elements)
			{
				QualityCondition qualityCondition = element.QualityCondition;

				List<DatasetTestParameterValue> datasetTestParameterValues =
					GetDatasetTestParameterValues(qualityCondition).ToList();

				for (var i = 0; i < datasetTestParameterValues.Count; i++)
				{
					DatasetTestParameterValue value1 = datasetTestParameterValues[i];

					if (value1.DatasetValue == null)
					{
						continue;
					}

					int startIndex = exportBidirectionalDependenciesAsUndirectedEdges
						                 ? i + 1 // half matrix
						                 : 0; // full matrix

					for (int j = startIndex; j < datasetTestParameterValues.Count; j++)
					{
						if (i == j)
						{
							continue;
						}

						DatasetTestParameterValue value2 = datasetTestParameterValues[j];

						if (value2.DatasetValue == null)
						{
							continue;
						}

						if (! includeSelfDependencies &&
						    Equals(value1.DatasetValue, value2.DatasetValue))
						{
							// dependency between same dataset (but different parameter indexes), ignore
							continue;
						}

						if (value1.UsedAsReferenceData)
						{
							// no dependency from value1 to value2
							continue;
						}

						if (value1.TestParameterName == value2.TestParameterName)
						{
							if (ExcludeDependenciesWithinMultiValuedParameter(qualityCondition,
							                                                  value1
								                                                  .TestParameterName)
							)
							{
								continue;
							}
						}

						bool directed;
						if (exportBidirectionalDependenciesAsUndirectedEdges)
						{
							// if (value2 is used as reference data: the dependency is directed)
							directed = value2.UsedAsReferenceData;
						}
						else
						{
							// always directed (bidirectional dependencies represented as two directed dependencies)
							directed = true;
						}

						result.AddDependency(element,
						                     value1.DatasetValue, value2.DatasetValue,
						                     value1.TestParameterName, value2.TestParameterName,
						                     value1.FilterExpression, value2.FilterExpression,
						                     directed);
					}
				}
			}

			return result;
		}

		[NotNull]
		public static string GetQualityConditionId(
			[NotNull] QualityCondition qualityCondition)
		{
			return qualityCondition.Name;
		}

		[NotNull]
		public static string GetDatasetId([NotNull] Dataset dataset)
		{
			return string.Format("{0}::{1}", dataset.Model.Name, dataset.Name);
		}

		internal static IEnumerable<DatasetTestParameterValue>
			GetDatasetTestParameterValues([NotNull] QualityCondition qualityCondition)
		{
			foreach (TestParameterValue testParameterValue in qualityCondition.ParameterValues)
			{
				var dsValue = testParameterValue as DatasetTestParameterValue;
				if (dsValue == null)
				{
					continue;
				}

				Dataset dataset = dsValue.DatasetValue;
				if (dataset == null || dataset.Deleted)
				{
					continue;
				}

				yield return dsValue;
			}
		}

		[NotNull]
		internal static IEnumerable<Dataset> GetDatasets(
			[NotNull] QualitySpecification qualitySpecification)
		{
			var result = new HashSet<Dataset>();

			foreach (QualitySpecificationElement element in qualitySpecification.Elements)
			{
				foreach (Dataset dataset in element.QualityCondition.GetDatasetParameterValues())
				{
					result.Add(dataset); // only added if not yet present
				}
			}

			return result;
		}

		private static bool ExcludeDependenciesWithinMultiValuedParameter(
			[NotNull] QualityCondition qualityCondition,
			[NotNull] string testParameterName)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));
			Assert.ArgumentNotNullOrEmpty(testParameterName, nameof(testParameterName));

			TestDescriptor testDescriptor = qualityCondition.TestDescriptor;
			if (testDescriptor == null)
			{
				return false;
			}

			ClassDescriptor testClass = testDescriptor.TestClass;

			if (testClass != null)
			{
				if (testClass.TypeName.EndsWith("Other", StringComparison.OrdinalIgnoreCase))
				{
					// naming convention: for "...Other" tests there is no dependency *within" each list parameter
					return true;
				}

				// TODO handle specific tests

				return false;
			}

			return false;
		}

		[NotNull]
		private static QualitySpecification Union(
			[NotNull] IEnumerable<QualitySpecification> qualitySpecifications)
		{
			Assert.ArgumentNotNull(qualitySpecifications, nameof(qualitySpecifications));

			IList<QualitySpecification> specifications = qualitySpecifications.ToList();

			Assert.ArgumentCondition(specifications.Count > 0,
			                         "Empty quality specification collection");

			if (specifications.Count == 1)
			{
				return Assert.NotNull(specifications[0], "collection item is null");
			}

			var result = new QualitySpecification(assignUuid: true);

			foreach (QualitySpecification spec in specifications)
			{
				Assert.NotNull(spec, "collection item is null");

				result = spec.Union(result);
			}

			result.Name = StringUtils.Concatenate(specifications, qspec => qspec.Name, ", ");

			return result;
		}
	}
}
