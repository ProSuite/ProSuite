using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.CodeAnnotations;
using Geometry = ArcGIS.Core.Geometry.Geometry;

namespace ProSuite.AGP.Editing.Picker
{
	public abstract class PickableFeatureClassItemBase : PropertyChangedBase,
	                                                     IPickableFeatureClassItem
	{
		private readonly string _datasetName;
		private bool _selected;

		/// <summary>
		/// Has to be called on MCT
		/// </summary>
		protected PickableFeatureClassItemBase([NotNull] Dataset dataset,
		                                       [NotNull] IReadOnlyList<Feature> features)
		{
			_datasetName = dataset.GetName();
			Oids = features.Select(feature => feature.GetObjectID()).ToList();
			Geometry = GeometryUtils.Union(features.Select(feature => feature.GetShape()).ToList());
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
