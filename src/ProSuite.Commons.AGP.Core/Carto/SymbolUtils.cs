using System;
using System.Collections.Generic;
using ArcGIS.Core.CIM;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.CodeAnnotations;
using Geometry = ArcGIS.Core.Geometry.Geometry;

namespace ProSuite.Commons.AGP.Core.Carto
{
	public static class SymbolUtils
	{
		public enum MarkerStyle
		{
			Circle,
			Square,
			Cross
		}

		public enum FillStyle
		{
			Null,
			Solid,
			ForwardDiagonal,
			BackwardDiagonal,
			Horizontal,
			Vertical,
			Cross,
			DiagonalCross
		}

		public static CIMColor DefaultFillColor => ColorUtils.GrayRGB;
		public static CIMColor DefaultStrokeColor => ColorUtils.BlackRGB;
		public const double DefaultStrokeWidth = 1.0;

		public static CIMMarker CreateMarker(CIMColor color, double size, MarkerStyle style)
		{
			var geometry = CreateMarkerGeometry(style);
			var symbol = CreatePolygonSymbol(color);

			var graphic = new CIMMarkerGraphic {Geometry = geometry, Symbol = symbol};

			var marker = new CIMVectorMarker();
			marker.ColorLocked = false;
			marker.Enable = true;
			marker.Size = size;
			marker.AnchorPointUnits = SymbolUnits.Relative;
			marker.BillboardMode3D = BillboardMode.FaceNearPlane;
			marker.DominantSizeAxis3D = DominantSizeAxis.Y;
			marker.ScaleSymbolsProportionally = true;
			marker.RespectFrame = true;
			marker.MarkerGraphics = new[] {graphic};
			marker.Frame = style == MarkerStyle.Circle
				               ? GeometryUtils.CreateEnvelope(-5, -5, 5, 5)
				               : graphic.Geometry.Extent;

			return marker;
		}

		public static Geometry CreateMarkerGeometry(MarkerStyle style)
		{
			switch (style)
			{
				case MarkerStyle.Circle:
					return GeometryUtils.CreateBezierCircle(5);

				case MarkerStyle.Square:
					var envelope = GeometryUtils.CreateEnvelope(-5, -5, 5, 5);
					return GeometryUtils.CreatePolygon(envelope);

				default:
					throw new NotImplementedException(
						"Sorry, this MarkerStyle is not yet implemented");
			}
		}

		public static CIMPolygonSymbol CreatePolygonSymbol(
			CIMColor color = null, FillStyle fillStyle = FillStyle.Solid, CIMStroke outline = null)
		{
			var symbolLayers = new List<CIMSymbolLayer>();

			if (outline != null)
			{
				symbolLayers.Add(outline);
			}

			switch (fillStyle)
			{
				case FillStyle.Null:
					break;
				case FillStyle.Solid:
					symbolLayers.Add(CreateSolidFill(color));
					break;
				case FillStyle.BackwardDiagonal:
					symbolLayers.Add(CreateHatchFill(-45.0, 5.3, color));
					break;
				case FillStyle.ForwardDiagonal:
					symbolLayers.Add(CreateHatchFill(45.0, 5.3, color));
					break;
				case FillStyle.DiagonalCross:
					symbolLayers.Add(CreateHatchFill(45.0, 5.3, color));
					symbolLayers.Add(CreateHatchFill(-45.0, 5.3, color));
					break;
				case FillStyle.Horizontal:
					symbolLayers.Add(CreateHatchFill(0.0, 7.5, color));
					break;
				case FillStyle.Vertical:
					symbolLayers.Add(CreateHatchFill(90.0, 7.5, color));
					break;
				case FillStyle.Cross:
					symbolLayers.Add(CreateHatchFill(0.0, 7.5, color));
					symbolLayers.Add(CreateHatchFill(90.0, 7.5, color));
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(fillStyle));
			}

			var symbol = new CIMPolygonSymbol();
			symbol.SymbolLayers = symbolLayers.ToArray();
			symbol.Effects = null; // no global effects
			return symbol;
		}

