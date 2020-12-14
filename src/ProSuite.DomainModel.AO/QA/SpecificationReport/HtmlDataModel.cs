using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.QA.SpecificationReport
{
	public class HtmlDataModel
	{
		[NotNull] private readonly DdxModel _model;

		[NotNull] private readonly IDictionary<Dataset, HtmlDataset> _datasets =
			new Dictionary<Dataset, HtmlDataset>();

		[CanBeNull] private List<HtmlDataset> _sortedDatasets;

		public HtmlDataModel([NotNull] DdxModel model)
		{
			Assert.ArgumentNotNull(model, nameof(model));
			Assert.ArgumentNotNullOrEmpty(model.Name, "model name is not defined");

			_model = model;
		}

		[NotNull]
		[UsedImplicitly]
		public string Name
		{
			get { return Assert.NotNullOrEmpty(_model.Name); }
		}

		[NotNull]
		[UsedImplicitly]
		public List<HtmlDataset> Datasets
		{
			get
			{
				return _sortedDatasets ??
				       (_sortedDatasets = _datasets.Values
				                                   .OrderBy(d => d.Name)
				                                   .ToList());
			}
		}

		[NotNull]
		internal HtmlDataset GetHtmlDataset([NotNull] Dataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			HtmlDataset result;
			if (! _datasets.TryGetValue(dataset, out result))
			{
				result = new HtmlDataset(dataset);
				_datasets.Add(dataset, result);
				_sortedDatasets = null;
			}

			return result;
		}
	}
}
