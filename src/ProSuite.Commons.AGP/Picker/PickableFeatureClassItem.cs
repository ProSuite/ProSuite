using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using Geometry = ArcGIS.Core.Geometry.Geometry;

namespace ProSuite.Commons.AGP.Picker;

public class PickableFeatureClassItem : PropertyChangedBase,
                                        IPickableFeatureClassItem
{
	private BitmapImage _image;
	private readonly string _datasetName;
	private bool _selected;
	private readonly HashSet<long> _oids;

	public PickableFeatureClassItem([NotNull] string datasetName,
	                                [NotNull] IEnumerable<long> oids,
	                                [NotNull] Geometry geometry)
	{
		Assert.NotNullOrEmpty(datasetName);

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

	// TODO: Highlight features that are in the primary workspace (ProjectWorkspace)
	public bool Highlight => false;

	public bool Selected
	{
		get => _selected;
		set => SetProperty(ref _selected, value);
	}

	public string DisplayValue => ToString();

	public ImageSource ImageSource
	{
		get
		{
			if (_image != null)
			{
				return _image;
			}

			// todo: daro refactor, unknown image
			BasicFeatureLayer layer = Assert.NotNull(Layers.FirstOrDefault());

			_image = new BitmapImage(PickerUtils.GetImagePath(layer));

			return _image;
		}
	}

	public double Score { get; set; }

	public override string ToString()
	{
		return $"{_datasetName}: #{Oids.Count}";
	}
}
