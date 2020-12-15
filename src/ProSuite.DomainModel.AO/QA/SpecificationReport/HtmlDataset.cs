using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.QA.SpecificationReport
{
	public class HtmlDataset
	{
		[NotNull] private readonly Dataset _dataset;

		[NotNull] private readonly List<HtmlDatasetReference> _datasetReferences =
			new List<HtmlDatasetReference>();

		internal HtmlDataset([NotNull] Dataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));
			Assert.ArgumentNotNullOrEmpty(dataset.Name, "dataset name is not defined");

			_dataset = dataset;
			GeometryType = GetGeometryTypeName(dataset);
		}

		[NotNull]
		[UsedImplicitly]
		public string Name
		{
			get { return Assert.NotNullOrEmpty(_dataset.Name); }
		}

		[NotNull]
		[UsedImplicitly]
		public string GeometryType { get; private set; }

		[NotNull]
		[UsedImplicitly]
		public List<HtmlDatasetReference> DatasetReferences
		{
			get { return _datasetReferences; }
		}

		internal void AddReference([NotNull] HtmlDatasetReference datasetReference)
		{
			Assert.ArgumentNotNull(datasetReference, nameof(datasetReference));

			_datasetReferences.Add(datasetReference);
		}

		[NotNull]
		internal static string GetGeometryTypeName([NotNull] IDdxDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			return dataset is ITableDataset
				       ? "Table"
				       : dataset.GeometryType == null
					       ? "<undefined>"
					       : dataset.GeometryType.Name;
		}
	}
}
