using System;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.WorkList;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;

namespace ProSuite.AGP.QA.ProPlugins
{
	// TODO: Move OneClickToolBase to ProSuite.AGP as a shared project instead of using AGP.Editing
	public abstract class VerifyPerimeterToolBase : OneClickToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected VerifyPerimeterToolBase()
		{
			IsSketchTool = true;
			CompleteSketchOnMouseUp = true;
			RequiresSelection = false;

			//SelectionCursor = ToolUtils.GetCursor(Resources.AdvancedReshapeToolCursor);
			//SelectionCursorShift = ToolUtils.GetCursor(Resources.AdvancedReshapeToolCursorShift);

			Register();
		}

		private void Register()
		{
			VerificationPlugInController.GetInstance(SessionContext).Register(this);
		}

		protected abstract IVerificationSessionContext SessionContext { get; }

		protected abstract IWorkListOpener WorkListOpener { get; }

		protected virtual
			Func<IQualityVerificationResult, ErrorDeletionInPerimeter, bool, Task<int>>
			SaveAction => null;

		/// <summary>
		/// Returns the perimeter to verify, derived from the sketched perimeter, for example
		/// intersected with the current work unit.
		/// </summary>
		/// <param name="sketchedPerimeter">The perimeter sketched by the user.</param>
		/// <returns>The perimeter to verify, or null if nothing is to be verified. In that
		/// case the implementation has informed the user.</returns>
		[CanBeNull]
		protected virtual Geometry GetPerimeterToVerify([NotNull] Geometry sketchedPerimeter)
		{
			return sketchedPerimeter;
		}

		protected override Task OnToolActivateAsync(bool active)
		{
			return base.OnToolActivateAsync(active);
		}

		protected override bool OnToolActivatedCore(bool hasMapViewChanged)
		{
			SetupSketch();

			SketchType = SketchGeometryType.Rectangle;

			return base.OnToolActivatedCore(hasMapViewChanged);
		}

		protected override Task<bool> OnSketchCompleteCoreAsync(
			Geometry sketchGeometry,
			CancelableProgressor progressor)
		{
			return VerifyUtils.CreateCommandRunner(SessionContext, WorkListOpener, SaveAction)
			                  .VerifySketch(sketchGeometry, GetPerimeterToVerify);
		}

		protected override SketchGeometryType GetSelectionSketchGeometryType()
		{
			return SketchGeometryType.Rectangle;
		}

		protected override Task HandleEscapeAsync()
		{
			return Task.CompletedTask;
		}

		protected override void LogUsingCurrentSelection()
		{
			_msg.Info("Draw a box or press P and draw a polygon");
		}

		protected override void LogPromptForSelection()
		{
			_msg.Info("Draw a box or press P and draw a polygon");
		}
	}
}
