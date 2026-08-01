using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.CreateBufferedLine
{
	[UsedImplicitly]
	public class CreateBufferedLineToolOptions
		: BufferedLineToolOptionsBase<PartialCreateBufferedLineOptions>
	{
		public CreateBufferedLineToolOptions(
			[CanBeNull] PartialCreateBufferedLineOptions centralOptions,
			[CanBeNull] PartialCreateBufferedLineOptions localOptions)
			: base(centralOptions, localOptions) { }

		public override string GetLocalOverridesMessage()
		{
			const string optionsName = "Create Buffered Line Options";
			return GetLocalOverridesMessage(optionsName);
		}
	}
}
