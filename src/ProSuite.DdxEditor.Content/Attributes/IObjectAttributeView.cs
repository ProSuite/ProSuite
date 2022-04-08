using System;
using System.Windows.Forms;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Attributes
{
	public interface IObjectAttributeView : IWrappedEntityControl<ObjectAttributeType>,
	                                        IWin32Window
	{
		Func<object> FindObjectAttributeTypeDelegate { get; set; }
	}
}