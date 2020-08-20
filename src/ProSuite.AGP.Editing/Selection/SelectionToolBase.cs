using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing.Selection
{
	public class SelectionToolBase: OneClickToolBase
	{
		public SelectionToolBase()
		{
			IsSketchTool = true;
		}

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		protected override bool IsInSelectionPhase()
		{
			return true;
		}

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

		
	}
}
