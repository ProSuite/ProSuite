using System.Windows.Forms;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.SpatialRef
{
	public class SpatialReferenceDescriptorPresenter :
		SimpleEntityItemPresenter<SpatialReferenceDescriptorItem>,
		ISpatialReferenceDescriptorObserver
	{
		private readonly ISpatialReferenceDescriptorView _view;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Initializes a new instance of the <see cref="SpatialReferenceDescriptorPresenter"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="view">The view.</param>
		public SpatialReferenceDescriptorPresenter(
			[NotNull] SpatialReferenceDescriptorItem item,
			[NotNull] ISpatialReferenceDescriptorView view)
			: base(item)
		{
			Assert.ArgumentNotNull(view, nameof(view));

			_view = view;

			view.Observer = this;
		}

		#region ISpatialReferenceDescriptorObserver Members

		public void GetFromDatasetClicked()
		{
			ISpatialReference spatialReference;
			string xmlString = Item.GetXmlStringFromDataset(_view, out spatialReference);

			if (string.IsNullOrEmpty(xmlString))
			{
				return;
			}

			_view.RenderXmlString(xmlString);
			_view.RenderSpatialReference(spatialReference);

			Item.SetXmlString(xmlString);
		}

		public void GetFromFeatureClassClicked()
		{
			ISpatialReference spatialReference;
			string xmlString = Item.GetXmlStringFromFeatureClass(_view, out spatialReference);

			if (string.IsNullOrEmpty(xmlString))
			{
				return;
			}

			_view.RenderXmlString(xmlString);
			_view.RenderSpatialReference(spatialReference);

			Item.SetXmlString(xmlString);
		}

		void ISpatialReferenceDescriptorObserver.CopyClicked()
		{
			string xmlString = Assert.NotNull(Item.GetEntity()).XmlString;

			if (string.IsNullOrEmpty(xmlString))
			{
				_msg.InfoFormat("No xml string defined");
			}
			else
			{
				Clipboard.SetDataObject(xmlString, true);

				_msg.InfoFormat("Xml string copied");
			}
		}

		#endregion
	}
}