		public static CIMLineSymbol CreateLineSymbol(CIMColor color = null, double width = -1)
		{
			var symbol = new CIMLineSymbol();
			symbol.SymbolLayers = new CIMSymbolLayer[] {CreateSolidStroke(color, width)};
			symbol.Effects = null; // no global effects
			return symbol;
		}

		public static CIMStroke CreateSolidStroke(CIMColor color, double width = -1)
		{
			var solidStroke = new CIMSolidStroke();
			solidStroke.Color = color ?? DefaultStrokeColor;
			solidStroke.ColorLocked = false;
			solidStroke.CapStyle = LineCapStyle.Round;
			solidStroke.JoinStyle = LineJoinStyle.Round;
			solidStroke.MiterLimit = 10.0;
			solidStroke.Width = width >= 0 ? width : DefaultStrokeWidth;
			solidStroke.Enable = true;
			return solidStroke;
		}

		public static CIMFill CreateSolidFill(CIMColor color = null)
		{
			var solidFill = new CIMSolidFill();
			solidFill.Color = color ?? DefaultFillColor;
			solidFill.ColorLocked = false;
			solidFill.Enable = true;
			return solidFill;
		}

		public static CIMFill CreateHatchFill(double angleDegrees, double separation, CIMColor color = null)
		{
			var hatchFill = new CIMHatchFill();
			hatchFill.Enable = true;
			hatchFill.Rotation = angleDegrees;
			hatchFill.Separation = separation;
			hatchFill.LineSymbol = CreateLineSymbol(color);
			return hatchFill;
		}

		#region Marker Placements

		public static T SetMarkerPlacement<T>(this T marker, [CanBeNull] CIMMarkerPlacement placement) where T : CIMMarker
		{
			if (marker == null)
				throw new ArgumentNullException(nameof(marker));

			marker.MarkerPlacement = placement;

			return marker;
		}

		public static T SetMarkerPlacementAlongLine<T>(this T marker, params double[] template) where T : CIMMarker
		{
			var placement = new CIMMarkerPlacementAlongLineSameSize();
			placement.PlacementTemplate = template ?? new[] {10 * DefaultStrokeWidth};

			return marker.SetMarkerPlacement(placement);
		}

		public static T SetMarkerPlacementAtExtremities<T>(this T marker, ExtremityPlacement extremities, double offsetAlongLine = 0.0) where T : CIMMarker
		{
			var placement = new CIMMarkerPlacementAtExtremities();
			placement.ExtremityPlacement = extremities;
			placement.OffsetAlongLine = offsetAlongLine;

			return marker.SetMarkerPlacement(placement);
		}

		public static T SetMarkerEndings<T>(this T marker, PlacementEndings endings, double customOffset = 0.0) where T : CIMMarker
		{
			if (marker.MarkerPlacement is CIMMarkerPlacementAlongLine placement)
			{
				placement.Endings = endings;
				placement.CustomEndingOffset = customOffset;
			}
			else
			{
				throw IncompatibleMarkerPlacement();
			}

			return marker;
		}

		public static T SetMarkerControlPointEndings<T>(this T marker, PlacementEndings endings) where T : CIMMarker
		{
			if (marker.MarkerPlacement is CIMMarkerPlacementAlongLine placement)
			{
				placement.ControlPointPlacement = endings;
			}
			else
			{
				throw IncompatibleMarkerPlacement();
			}

			return marker;
		}

		public static T SetMarkerOffsetAlongLine<T>(this T marker, double offsetAlongLine) where T : CIMMarker
		{
			switch (marker.MarkerPlacement)
			{
				case CIMMarkerPlacementAlongLine alongLine:
					alongLine.OffsetAlongLine = offsetAlongLine;
					break;
				case CIMMarkerPlacementAtExtremities atExtremities:
					atExtremities.OffsetAlongLine = offsetAlongLine;
					break;
				default:
					throw IncompatibleMarkerPlacement();
			}

			return marker;
		}

