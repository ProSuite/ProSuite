namespace ProSuite.Commons.AGP.Carto;

public class SymbolDisplaySettings
{
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
			AutoSwitch = settings.AutoSwitch;
			AutoMinScaleDenom = settings.AutoMinScaleDenom;
			AutoMaxScaleDenom = settings.AutoMaxScaleDenom;
			NoMaskingWithoutSLD = settings.NoMaskingWithoutSLD;
		}
	}

	public void Reset()
	{
		AutoSwitch = false;
		AutoMinScaleDenom = 0;
		AutoMaxScaleDenom = 0;
		NoMaskingWithoutSLD = false;
	}
}
