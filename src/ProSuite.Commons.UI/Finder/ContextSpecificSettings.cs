using System.ComponentModel;

namespace ProSuite.Commons.UI.Finder
{
	public class ContextSpecificSettings
	{
		[DefaultValue(null)]
		public string FindText { get; set; }

		[DefaultValue(null)]
		public string FinderQueryId { get; set; }
	}
}