		public static T SetMarkerAngleToLine<T>(this T marker, bool angleToLine) where T : CIMMarker
		{
			if (marker.MarkerPlacement is CIMMarkerStrokePlacement placement)
			{
				placement.AngleToLine = angleToLine;
			}
			else
			{
				throw IncompatibleMarkerPlacement();
			}

			return marker;
		}

		public static T SetMarkerPerpendicularOffset<T>(this T marker, double offset) where T : CIMMarker
		{
			if (marker.MarkerPlacement is CIMMarkerStrokePlacement placement)
			{
				placement.Offset = offset;
			}
			else
			{
				throw IncompatibleMarkerPlacement();
			}

			return marker;
		}

		private static InvalidOperationException IncompatibleMarkerPlacement()
		{
			return new InvalidOperationException("Incompatible Marker Placement");
		}

		#endregion

		#region Geometric Effects

		public static T SetEffects<T>(this T layer, params CIMGeometricEffect[] effects) where T : CIMSymbolLayer
		{
			if (layer == null)
				throw new ArgumentNullException(nameof(layer));

			layer.Effects = effects ?? new CIMGeometricEffect[0];

			return layer;
		}

		public static T AddEffect<T>(this T layer, CIMGeometricEffect effect) where T : CIMSymbolLayer
		{
			if (layer == null)
				throw new ArgumentNullException(nameof(layer));
			if (effect == null)
				throw new ArgumentNullException(nameof(effect));

			if (layer.Effects == null)
			{
				layer.Effects = new[] {effect};
			}
			else
			{
				var effects = new CIMGeometricEffect[layer.Effects.Length + 1];
				Array.Copy(layer.Effects, effects, layer.Effects.Length);
				effects[layer.Effects.Length] = effect;
				layer.Effects = effects;
			}

			return layer;
		}

		public static CIMSymbolLayer AddOffset(
			this CIMSymbolLayer layer, double offset,
			GeometricEffectOffsetMethod method = GeometricEffectOffsetMethod.Bevelled,
			GeometricEffectOffsetOption option = GeometricEffectOffsetOption.Accurate)
		{
			return layer.AddEffect(CreateEffectOffset(offset, method, option));
		}

		public static CIMSymbolLayer AddDashes(
			this CIMSymbolLayer layer, double[] pattern,
			LineDashEnding ending = LineDashEnding.NoConstraint,
			double customEndingOffset = 0.0, double offsetAlongLine = 0.0,
			LineDashEnding controlPointEnding = LineDashEnding.NoConstraint)
		{
			return layer.AddEffect(CreateEffectDashes(pattern, ending, customEndingOffset,
			                                          offsetAlongLine, controlPointEnding));
		}

		public static CIMGeometricEffect CreateEffectOffset(
			double offset,
			GeometricEffectOffsetMethod method = GeometricEffectOffsetMethod.Bevelled,
			GeometricEffectOffsetOption option = GeometricEffectOffsetOption.Accurate)
		{
			return new CIMGeometricEffectOffset
			       {
				       Offset = offset,
				       Method = method,
				       Option = option
			       };
		}

		public static CIMGeometricEffect CreateEffectDashes(
			double[] dashPattern, LineDashEnding lineEnding = LineDashEnding.NoConstraint,
			double customEndingOffset = 0.0, double offsetAlongLine = 0.0,
			LineDashEnding controlPointEnding = LineDashEnding.NoConstraint)
		{
			// Note: CustomEndingOffset applies only if LineDashEnding is Custom
			// Note: OffsetAlongLine applies only if LineDashEnding is Custom or NoConstraint

			return new CIMGeometricEffectDashes
			       {
				       DashTemplate = dashPattern,
				       LineDashEnding = lineEnding,
				       CustomEndingOffset = customEndingOffset,
				       OffsetAlongLine = offsetAlongLine,
				       ControlPointEnding = controlPointEnding
			       };
		}

		public static double[] CreateDashPattern(params double[] pattern)
		{
			return pattern;
		}

		#endregion
	}
}
