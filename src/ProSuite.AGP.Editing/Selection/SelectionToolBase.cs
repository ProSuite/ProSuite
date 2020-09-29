using System.Collections.Generic;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing.Selection
{
	public class SelectionToolBase: OneClickToolBase
	{
		public SelectionToolBase()
		{
			IsSketchTool = true;
			SelectionSettings = new SelectionSettings();
			SelectionMode = SelectionMode.Normal;
		}

		protected override bool CanUseSelection(IEnumerable<Feature> selectedFeatures)
		{
			return false;
		}

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private SelectionSettings _selectionSettings;
		
		protected override bool IsInSelectionPhase()
		{
			return true;
		}

		protected override void OnKeyDownCore(MapViewKeyEventArgs k)
		{
			if (k.Key == Key.LeftCtrl || k.Key == Key.RightCtrl)
			{
				SelectionMode = SelectionMode.UserSelect;
			}
			if (k.Key == Key.LeftAlt || k.Key == Key.RightAlt)
			{
				SelectionMode = SelectionMode.Original;
			}
			_msg.Info($"Key {k.Key} was pressed. SelectionMode: '{SelectionMode}'");
		}

		//protected override void OnKeyUpCore(MapViewKeyEventArgs k)
		//{
		//	if (k.Key == Key.LeftCtrl || k.Key == Key.RightCtrl || k.Key == Key.LeftAlt || k.Key == Key.RightAlt)
		//	{
		//		SelectionMode = SelectionMode.Normal;
		//	}
		//}

		protected override bool HandleEscape()
		{
			// throw new NotImplementedException();
			return true;
		}

		protected override void LogUsingCurrentSelection()
		{
			// throw new NotImplementedException();
		}

		
		protected override void LogPromptForSelection()
		{
			_msg.InfoFormat("Select features by clicking or dragging a box");
		}

		protected override SelectionSettings SelectionSettings
		{
			get => _selectionSettings;
			set => _selectionSettings = value;
		}
	}
}
