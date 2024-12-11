using System.Windows.Media;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;
using Geometry = ArcGIS.Core.Geometry.Geometry;

namespace ProSuite.Commons.AGP.Picker
{
	public abstract class PickableFeatureItemBase : PropertyChangedBase, IPickableFeatureItem
	{
		private readonly string _displayValue;
		private bool _selected;

		protected PickableFeatureItemBase(BasicFeatureLayer layer, Feature feature,
		                                  Geometry geometry, long oid, string displayValue)
		{
			Layer = layer;
			Feature = feature;
			Geometry = geometry;
			Oid = oid;
			_displayValue = displayValue;
		}

		[NotNull]
		public Feature Feature { get; }

		public long Oid { get; }

		[NotNull]
		public Geometry Geometry { get; }

		[NotNull]
		public BasicFeatureLayer Layer { get; }

		public bool Selected
		{
			get => _selected;
			set => SetProperty(ref _selected, value);
		}

		[NotNull]
		public string DisplayValue => ToString();

		public abstract ImageSource ImageSource { get; }

		public double Score { get; set; }

		public override string ToString()
		{
			// TODO: Alternatively allow using layer.QueryDisplayExpressions. But typically this is just the OID which is not very useful -> Requires configuration

			return Score >= double.Epsilon ? $"{_displayValue} - {Score}" : $"{_displayValue}";
		}
	}
}
