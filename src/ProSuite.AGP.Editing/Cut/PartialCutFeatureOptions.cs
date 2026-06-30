using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.Cut;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public class PartialCutFeatureOptions : PartialOptionsBase
{
	[CanBeNull]
	public OverridableSetting<ChangeAlongZSource> ZValueSource { get; set; }

	[CanBeNull]
	public List<DatasetSpecificValue<ChangeAlongZSource>> DatasetSpecificZSource { get; set; }

	public override PartialOptionsBase Clone()
	{
		var result = new PartialCutFeatureOptions
		             {
			             ZValueSource = TryClone(ZValueSource)
		             };

		if (DatasetSpecificZSource != null)
		{
			result.DatasetSpecificZSource =
				new List<DatasetSpecificValue<ChangeAlongZSource>>();

			result.DatasetSpecificZSource.AddRange(
				DatasetSpecificZSource.Select(dsz => dsz.Clone()));
		}

		return result;
	}
}
