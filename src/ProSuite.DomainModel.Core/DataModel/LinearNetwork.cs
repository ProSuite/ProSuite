using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class LinearNetwork : VersionedEntityWithMetadata, INamed, IAnnotated
	{
		[UsedImplicitly] private string _name;
		[UsedImplicitly] private string _description;

		private readonly IList<LinearNetworkDataset> _networkDatasets =
			new List<LinearNetworkDataset>();

		[UsedImplicitly] private double _customTolerance;
		[UsedImplicitly] private bool _enforceFlowDirection;

		public LinearNetwork() { }

		public LinearNetwork(string name)
		{
			_name = name;
		}

		public LinearNetwork(string name,
		                     [NotNull] IEnumerable<LinearNetworkDataset> networkDatasets)
			: this(name)
		{
			_networkDatasets = networkDatasets.ToList();
		}

		[Required]
		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}

		[UsedImplicitly]
		public string Description
		{
			get { return _description; }
			set { _description = value; }
		}

		public IReadOnlyCollection<LinearNetworkDataset> NetworkDatasets =>
			new ReadOnlyCollection<LinearNetworkDataset>(_networkDatasets.ToList());

		public int ModelId
		{
			get
			{
				int result = -1;

				foreach (var dataset in _networkDatasets.Select(nd => nd.Dataset))
				{
					if (result < 0)
					{
						result = dataset.Model.Id;
					}
					else
					{
						Assert.AreEqual(result, dataset.Model.Id,
						                "The network {0} contains datasets from different models",
						                Name);
					}
				}

				return result;
			}
		}

		public double CustomTolerance
		{
			get { return _customTolerance; }
			set { _customTolerance = value; }
		}

		public bool EnforceFlowDirection
		{
			get { return _enforceFlowDirection; }
			set { _enforceFlowDirection = value; }
		}

		public LinearNetworkDataset DefaultJunctionDataset =>
			_networkDatasets.FirstOrDefault(d => d.IsDefaultJunction);

		public void AddNetworkDataset(LinearNetworkDataset linearNetworkDataset)
		{
			Assert.ArgumentNotNull(linearNetworkDataset, nameof(linearNetworkDataset));

			if (_networkDatasets.Count > 0 && linearNetworkDataset.Dataset.Model.Id != ModelId)
			{
				throw new ArgumentException(
					"Cannot add a dataset to the network from a different model than the existing datasets.");
			}

			_networkDatasets.Add(linearNetworkDataset);
		}

		public bool RemoveNetworkDataset([NotNull] VectorDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			LinearNetworkDataset linearNetworkDataset =
				_networkDatasets.FirstOrDefault(ds => ds.Dataset.Equals(dataset));
			if (linearNetworkDataset != null)
			{
				_networkDatasets.Remove(linearNetworkDataset);
				return true;
			}

			return false;
		}

		public void ClearNetworkDatasets()
		{
			_networkDatasets.Clear();
		}

		protected bool Equals(LinearNetwork other)
		{
			return _name == other._name;
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

			return Equals((LinearNetwork) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return _name?.GetHashCode() ?? 0;
			}
		}
	}
}
