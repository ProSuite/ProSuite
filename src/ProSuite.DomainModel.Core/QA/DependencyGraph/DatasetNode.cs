using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.QA.DependencyGraph
{
	public class DatasetNode : IEquatable<DatasetNode>
	{
		[NotNull] private readonly Dataset _dataset;

		[NotNull] private readonly List<DatasetDependency> _outgoingDependencies =
			new List<DatasetDependency>();

		[NotNull] private readonly List<DatasetDependency> _incomingDependencies =
			new List<DatasetDependency>();

		public DatasetNode([NotNull] Dataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			_dataset = dataset;
			NodeId = DependencyGraphUtils.GetDatasetId(dataset);
		}

		[NotNull]
		public string NodeId { get; private set; }

		[NotNull]
		public Dataset Dataset
		{
			get { return _dataset; }
		}

		public override string ToString()
		{
			return string.Format("Dataset: {0}", _dataset);
		}

		public bool Equals(DatasetNode other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return _dataset.Equals(other._dataset);
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

			return Equals((DatasetNode) obj);
		}

		public override int GetHashCode()
		{
			return _dataset.GetHashCode();
		}

		public void AddOutgoingDependency([NotNull] DatasetDependency dependency)
		{
			_outgoingDependencies.Add(dependency);
		}

		public void AddIncomingDependency([NotNull] DatasetDependency dependency)
		{
			_incomingDependencies.Add(dependency);
		}

		[NotNull]
		public IEnumerable<DatasetDependency> OutgoingDependencies
		{
			get { return _outgoingDependencies; }
		}

		public int OutgoingDependenciesCount
		{
			get { return _outgoingDependencies.Count; }
		}

		[NotNull]
		public IEnumerable<DatasetDependency> IncomingDependencies
		{
			get { return _incomingDependencies; }
		}

		public int IncomingDependenciesCount
		{
			get { return _incomingDependencies.Count; }
		}
	}
}
