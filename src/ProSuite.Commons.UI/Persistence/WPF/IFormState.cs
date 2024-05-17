namespace ProSuite.Commons.UI.Persistence.WPF
{
	public interface IFormState
	{
		double Left { get; set; }
		double Top { get; set; }
		double Width { get; set; }
		double Height { get; set; }

		bool Topmost { get; set; }
		bool IsMaximized { get; set; }
	}
}

