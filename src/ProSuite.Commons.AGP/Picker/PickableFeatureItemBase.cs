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

		protected PickableFeatureItemBase([NotNull] BasicFeatureLayer layer,
		                                  [NotNull] Feature feature,
		                                  [NotNull] Geometry geometry,
		                                  long oid,
		                                  string displayValue)
		{
			Layer = layer;
			Feature = feature;
			Geometry = geometry;
			Oid = oid;
			_displayValue = displayValue;
		}

		public Feature Feature { get; }

		public long Oid { get; }

		public Geometry Geometry { get; }

		public BasicFeatureLayer Layer { get; }

		// TODO: Highlight features that are in the primary workspace (ProjectWorkspace)
		public bool Highlight => false;

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
			// TODO: Alternatively allow using layer.QueryDisplayExpressions. But typically this is just the OID which is not very useful -> Requires configuration

			return Score >= double.Epsilon ? $"{_displayValue} - {Score}" : $"{_displayValue}";
		}
	}
}
