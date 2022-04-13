using System;
using System.Windows.Forms;
using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.Attributes
{
	public interface IObjectAttributeView : IWrappedEntityControl<ObjectAttributeType>,
	                                        IWin32Window
	{
		Func<object> FindObjectAttributeTypeDelegate { get; set; }
	}
}
