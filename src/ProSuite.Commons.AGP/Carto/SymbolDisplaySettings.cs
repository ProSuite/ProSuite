namespace ProSuite.Commons.AGP.Carto;

public class SymbolDisplaySettings
{
	public bool? WantSLD { get; set; }
	public bool? WantLM { get; set; }

	public bool AutoSwitch { get; set; }
	public double AutoMinScaleDenom { get; set; }
	public double AutoMaxScaleDenom { get; set; }

	public bool NoMaskingWithoutSLD { get; set; }

	public SymbolDisplaySettings() { }

	public SymbolDisplaySettings(SymbolDisplaySettings settings)
	{
		if (settings is null)
		{
			Reset();
		}
		else
		{
			WantSLD = null; // do not copy
			WantLM = null; // do not copy
			AutoSwitch = settings.AutoSwitch;
			AutoMinScaleDenom = settings.AutoMinScaleDenom;
			AutoMaxScaleDenom = settings.AutoMaxScaleDenom;
			NoMaskingWithoutSLD = settings.NoMaskingWithoutSLD;
		}
	}

	public void Reset()
	{
		WantSLD = null;
		WantLM = null;
		AutoSwitch = false;
		AutoMinScaleDenom = 0;
		AutoMaxScaleDenom = 0;
		NoMaskingWithoutSLD = false;
	}
}
