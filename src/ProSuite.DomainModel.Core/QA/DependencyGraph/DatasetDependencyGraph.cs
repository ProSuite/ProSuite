using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainModel.Core.QA.DependencyGraph
{
	public class DatasetDependencyGraph
	{
		[NotNull] private readonly Dictionary<Dataset, DatasetNode> _datasetNodes;

		[NotNull] private readonly Dictionary<DdxModel, ModelNode> _modelNodes =
			new Dictionary<DdxModel, ModelNode>();

		[NotNull] private readonly List<DatasetDependency> _datasetDependencies =
			new List<DatasetDependency>();

		public DatasetDependencyGraph([NotNull] IEnumerable<Dataset> datasets,
		                              [CanBeNull] string description = null)
		{
			Assert.ArgumentNotNull(datasets, nameof(datasets));

			Description = description;

			_datasetNodes = datasets.ToDictionary(
				ds => ds,
				ds => new DatasetNode(ds));

			foreach (DatasetNode datasetNode in _datasetNodes.Values)
			{
				DdxModel model = datasetNode.Dataset.Model;
				ModelNode modelNode;
				if (! _modelNodes.TryGetValue(model, out modelNode))
				{
					modelNode = new ModelNode(model);
					_modelNodes.Add(model, modelNode);
				}

				modelNode.AddDatasetNode(datasetNode);
			}
		}

		public void AddDependency([NotNull] QualitySpecificationElement element,
		                          [NotNull] Dataset fromDataset,
		                          [NotNull] Dataset toDataset,
		                          [NotNull] string fromParameterName,
		                          [NotNull] string toParameterName,
		                          [CanBeNull] string fromFilterExpression = null,
		                          [CanBeNull] string toFilterExpression = null,
		                          bool directed = true)
		{
			DatasetNode fromNode = _datasetNodes[fromDataset];
			DatasetNode toNode = _datasetNodes[toDataset];

			var dependency = new DatasetDependency(element,
			                                       fromNode, toNode,
			                                       fromParameterName, toParameterName,
			                                       fromFilterExpression, toFilterExpression,
			                                       directed);

			fromNode.AddOutgoingDependency(dependency);
			toNode.AddIncomingDependency(dependency);

			_datasetDependencies.Add(dependency);
		}

		public string Description { get; private set; }

		[NotNull]
		public IEnumerable<ModelNode> ModelNodes
		{
			get { return _modelNodes.Values; }
		}

		[NotNull]
		public IEnumerable<DatasetNode> DatasetNodes
		{
			get { return _datasetNodes.Values; }
		}

		[NotNull]
		public IEnumerable<DatasetDependency> DatasetDependencies
		{
			get { return _datasetDependencies; }
		}
	}
}
