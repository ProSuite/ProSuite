using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Xsl;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase.SchemaInfo;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.SpatialRef
{
	public partial class SpatialReferenceDescriptorControl : UserControl,
	                                                         ISpatialReferenceDescriptorView
	{
		private bool _allowNavigation;

		private ISpatialReferenceDescriptorObserver _observer;

		private static string _lastSelectedTabPage;

		/// <summary>
		/// Initializes a new instance of the <see cref="SpatialReferenceDescriptorControl"/> class.
		/// </summary>
		public SpatialReferenceDescriptorControl()
		{
			InitializeComponent();
		}

		ISpatialReferenceDescriptorObserver ISpatialReferenceDescriptorView.Observer
		{
			get { return _observer; }
			set { _observer = value; }
		}

		void IWrappedEntityControl<SpatialReferenceDescriptor>.OnBindingTo(
			SpatialReferenceDescriptor entity) { }

		void IWrappedEntityControl<SpatialReferenceDescriptor>.SetBinder(
			ScreenBinder<SpatialReferenceDescriptor> binder)
		{
			binder.Bind(m => m.Name)
			      .To(_textBoxName)
			      .WithLabel(_labelName);

			binder.Bind(m => m.Description)
			      .To(_textBoxDescription)
			      .WithLabel(_labelDescription);

			TabControlUtils.SelectTabPage(_tabControlProperties, _lastSelectedTabPage);
		}

		void IWrappedEntityControl<SpatialReferenceDescriptor>.OnBoundTo(
			SpatialReferenceDescriptor entity)
		{
			RenderXmlStringCore(entity.XmlString);

			if (StringUtils.IsNotEmpty(entity.XmlString))
			{
				try
				{
					RenderSpatialReference(entity.GetSpatialReference());
				}
				catch (Exception)
				{
					_tabPageProperties.Visible = false;
				}
			}
			else
			{
				_tabPageProperties.Visible = false;
			}
		}

		public void RenderSpatialReference(ISpatialReference spatialReference)
		{
			_propertyGridGeneral.SelectedObject =
				spatialReference == null
					? null
					: new SpatialReferenceProperties(spatialReference);

			_propertyGridGeneral.ExpandAllGridItems();

			_tabPageProperties.Visible = spatialReference != null;
		}

		void ISpatialReferenceDescriptorView.RenderXmlString(string xmlString)
		{
			RenderXmlStringCore(xmlString);
		}

		private void RenderXmlStringCore([CanBeNull] string xmlString)
		{
			_allowNavigation = true;

			_webBrowserXml.DocumentText = string.IsNullOrEmpty(xmlString)
				                              ? string.Empty
				                              : GetFormattedXml(xmlString);
		}

		[NotNull]
		private static string GetFormattedXml([NotNull] string xmlString)
		{
			Assert.ArgumentNotNullOrEmpty(xmlString, nameof(xmlString));

			var xslt = new XslCompiledTransform();

			var xdoc = new XmlDocument();
			xdoc.LoadXml(xmlString);

			using (var stream =
			       new MemoryStream(new UTF8Encoding().GetBytes(
				                        XmlPrettyPrint.XmlPrettyPrintStylesheet)))
			{
				xslt.Load(new XmlTextReader(stream));

				using (var writer = new StringWriter())
				{
					xslt.Transform(xdoc, new XmlTextWriter(writer));

					return writer.GetStringBuilder().ToString();
				}
			}
		}

		private void _buttonGetFromDataset_Click(object sender, EventArgs e)
		{
			_observer?.GetFromDatasetClicked();
		}

		private void _toolStripButtonCopy_Click(object sender, EventArgs e)
		{
			_observer?.CopyClicked();
		}

		private void _toolStripButtonImportFromFeatureClass_Click(object sender, EventArgs e)
		{
			_observer?.GetFromFeatureClassClicked();
		}

		private void _webBrowserXml_Navigating(object sender,
		                                       WebBrowserNavigatingEventArgs e)
		{
			// prevent navigation dynamically, NOT with AllowNavigation property, as this
			// permanently disallows changing the DocumentText of the control
			// (see http://blog.dynatrace.com/2009/03/04/how-to-use-the-webbrowser-control-to-render-custom-content/)

			if (! _allowNavigation)
			{
				e.Cancel = true;
			}
		}

		private void _webBrowserXml_Navigated(object sender, WebBrowserNavigatedEventArgs e)
		{
			_allowNavigation = false;
		}

		private void _tabControlProperties_SelectedIndexChanged(object sender, EventArgs e)
		{
			_lastSelectedTabPage = TabControlUtils.GetSelectedTabPageName(_tabControlProperties);
		}
	}
}
