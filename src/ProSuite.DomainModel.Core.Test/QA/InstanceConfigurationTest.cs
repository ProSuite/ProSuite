using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainModel.Core.Test.QA
{
	[TestFixture]
	public class InstanceConfigurationTest
	{
		[Test]
		public void CanGetDatasetParameterValuesOfSelfReferencingTransformer()
		{
			var dataset = new ErrorLineDataset("Roads");

			TransformerConfiguration transformer = CreateTransformer("T");
			AddDatasetParameter(transformer, "featureClass", dataset);

			// Circular references must not exist, but they can be created by editing:
			AddTransformerParameter(transformer, "other", transformer);

			List<Dataset> datasets =
				transformer.GetDatasetParameterValues(includeSourceDatasets: true).ToList();

			Assert.AreEqual(new[] { dataset }, datasets);
		}

		[Test]
		public void CanGetDatasetParameterValuesOfMutuallyReferencingTransformers()
		{
			var dataset0 = new ErrorLineDataset("Roads");
			var dataset1 = new ErrorLineDataset("Rails");

			TransformerConfiguration transformer0 = CreateTransformer("T0");
			TransformerConfiguration transformer1 = CreateTransformer("T1");

			AddDatasetParameter(transformer0, "featureClass", dataset0);
			AddTransformerParameter(transformer0, "other", transformer1);

			AddDatasetParameter(transformer1, "featureClass", dataset1);
			AddTransformerParameter(transformer1, "other", transformer0);

			List<Dataset> datasets =
				transformer0.GetDatasetParameterValues(includeSourceDatasets: true).ToList();

			Assert.AreEqual(new[] { dataset0, dataset1 }, datasets);
		}

		[Test]
		public void CanGetDatasetParameterValuesOfReferencedProcessorsWithCircularReference()
		{
			var dataset0 = new ErrorLineDataset("Roads");
			var dataset1 = new ErrorLineDataset("Rails");

			TransformerConfiguration transformer0 = CreateTransformer("T0");
			TransformerConfiguration transformer1 = CreateTransformer("T1");

			AddDatasetParameter(transformer0, "featureClass", dataset0);
			AddTransformerParameter(transformer0, "other", transformer1);

			AddDatasetParameter(transformer1, "featureClass", dataset1);
			AddTransformerParameter(transformer1, "other", transformer0);

			List<Dataset> datasets =
				transformer0.GetDatasetParameterValues(includeReferencedProcessors: true).ToList();

			Assert.AreEqual(new[] { dataset0, dataset1 }, datasets);
		}

		[Test]
		public void CanGetDatasetParameterValuesOfRepeatedlyReferencedTransformer()
		{
			// The same transformer referenced twice is not a circular reference:
			var dataset = new ErrorLineDataset("Roads");

			TransformerConfiguration referenced = CreateTransformer("T1");
			AddDatasetParameter(referenced, "featureClass", dataset);

			TransformerConfiguration transformer = CreateTransformer("T0");
			AddTransformerParameter(transformer, "first", referenced);
			AddTransformerParameter(transformer, "second", referenced);

			List<Dataset> datasets =
				transformer.GetDatasetParameterValues(includeSourceDatasets: true).ToList();

			Assert.AreEqual(new[] { dataset, dataset }, datasets);
		}

		[NotNull]
		private static TransformerConfiguration CreateTransformer([NotNull] string name)
		{
			var descriptor = new TransformerDescriptor(
				$"{name}Descriptor", new ClassDescriptor(typeof(InstanceConfigurationTest)), 0);

			return new TransformerConfiguration(name, descriptor);
		}

		private static void AddDatasetParameter(
			[NotNull] InstanceConfiguration configuration,
			[NotNull] string parameterName,
			[NotNull] Dataset dataset)
		{
			configuration.AddParameterValue(
				new DatasetTestParameterValue(parameterName, typeof(Dataset))
				{
					DatasetValue = dataset
				});
		}

		private static void AddTransformerParameter(
			[NotNull] InstanceConfiguration configuration,
			[NotNull] string parameterName,
			[NotNull] TransformerConfiguration valueSource)
		{
			configuration.AddParameterValue(
				new DatasetTestParameterValue(parameterName, typeof(Dataset))
				{
					ValueSource = valueSource
				});
		}
	}
}
