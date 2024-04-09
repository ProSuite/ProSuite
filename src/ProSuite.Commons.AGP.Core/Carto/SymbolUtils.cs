using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.CodeAnnotations;

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
		public const double DefaultMarkerSize = 10.0;

		#region Conversion

		/// <remarks>
		/// There is no universal standard for the length of a typographic point.
		/// However, PostScript defines the point to be 1/72 of an inch and this
		/// seems to be established practice in desktop publishing. We adopt it.
		/// </remarks>
		private const double PointsPerMillimeter = 2.83465;

		public static double PointsToMillimeters(double points)
		{
			return points / PointsPerMillimeter;
		}

		public static double MillimetersToPoints(double millimeters)
		{
			return millimeters * PointsPerMillimeter;
		}

		#endregion

		#region Creation

		public static CIMPointSymbol CreatePointSymbol(
			CIMColor color, double size = -1, MarkerStyle style = MarkerStyle.Circle)
		{
			return CreatePointSymbol(CreateMarker(color, size, style));
		}

		public static CIMPointSymbol CreatePointSymbol(CIMMarker marker)
		{
			var symbol = new CIMPointSymbol();
			symbol.ScaleX = 1.0;
			symbol.HaloSize = 1.0;
			symbol.SymbolLayers = new CIMSymbolLayer[] {marker};
			return symbol;
		}

		public static CIMPointSymbol MakePointSymbol(this CIMMarker marker)
		{
			return CreatePointSymbol(marker);
		}

		public static CIMLineSymbol CreateLineSymbol(float red, float green, float blue,
		                                             double lineWidth)
		{
			CIMColor color = ColorUtils.CreateRGB(red, green, blue);

			var solidStroke = new CIMSolidStroke
			                  {
				                  Color = color,
				                  Width = lineWidth
			                  };

			CIMLineSymbol lineSymbol = CreateLineSymbol(solidStroke);
			return lineSymbol;
		}

		public static CIMLineSymbol CreateLineSymbol(params CIMSymbolLayer[] layers)
		{
			return new CIMLineSymbol {SymbolLayers = layers, Effects = null};
		}

		public static CIMLineSymbol CreateLineSymbol(CIMColor color = null, double width = -1)
		{
			return CreateLineSymbol(CreateSolidStroke(color, width));
		}

		public static CIMPolygonSymbol CreatePolygonSymbol(params CIMSymbolLayer[] layers)
		{
			return new CIMPolygonSymbol {SymbolLayers = layers, Effects = null};
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

			return CreatePolygonSymbol(symbolLayers.ToArray());
		}

		public static CIMPolygonSymbol CreateHatchFillSymbol(float red, float green, float blue,
		                                                     double rotation = 0,
		                                                     double lineWidth = 2,
		                                                     double lineSeparation = 5)
		{
			CIMLineSymbol lineSymbol = CreateLineSymbol(red, green, blue, lineWidth);

			var hatchFill = new CIMHatchFill
			                {
				                Enable = true,
				                Rotation = rotation,
				                Separation = lineSeparation,
				                LineSymbol = lineSymbol
			                };

			var symbolLayers = new List<CIMSymbolLayer>();

			symbolLayers.AddRange(lineSymbol.SymbolLayers);
			symbolLayers.Add(hatchFill);

			return new CIMPolygonSymbol {SymbolLayers = symbolLayers.ToArray()};
		}

		#endregion

		#region Modification

		/// <summary>
		/// See <see cref="Blend(CIMSymbol,CIMColor,float)"/>. Modifies
		/// the given symbol reference and returns it for convenience.
		/// </summary>
		public static CIMSymbolReference Blend(this CIMSymbolReference symref,
		                                       CIMColor color, float factor = 0.5f)
		{
			symref?.Symbol.Blend(color, factor);
			return symref;
		}

		/// <summary>
		/// Blend the colors of the given symbol with the given color.
		/// A <paramref name="factor"/> value of 0 keeps the original
		/// colors, a value of 1 uses the given color, values in-between
		/// blend, and other values have an undefined result.
		/// Modifies this symbol (!) and returns it for convenience.
		/// </summary>
		public static CIMSymbol Blend(this CIMSymbol symbol, CIMColor color, float factor = 0.5f)
		{
			if (color is null) return symbol;

			if (symbol is CIMMultiLayerSymbol multiLayerSymbol &&
			    multiLayerSymbol.SymbolLayers != null)
			{
				foreach (var symbolLayer in multiLayerSymbol.SymbolLayers)
				{
					if (symbolLayer is CIMSolidFill solidFill)
					{
						solidFill.Color = Blend(solidFill.Color, color, factor);
					}
					else if (symbolLayer is CIMSolidStroke solidStroke)
					{
						solidStroke.Color = Blend(solidStroke.Color, color, factor);
					}
					else if (symbolLayer is CIMVectorMarker vectorMarker &&
					         vectorMarker.MarkerGraphics != null)
					{
						foreach (var markerGraphic in vectorMarker.MarkerGraphics)
						{
							markerGraphic.Symbol.Blend(color, factor);
						}
					}
					else if (symbolLayer is CIMCharacterMarker characterMarker)
					{
						characterMarker.Symbol.Blend(color, factor);
					}
					//else: other symbol layer types are left unmodified
				}
			}
			else if (symbol is CIMTextSymbol textSymbol)
			{
				textSymbol.Symbol.Blend(color, factor);
			}
			// else: should not occur (all symbols are either MultiLayer or Text)

			return symbol;
		}

		private static CIMColor Blend(CIMColor color, CIMColor other, float factor)
		{
			if (color is null) return null;
			if (other is null) return color;
			return ColorUtils.Blend(color, other, factor);
		}

		#endregion

		#region Symbol Layers

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

		public static CIMFill CreateHatchFill(double angleDegrees, double separation,
		                                      CIMColor color = null)
		{
			var hatchFill = new CIMHatchFill();
			hatchFill.Enable = true;
			hatchFill.Rotation = angleDegrees;
			hatchFill.Separation = separation;
			hatchFill.LineSymbol = CreateLineSymbol(color);
			return hatchFill;
		}

		public static CIMMarker CreateMarker(CIMColor color, double size, MarkerStyle style)
		{
			var geometry = CreateMarkerGeometry(style);
			var symbol = CreatePolygonSymbol(color);

			var graphic = new CIMMarkerGraphic {Geometry = geometry, Symbol = symbol};

			var marker = new CIMVectorMarker();
			marker.ColorLocked = false;
			marker.Enable = true;
			marker.Size = size >= 0 ? size : DefaultMarkerSize;
			marker.AnchorPointUnits = SymbolUnits.Relative;
			marker.BillboardMode3D = BillboardMode.FaceNearPlane;
			marker.DominantSizeAxis3D = DominantSizeAxis.Y;
			marker.ScaleSymbolsProportionally = true;
			marker.RespectFrame = true;
			marker.MarkerGraphics = new[] {graphic};
			marker.Frame = style == MarkerStyle.Circle
				               ? GeometryFactory.CreateEnvelope(-5, -5, 5, 5)
				               : graphic.Geometry.Extent;

			return marker;
		}

		public static Geometry CreateMarkerGeometry(MarkerStyle style)
		{
			switch (style)
			{
				case MarkerStyle.Circle:
					return GeometryFactory.CreateBezierCircle(5);

				case MarkerStyle.Square:
					var envelope = GeometryFactory.CreateEnvelope(-5, -5, 5, 5);
					return GeometryFactory.CreatePolygon(envelope);

				default:
					throw new NotImplementedException(
						"Sorry, this MarkerStyle is not yet implemented");
			}
		}

		#endregion

		#region Marker Placements

		public static T SetMarkerPlacement<T>(this T marker,
		                                      [CanBeNull] CIMMarkerPlacement placement)
			where T : CIMMarker
		{
			if (marker == null)
				throw new ArgumentNullException(nameof(marker));

			marker.MarkerPlacement = placement;

			return marker;
		}

		public static T SetMarkerPlacementAlongLine<T>(this T marker, params double[] template)
			where T : CIMMarker
		{
			var placement = new CIMMarkerPlacementAlongLineSameSize();
			placement.PlacementTemplate = template ?? new[] {10 * DefaultStrokeWidth};

			return marker.SetMarkerPlacement(placement);
		}

		public static T SetMarkerPlacementAtExtremities<T>(this T marker,
		                                                   ExtremityPlacement extremities,
		                                                   double offsetAlongLine = 0.0)
			where T : CIMMarker
		{
			var placement = new CIMMarkerPlacementAtExtremities();
			placement.ExtremityPlacement = extremities;
			placement.OffsetAlongLine = offsetAlongLine;

			return marker.SetMarkerPlacement(placement);
		}

		public static T SetMarkerEndings<T>(this T marker, PlacementEndings endings,
		                                    double customOffset = 0.0) where T : CIMMarker
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

		public static T SetMarkerControlPointEndings<T>(this T marker, PlacementEndings endings)
			where T : CIMMarker
		{
			if (marker.MarkerPlacement is CIMMarkerPlacementAlongLine placement)
			{
				// TODO: How is this done in 3.0?
				throw new NotImplementedException();
				//placement.ControlPointPlacement = endings;
			}
			else
			{
				throw IncompatibleMarkerPlacement();
			}

			return marker;
		}

		public static T SetMarkerOffsetAlongLine<T>(this T marker, double offsetAlongLine)
			where T : CIMMarker
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

		public static T SetMarkerPerpendicularOffset<T>(this T marker, double offset)
			where T : CIMMarker
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

		public static T SetEffects<T>(this T layer, params CIMGeometricEffect[] effects)
			where T : CIMSymbolLayer
		{
			if (layer == null)
				throw new ArgumentNullException(nameof(layer));

			layer.Effects = effects ?? Array.Empty<CIMGeometricEffect>();

			return layer;
		}

		public static T AddGlobalEffect<T>(this T symbol, CIMGeometricEffect effect)
			where T : CIMMultiLayerSymbol
		{
			if (symbol == null)
				throw new ArgumentNullException(nameof(symbol));

			symbol.Effects = AddOne(symbol.Effects, effect);

			return symbol;
		}

		public static T AddEffect<T>(this T layer, CIMGeometricEffect effect)
			where T : CIMSymbolLayer
		{
			if (layer == null)
				throw new ArgumentNullException(nameof(layer));

			layer.Effects = AddOne(layer.Effects, effect);

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

		#region Overrides

		// What is called a Primitive Override in CIM is called
		// a "symbol property connection" or an "attribute mapping"
		// in the user interface. Overrides live on the SymbolReference,
		// not on the Symbol. They refer to properties of geometric
		// effects, symbol layers, and marker placements by means
		// of a PrimitiveName and the PropertyName. The PrimitiveName
		// is set to a GUID on both the primitive and the override.
		//
		// Overrides are "typically set by renderers at draw time".
		//
		// The name "override" is unfortunate because it collides
		// with a C# keyword; here we use the name "mapping" instead.

		public static CIMSymbolReference CreateReference(this CIMSymbol symbol)
		{
			if (symbol == null)
				throw new ArgumentNullException(nameof(symbol));

			return new CIMSymbolReference {Symbol = symbol};
		}

		public static CIMSymbolReference AddMapping(
			this CIMSymbolReference reference, string label, string property, string expression)
		{
			if (reference == null)
				throw new ArgumentNullException(nameof(reference));

			var primitive = FindPrimitiveByName<CIMObject>(reference.Symbol, label);

			if (primitive == null)
			{
				throw new InvalidOperationException($"Found no primitive with name {label}");
			}

			if (primitive.GetType().GetProperty(
				    property, BindingFlags.Public | BindingFlags.Instance) == null)
			{
				throw new InvalidOperationException($"Primitive has no property '{property}'");
			}

			var mapping = new CIMPrimitiveOverride
			              {
				              PrimitiveName = label,
				              PropertyName = property,
				              Expression = expression
			              };

			reference.PrimitiveOverrides = AddOne(reference.PrimitiveOverrides, mapping);

			return reference;
		}

		public static T LabelLayer<T>(this T primitive, out string label) where T : CIMSymbolLayer
		{
			if (primitive == null)
				throw new ArgumentNullException(nameof(primitive));

			label = CreateUniqueLabel();
			primitive.PrimitiveName = label;

			return primitive;
		}

		public static T LabelEffect<T>(this T primitive, out string label)
			where T : CIMGeometricEffect
		{
			if (primitive == null)
				throw new ArgumentNullException(nameof(primitive));

			label = CreateUniqueLabel();
			primitive.PrimitiveName = label;

			return primitive;
		}

		public static T LabelPlacement<T>(this T primitive, out string label)
			where T : CIMMarkerPlacement
		{
			if (primitive == null)
				throw new ArgumentNullException(nameof(primitive));

			label = CreateUniqueLabel();
			primitive.PrimitiveName = label;

			return primitive;
		}

		public static T LabelGraphic<T>(this T primitive, out string label) where T : CIMMarkerGraphic
		{
			if (primitive == null)
				throw new ArgumentNullException(nameof(primitive));

			label = CreateUniqueLabel();
			primitive.PrimitiveName = label;

			return primitive;
		}

		public static T FindPrimitiveByName<T>(CIMSymbol symbol, string label) where T : CIMObject
		{
			// TextSymbol has no PrimitiveName; all other symbols derive from MultiLayerSymbol
			if (! (symbol is CIMMultiLayerSymbol multiLayerSymbol)) return null;

			string primitiveName = label;

			if (multiLayerSymbol.Effects != null)
			{
				foreach (var effect in multiLayerSymbol.Effects)
				{
					if (string.Equals(effect.PrimitiveName, primitiveName))
					{
						if (effect is T found) return found;
					}
				}
			}

			if (multiLayerSymbol.SymbolLayers != null)
			{
				foreach (var layer in multiLayerSymbol.SymbolLayers)
				{
					if (string.Equals(layer.PrimitiveName, primitiveName))
					{
						if (layer is T found) return found;
					}

					if (layer is CIMMarker marker)
					{
						var placement = marker.MarkerPlacement;
						if (placement != null &&
						    string.Equals(placement.PrimitiveName, primitiveName))
						{
							if (placement is T found) return found;
						}

						if (layer is CIMVectorMarker vectorMarker)
						{
							var graphics = vectorMarker.MarkerGraphics;
							if (graphics != null)
							{
								foreach (var graphic in graphics)
								{
									if (string.Equals(graphic.PrimitiveName, primitiveName))
									{
										if (graphic is T found) return found;
									}
								}
							}
						}
					}

					if (layer.Effects != null)
					{
						foreach (var effect in layer.Effects)
						{
							if (string.Equals(effect.PrimitiveName, primitiveName))
							{
								if (effect is T found) return found;
							}
						}
					}
				}
			}

			return null; // not found
		}

		/// <summary>
		/// Find and return the "primitive" (that is, symbol layer, geometric effect,
		/// or marker placement) at the specified location within the given symbol.
		/// Location specification is one of:
		/// <list type="bullet">
		/// <item>effect N</item>
		/// <item>layer N</item>
		/// <item>layer N placement</item>
		/// <item>layer N graphic M</item>
		/// <item>layer N effect M</item>
		/// </list>
		/// where N and M are non-negative integers.
		/// </summary>
		/// <typeparam name="T">Typically one of CIMGeometricEffect or CIMSymbolLayer
		/// or CIMMarkerPlacement or a subtype of these.</typeparam>
		/// <returns>The primitive found, or <c>null</c> if not found</returns>
		public static T FindPrimitiveByPath<T>(CIMSymbol symbol, string spec) where T : CIMObject
		{
			if (string.IsNullOrEmpty(spec)) return null;
			if (! (symbol is CIMMultiLayerSymbol multiLayerSymbol)) return null;

			const char blank = ' ';
			const char tab = '\t';
			var parts = spec.Split(new[] {blank, tab}, StringSplitOptions.RemoveEmptyEntries);

			if (parts.Length < 2 || parts.Length > 4)
			{
				throw InvalidPrimitiveSpec(spec);
			}

			if (! TryParseIndex(parts[1], out int index) || index < 0)
			{
				throw InvalidPrimitiveSpec(spec);
			}

			int localIndex = -1;
			if (parts.Length == 4 && (! TryParseIndex(parts[3], out localIndex) || localIndex < 0))
			{
				throw InvalidPrimitiveSpec(spec);
			}

			string root = parts[0].Trim();
			string suffix = parts.Length >= 3 ? parts[2].Trim() : null;

			if (string.Equals(root, "effect", StringComparison.OrdinalIgnoreCase))
			{
				var effects = multiLayerSymbol.Effects;
				var effect = effects != null && index < effects.Length ? effects[index] : null;
				return string.IsNullOrEmpty(suffix) ? effect as T : null;
			}

			if (string.Equals(root, "layer", StringComparison.OrdinalIgnoreCase))
			{
				var layers = multiLayerSymbol.SymbolLayers;
				var layer = layers != null && index < layers.Length ? layers[index] : null;
				if (string.IsNullOrEmpty(suffix)) return layer as T;

				if (string.Equals(suffix, "placement", StringComparison.OrdinalIgnoreCase))
				{
					return layer is CIMMarker marker ? marker.MarkerPlacement as T : null;
				}

				if (string.Equals(suffix, "effect", StringComparison.OrdinalIgnoreCase))
				{
					var effects = layer?.Effects;
					var effect = effects != null && localIndex < effects.Length
						             ? effects[localIndex]
						             : null;
					return effect as T;
				}

				if (string.Equals(suffix, "graphic", StringComparison.OrdinalIgnoreCase))
				{
					var graphics = (layer as CIMVectorMarker)?.MarkerGraphics;
					var graphic = graphics != null && localIndex < graphics.Length
						              ? graphics[localIndex]
						              : null;
					return graphic as T;

					// TODO MarkerGraphic has a PrimitiveName, but no suitable properties for mapping; instead, recurse on graphic.Symbol
				}

				throw InvalidPrimitiveSpec(spec);
			}

			throw InvalidPrimitiveSpec(spec);
		}

		private static ArgumentException InvalidPrimitiveSpec(string spec)
		{
			string message = $"Invalid primitive spec: {spec}. Expect \"effect N\" " +
			                 "or \"layer N\" or " +
			                 "or \"layer N placement\" " +
			                 "or \"layer N effect M\" " +
			                 "or \"layer N graphic M\" " +
			                 "where N and M are non-negative integers";
			return new ArgumentException(message);
		}

		#endregion

		#region Retrieval

		public static CIMSymbolReference GetSymbol(CIMRenderer renderer, INamedValues feature = null, double scaleDenom = 0)
		{
			if (renderer is null)
				throw new ArgumentNullException(nameof(renderer));

			if (renderer is CIMSimpleRenderer simpleRenderer)
			{
				return GetSymbol(simpleRenderer, feature, scaleDenom);
			}

			if (renderer is CIMUniqueValueRenderer uvRenderer)
			{
				return GetSymbol(uvRenderer, feature, scaleDenom);
			}

			throw new NotSupportedException(
				$"{renderer.GetType().Name} is not supported, sorry");
		}

		private static CIMSymbolReference GetSymbol(CIMSimpleRenderer renderer, INamedValues feature, double scaleDenom)
		{
			// CIMSimpleRenderer:
			// - .Symbol: CIMSymbolReference
			// - .AlternateSymbols: [CIMSymbolReference]
			// - .VisualVariables: [] // Color, Rotation, Size, ...

			var symref = GetSymbolReference(renderer.Symbol, renderer.AlternateSymbols, scaleDenom);

			return ApplyOverrides(symref, feature, scaleDenom, renderer.VisualVariables);
		}

		private static CIMSymbolReference GetSymbol(CIMUniqueValueRenderer renderer, INamedValues feature, double scaleDenom)
		{
			// CIMUniqueValueRenderer:
			// - .Fields: string[]
			// - .ValueExpressionInfo: {Expression,ReturnType,Title,Name}
			// - .Groups: [{Heading, Classes}]
			// - .VisualVariables
			// - .DefaultSymbol: CIMSymbolReference
			// Either 1..3 fields (.Fields) -or- 1 Arcade expression (.ValueExpressionInfo)

			CIMSymbolReference primary = renderer.DefaultSymbol;
			CIMSymbolReference[] alternates = null;

			if (feature != null)
			{
				var clazz = FindClass(renderer, feature);
				if (clazz != null)
				{
					primary = clazz.Symbol;
					alternates = clazz.AlternateSymbols;
				}
			}

			var symref = GetSymbolReference(primary, alternates, scaleDenom);

			return ApplyOverrides(symref, feature, scaleDenom, renderer.VisualVariables);
		}

		private static CIMUniqueValueClass FindClass(CIMUniqueValueRenderer renderer, INamedValues feature)
		{
			if (renderer.ValueExpressionInfo?.Expression != null)
			{
				throw new NotImplementedException(
					"Renderer uses ValueExpressionInfo (Arcade), which is not yet implemented");
			}

			var fields = renderer.Fields;
			if (fields is { Length: > 0 })
			{
				var featureValues = new string[fields.Length];
				var invariant = CultureInfo.InvariantCulture;
				for (int i = 0; i < fields.Length; i++)
				{
					string fieldName = fields[i];
					object value = feature.GetValue(fieldName);
					featureValues[i] = Convert.ToString(value, invariant);
				}

				foreach (var group in renderer.Groups)
				{
					foreach (var clazz in group.Classes)
					{
						foreach (var combo in clazz.Values)
						{
							var classValues = combo.FieldValues;
							if (EqualValues(featureValues, classValues))
							{
								return clazz;
							}
						}
					}
				}
			}

			return null;
		}

		private static bool EqualValues(string[] givenValues, string[] candidates)
		{
			// Both arrays are expected to have the same length,
			// but here we want no errors, so be defensive:
			int count = Math.Min(givenValues.Length, candidates.Length);
			if (count < 1) return false;

			for (int i = 0; i < count; i++)
			{
				if (!string.Equals(givenValues[i], candidates[i]))
				{
					return false;
				}
			}

			return true;
		}

		private static CIMSymbolReference GetSymbolReference(
			CIMSymbolReference primary, [CanBeNull] CIMSymbolReference[] alternates, double scaleDenom)
		{
			if (!(scaleDenom > 0))
			{
				return primary;
			}

			if (alternates == null || alternates.Length < 1)
			{
				return primary;
			}

			// Scale range representation and inclusion of boundaries:
			// - only denominator is stored; 0 means undefined
			// - MaxScale is max scale = least denominator
			// - MinScale is min scale = greatest denominator
			// - MaxScale is exclusive, MinScale is inclusive (empirical)
			// For example, a scale range 1:5000 to 1:10'000 has MinScale=10000 and MaxScale=5000
			// and the corresponding symbol is used for a map scale denom in (5000,10000]

			double minScale = primary.MinScale > 0 ? primary.MinScale : double.MaxValue;
			double maxScale = primary.MaxScale > 0 ? primary.MaxScale : 0.0;

			if (maxScale < scaleDenom && scaleDenom <= minScale)
			{
				return primary;
			}

			foreach (var alternate in alternates)
			{
				minScale = alternate.MinScale > 0 ? alternate.MinScale : double.MaxValue;
				maxScale = alternate.MaxScale > 0 ? alternate.MaxScale : 0.0;

				if (maxScale < scaleDenom && scaleDenom <= minScale)
				{
					return alternate;
				}
			}

			return primary; // default if no alternate symbol matches
		}

		public static CIMSymbolReference ApplyOverrides(
			CIMSymbolReference symref, INamedValues feature,
			double scaleDenom = 0, CIMVisualVariable[] visualVariables = null)
		{
			if (symref is null) return null;

			var symbol = MaterializeSymbol(symref, feature, scaleDenom, visualVariables);

			return new CIMSymbolReference
			       {
				       SymbolName = symref.SymbolName,
				       MinScale = symref.MinScale,
				       MaxScale = symref.MaxScale,
				       MinDistance = symref.MinDistance,
				       MaxDistance = symref.MaxDistance,
				       Symbol = symbol, // the now materialized symbol
				       PrimitiveOverrides = null, // they have been applied
				       ScaleDependentSizeVariation = null, // they have been applied
				       StylePath = null // no longer from style
			       };
		}

		private static CIMSymbol MaterializeSymbol(
			CIMSymbolReference symref, INamedValues feature, double scaleDenom,
			CIMVisualVariable[] visualVariables = null)
		{
			// CIMSymbolReference:
			// - Symbol: CIMSymbol
			// - MinScale, MaxScale: double
			// - PrimitiveOverrides: [{PrimitiveName, PropertyName, Expression}]
			// - ScaleDependentSizeVariation: [{Scale, Size}]

			// Make a clone, because we may modify it!

			var symbol = (CIMSymbol) CIMObject.Clone(symref.Symbol);

			if (feature != null && symref.PrimitiveOverrides is { Length: > 0 })
			{
				foreach (var mapping in symref.PrimitiveOverrides)
				{
					// - evaluate mapping.Expression (or Arcade: mapping.ValueExpressionInfo) against given feature
					// - find primitive by mapping.PrimitiveName (see SymbolUtils on how to traverse a CIMSymbol)
					// - replace primitive's mapping.PropertyName by expression value

					try
					{
						ApplyOverride(symbol, mapping, feature);
					}
					catch (Exception ex)
					{
						var expr = mapping.ValueExpressionInfo?.Expression ??
								   mapping.Expression ?? "(n/a)";
						throw new Exception(
							$"Cannot apply primitive override (primitive {mapping.PrimitiveName}, " +
							$"property {mapping.PropertyName}, value {expr}): {ex.Message}");
					}
				}
			}

			if (scaleDenom > 0 && symref.ScaleDependentSizeVariation is { Length: > 0 })
			{
				// apply scale dependent size (Size of points, Width of lines, Outline width of polygons)
				throw new NotImplementedException(
					"Symbol has ScaleDependentSizeVariation, which is not yet implemented");
			}

			if (feature != null && visualVariables is { Length: > 0 })
			{
				// apply visual variables
				throw new NotImplementedException(
					"Renderer has VisualVariables, which are not yet implemented");
			}

			return symbol;
		}

		private static void ApplyOverride(CIMSymbol symbol,
										  CIMPrimitiveOverride mapping, INamedValues feature)
		{
			var primitive = FindPrimitiveByName<CIMObject>(symbol, mapping.PrimitiveName);
			if (primitive is null)
			{
				throw new Exception($"Graphic primitive '{mapping.PrimitiveName}' not found in symbol");
			}

			var property = primitive.GetType().GetProperty(mapping.PropertyName);
			if (property is null)
			{
				throw new Exception(
					$"Property '{mapping.PropertyName}' not found on {primitive.GetType().Name}");
			}

			object value = Evaluate(mapping.Expression, mapping.ValueExpressionInfo, feature);
			if (value != null && value != DBNull.Value)
			{
				property.SetValue(primitive, value);
			}
			// else: use symbol's default value
		}

		private static object Evaluate(
			string expression, CIMExpressionInfo arcade, INamedValues feature)
		{
			if (feature is null)
			{
				return null; // no feature means no override value
			}

			if (!string.IsNullOrWhiteSpace(expression))
			{
				var match = _fieldExpressionRegex.Match(expression);
				if (match.Success)
				{
					string fieldName = match.Result("$1");
					return feature.GetValue(fieldName);
				}

				return feature.GetValue(expression);
			}

			if (arcade is { Expression.Length: > 0 })
			{
				// TODO handle the simple case: $feature.FIELDNAME
				// TODO since Pro SDK 3.x there's an Arcade evaluator: use it (if dependencies are ok)
				throw new NotImplementedException("Arcade expression evaluation is not yet implemented");
			}

			return null;
		}

		private static readonly Regex _fieldExpressionRegex = new(@"^\s*\[\s*([ _\w]+)\s*\]\s*$");

		#endregion

		#region Private utils

		private static string CreateUniqueLabel()
		{
			// Empirical: Pro uses GUIDs for primitive names in
			// the default lower-case-dashes-no-braces format.

			return Guid.NewGuid().ToString();
		}

		private static bool TryParseIndex(string text, out int index)
		{
			// Integer in decimal notation, allow leading and trailing blanks
			return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture,
			                    out index);
		}

		/// <remarks>
		/// Usage: <c>foo.Array = AddOne(foo.Array, item)</c>
		/// </remarks>
		private static T[] AddOne<T>([CanBeNull] T[] array, T item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			if (array == null)
			{
				return new[] {item};
			}

			var enlarged = new T[array.Length + 1];
			Array.Copy(array, enlarged, array.Length);
			enlarged[array.Length] = item;
			return enlarged;
		}

		#endregion
	}
}
