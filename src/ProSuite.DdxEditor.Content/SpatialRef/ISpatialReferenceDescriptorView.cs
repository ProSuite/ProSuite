using System.Windows.Forms;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.SpatialRef
{
	public interface ISpatialReferenceDescriptorView :
		IWrappedEntityControl<SpatialReferenceDescriptor>, IWin32Window
	{
		ISpatialReferenceDescriptorObserver Observer { get; set; }

		void RenderXmlString(string xmlString);

		void RenderSpatialReference([CanBeNull] ISpatialReference spatialReference);
	}
}
