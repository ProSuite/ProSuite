using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.QA.DependencyGraph
{
	public class ModelNode : IEquatable<ModelNode>
	{
		private readonly DdxModel _model;
		private readonly HashSet<DatasetNode> _datasetNodes = new HashSet<DatasetNode>();

		public ModelNode([NotNull] DdxModel model)
		{
			_model = model;
			NodeId = model.Name;
		}

		[NotNull]
		public DdxModel Model
		{
			get { return _model; }
		}

		[NotNull]
		public string NodeId { get; private set; }

		public void AddDatasetNode([NotNull] DatasetNode datasetNode)
		{
			_datasetNodes.Add(datasetNode);
		}

		[NotNull]
		public IEnumerable<DatasetNode> DatasetNodes
		{
			get { return _datasetNodes; }
		}

		public bool Equals(ModelNode other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return _model.Equals(other._model);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((ModelNode) obj);
		}

		public override int GetHashCode()
		{
			return _model.GetHashCode();
		}
	}
}
