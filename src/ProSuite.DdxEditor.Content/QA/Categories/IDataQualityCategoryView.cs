using System;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.QA.Categories
{
	public interface IDataQualityCategoryView :
		IWrappedEntityControl<DataQualityCategory>, IWin32Window
	{
		[CanBeNull]
		IDataQualityCategoryObserver Observer { get; set; }

		Func<object> FindDefaultModelDelegate { get; set; }
	}
}
