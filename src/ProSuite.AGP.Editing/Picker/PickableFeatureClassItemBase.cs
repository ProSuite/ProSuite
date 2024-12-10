using System.Collections.Generic;
using System.Windows.Media;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Picker;
using ProSuite.Commons.Essentials.CodeAnnotations;
using Geometry = ArcGIS.Core.Geometry.Geometry;

namespace ProSuite.AGP.Editing.Picker
{
	public abstract class PickableFeatureClassItemBase : PropertyChangedBase,
	                                                     IPickableFeatureClassItem
	{
		private readonly string _datasetName;
		private bool _selected;
		
		protected PickableFeatureClassItemBase([NotNull] string datasetName,
		                                       [NotNull] IReadOnlyList<long> oids, Geometry geometry)
		{
			_datasetName = datasetName;
			Oids = oids;
			Geometry = geometry;
		}

		public IReadOnlyList<long> Oids { get; }
		
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
