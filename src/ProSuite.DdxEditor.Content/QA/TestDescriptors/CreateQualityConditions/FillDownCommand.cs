using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors.CreateQualityConditions
{
	internal class FillDownCommand : CommandBase
	{
		private readonly CellSelection _cellSelection;
		private readonly ICreateQualityConditionsFillDown _fillDown;

		public FillDownCommand([NotNull] CellSelection cellSelection,
		                       [NotNull] ICreateQualityConditionsFillDown fillDown)
		{
			_cellSelection = cellSelection;
			_fillDown = fillDown;
		}

		public override string Text => "Fill down";

		public override Image Image => Resources.FillDown;

		protected override void ExecuteCore()
		{
			_fillDown.FillDown(_cellSelection);
		}
	}
}
