using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Persistence.WPF;

namespace ProSuite.UI.Core.QA.VerificationProgress
{
	[UsedImplicitly]
	public class VerificationProgressFormState : FormState
	{
		[XmlAttribute("showProgressOverlay")]
		public bool ShowProgressOverlay { get; set; }
	}
}
