using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;
using Geometry = ArcGIS.Core.Geometry.Geometry;

namespace ProSuite.Commons.AGP.Picker
{
	public abstract class PickableFeatureClassItemBase : PropertyChangedBase,
	                                                     IPickableFeatureClassItem
	{
		private readonly string _datasetName;
		private bool _selected;
		private readonly HashSet<long> _oids;

		protected PickableFeatureClassItemBase([NotNull] string datasetName,
		                                       [NotNull] IEnumerable<long> oids,
		                                       Geometry geometry)
		{
			_datasetName = datasetName;
			_oids = oids.ToHashSet();
			Geometry = geometry;
		}

		public ICollection<long> Oids => _oids;

		public void AddOids(IEnumerable<long> oids)
		{
			foreach (long oid in oids)
			{
				_oids.Add(oid);
			}
		}

		public Geometry Geometry { get; }

		public List<BasicFeatureLayer> Layers { get; } = new();

		public bool Selected
		{
			get => _selected;
			set => SetProperty(ref _selected, value);
		}

		public string DisplayValue => ToString();

		public abstract ImageSource ImageSource { get; }

		public double Score { get; set; }

		public override string ToString()
		{
			return $"{_datasetName}: #{Oids.Count}";
		}
	}
}
