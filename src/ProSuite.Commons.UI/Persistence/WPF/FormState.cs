using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.Persistence.WPF
{
	/// <remarks>
	/// Stores form (window/dialog) state for serialization.
	/// Can be subclassed to add your own custom form state.
	/// Must have a public parameterless constructor!
	/// </remarks>
	[UsedImplicitly]
	public class FormState : IFormState
	{
		public bool HasLocation => !double.IsNaN(Left) && !double.IsNaN(Top);
		public double Left { get; set; } = double.NaN;
		public double Top { get; set; } = double.NaN;

		public bool HasSize => !double.IsNaN(Width) && !double.IsNaN(Height);
		public double Width { get; set; } = double.NaN;
		public double Height { get; set; } = double.NaN;

		public bool Topmost { get; set; }
		public bool IsMaximized { get; set; }
	}
}
