using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Internal.Geometry;
using Google.Protobuf;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.Commons.Geom.Wkb;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Definitions.Shared.Gdb;
using Version = ArcGIS.Core.Data.Version;

namespace ProSuite.Microservices.Client.AGP;

[CanBeNull]
public static class ProtobufConversionUtils
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	[CanBeNull]
	public static SpatialReference FromSpatialReferenceMsg(
		[CanBeNull] SpatialReferenceMsg spatialRefMsg,
		[CanBeNull] SpatialReference classSpatialRef = null)
	{
		if (spatialRefMsg == null)
		{
			return null;
		}

		switch (spatialRefMsg.FormatCase)
		{
			case SpatialReferenceMsg.FormatOneofCase.None:
				return null;
			case SpatialReferenceMsg.FormatOneofCase.SpatialReferenceEsriXml:

				string xml = spatialRefMsg.SpatialReferenceEsriXml;

				return string.IsNullOrEmpty(xml)
					       ? null
					       : SpatialReferenceBuilder.FromXml(xml);

			case SpatialReferenceMsg.FormatOneofCase.SpatialReferenceWkid:

				int wkId = spatialRefMsg.SpatialReferenceWkid;

				return classSpatialRef?.Wkid == wkId
					       ? classSpatialRef
					       : SpatialReferenceBuilder.CreateSpatialReference(wkId);

			case SpatialReferenceMsg.FormatOneofCase.SpatialReferenceWkt:

				return SpatialReferenceBuilder.CreateSpatialReference(
					spatialRefMsg.SpatialReferenceWkt);

			default:
				throw new NotImplementedException(
					$"Unsupported spatial reference format: {spatialRefMsg.FormatCase}");
		}
	}

	[CanBeNull]
	public static SpatialReferenceMsg ToSpatialReferenceMsg(
		[CanBeNull] SpatialReference spatialReference,
		SpatialReferenceMsg.FormatOneofCase format)
	{
		if (spatialReference == null)
		{
			return null;
		}

		SpatialReferenceMsg result = new SpatialReferenceMsg();

		switch (format)
		{
			case SpatialReferenceMsg.FormatOneofCase.None:
				break;

			case SpatialReferenceMsg.FormatOneofCase.SpatialReferenceEsriXml:
				result.SpatialReferenceEsriXml = spatialReference.ToXml();
				break;

			case SpatialReferenceMsg.FormatOneofCase.SpatialReferenceWkid:
				result.SpatialReferenceWkid = spatialReference.Wkid;
				break;

			case SpatialReferenceMsg.FormatOneofCase.SpatialReferenceWkt:
				result.SpatialReferenceWkt = spatialReference.Wkt;
				break;

			default:
				throw new NotImplementedException(
					$"Unsupported spatial reference format: {format}");
		}

		return result;
	}

	[CanBeNull]
	public static Geometry FromShapeMsg(
		[CanBeNull] ShapeMsg shapeMsg,
		[CanBeNull] SpatialReference knownSpatialReference = null)
	{
		// TODO: Make sure all callers provide a spatial reference! Otherwise the VCS might get lost.

		if (shapeMsg == null) return null;

		if (shapeMsg.FormatCase == ShapeMsg.FormatOneofCase.None) return null;

		SpatialReference sr = knownSpatialReference ??
		                      FromSpatialReferenceMsg(shapeMsg.SpatialReference);

		Geometry result;

		switch (shapeMsg.FormatCase)
		{
			case ShapeMsg.FormatOneofCase.EsriShape:

				if (shapeMsg.EsriShape.IsEmpty) return null;

				result = FromEsriShapeBuffer(shapeMsg.EsriShape.ToByteArray(), sr);

				break;
			case ShapeMsg.FormatOneofCase.Wkb:

				result = FromWkb(shapeMsg.Wkb.ToByteArray(), sr);

				break;
			case ShapeMsg.FormatOneofCase.Envelope:

				result = FromEnvelopeMsg(shapeMsg.Envelope, sr);

				break;
			default:
				throw new NotImplementedException(
					$"Unsupported format: {shapeMsg.FormatCase}");
		}

		return result;
	}

	[NotNull]
	public static List<Geometry> FromShapeMsgList(
		[NotNull] ICollection<ShapeMsg> shapeBufferList,
		[CanBeNull] SpatialReference spatialReference)
	{
		var geometryList = new List<Geometry>(shapeBufferList.Count);

		foreach (var shapeMsg in shapeBufferList)
		{
			var geometry = FromShapeMsg(shapeMsg, spatialReference);
			geometryList.Add(geometry);
		}

		return geometryList;
	}

	[CanBeNull]
	private static Envelope FromEnvelopeMsg([CanBeNull] EnvelopeMsg envProto,
	                                        [CanBeNull] SpatialReference spatialReference)
	{
		if (envProto == null)
		{
			return null;
		}

		var result =
			EnvelopeBuilderEx.CreateEnvelope(new Coordinate2D(envProto.XMin, envProto.YMin),
			                                 new Coordinate2D(envProto.XMax, envProto.YMax),
			                                 spatialReference);

		return result;
	}

	/// <summary>
	/// Converts a geometry to its wire format.
	/// </summary>
	/// <param name="geometry">The geometry</param>
	/// <param name="useSpatialRefWkId">Whether only the spatial reference well-known
	/// id is to be transferred rather than the entire XML representation, which is
	/// a performance bottleneck for a large number of features.</param>
	/// <returns></returns>
	[CanBeNull]
	public static ShapeMsg ToShapeMsg([CanBeNull] Geometry geometry,
	                                  bool useSpatialRefWkId = false)
	{
		if (geometry == null) return null;

		SpatialReference spatialRef = geometry.SpatialReference;

		Assert.ArgumentCondition(spatialRef != null,
		                         "Spatial reference must not be null");

		ShapeMsg result = new ShapeMsg();

		if (geometry.GeometryType == GeometryType.Multipatch)
		{
			// Do not use esri shape because it arrives as empty geometry on the other side
			Multipatch multipatch = (Multipatch) geometry;
			result.Wkb = GetWkbByteString(multipatch);
		}
		else
		{
			// TODO: For reduced copying and array instantiation, consider using
			// GeometryEngine.Instance.ExportToEsriShape(... ref byte[] buffer)
			//       and then use ByteString.CopyFrom(buffer, 0, buffer.Length);
			//       ... but measure first!
			result.EsriShape = ByteString.CopyFrom(geometry.ToEsriShape());
		}

		result.SpatialReference = ToSpatialReferenceMsg(
			spatialRef,
			useSpatialRefWkId
				? SpatialReferenceMsg.FormatOneofCase.SpatialReferenceWkid
				: SpatialReferenceMsg.FormatOneofCase.SpatialReferenceEsriXml);

		return result;
	}

	[NotNull]
	public static GdbObjRefMsg ToGdbObjRefMsg([NotNull] Row row)
	{
		var result = new GdbObjRefMsg();

		result.ClassHandle = GeometryProcessingUtils.GetUniqueClassId(row);
		result.ObjectId = row.GetObjectID();

		return result;
	}

	[NotNull]
	public static GdbObjectMsg ToGdbObjectMsg([NotNull] Row feature,
	                                          [CanBeNull] Geometry geometry,
	                                          bool useSpatialRefWkId)
	{
		Table table = feature.GetTable();

		return ToGdbObjectMsg(feature, geometry,
		                      GeometryProcessingUtils.GetUniqueClassId(table),
		                      useSpatialRefWkId);
	}

	[NotNull]
	public static GdbObjectMsg ToGdbObjectMsg([NotNull] Row feature,
	                                          [CanBeNull] Geometry geometry,
	                                          long objectClassHandle,
	                                          bool useSpatialRefWkId)
	{
		var result = new GdbObjectMsg();

		// Or use handle?
		result.ClassHandle = objectClassHandle;

		result.ObjectId = feature.GetObjectID();

		result.Shape = ToShapeMsg(geometry, useSpatialRefWkId);

		return result;
	}

	/// <summary>
	/// Converts the specified features to proto messages and adds them to the provided lists.
	/// </summary>
	/// <param name="features"></param>
	/// <param name="resultGdbObjects"></param>
	/// <param name="resultGdbClasses"></param>
	/// <param name="keepFeatureClassSpatialRef">Whether the spatial reference of the feature
	/// classes should be kept in the message. If false, the spatial reference of the shapes'
	/// will be used also on the feature class.</param>
	/// <param name="getFeatureGeometry">Custom geometry for feature function. This allows for
	/// extra transformations, such as clipping of the shape.</param>
	public static void ToGdbObjectMsgList(
		[NotNull] IEnumerable<Feature> features,
		[NotNull] ICollection<GdbObjectMsg> resultGdbObjects,
		[NotNull] ICollection<ObjectClassMsg> resultGdbClasses,
		bool keepFeatureClassSpatialRef = false,
		[CanBeNull] Func<Feature, Geometry> getFeatureGeometry = null)
	{
		Stopwatch watch = null;

		if (_msg.IsVerboseDebugEnabled)
		{
			watch = Stopwatch.StartNew();
		}

		var classesByClassId = new Dictionary<long, FeatureClass>();

		// Optimization (in Pro, the Map SR seems to be generally equal to the FCs SR, if they match)
		bool omitDetailedShapeSpatialRef = true;

		foreach (Feature feature in features)
		{
			FeatureClass featureClass = feature.GetTable();
			Assert.NotNull(featureClass,
			               $"FeatureClass is null {GdbObjectUtils.ToString(feature)}");

			long uniqueClassId = GeometryProcessingUtils.GetUniqueClassId(featureClass);

			Geometry shape = null;

			// NOTE: The following calls are expensive:
			// - Geometry.GetShape() (internally, the feature's spatial creation seems costly)
			// - FeatureClassDefinition.GetSpatialReference()
			// In case of a large feature count, they should be avoided on a per-feature basis:

			if (! classesByClassId.ContainsKey(uniqueClassId))
			{
				shape = feature.GetShape();

				// Assumption: All features' shapes have the same (map) spatial reference
				// -> Make the remote feature class carry the map SR and each individual feature
				// only keeps the WkId. Do not use the actual feature class' SR to avoid
				// to-and-from transformations!
				SpatialReference spatialRef = shape.SpatialReference;
				resultGdbClasses.Add(
					ToObjectClassMsg(featureClass, uniqueClassId, false, spatialRef));

				classesByClassId.Add(uniqueClassId, featureClass);

				SpatialReference featureClassSpatialRef =
					featureClass.GetDefinition().GetSpatialReference();

				if (keepFeatureClassSpatialRef &&
				    ! SpatialReference.AreEqual(featureClassSpatialRef, spatialRef, false,
				                                true))
				{
					omitDetailedShapeSpatialRef = false;
				}
			}

			if (getFeatureGeometry != null)
			{
				shape = getFeatureGeometry(feature);

				if (shape == null)
				{
					_msg.VerboseDebug(
						() =>
							$"Null geometry provided for {GdbObjectUtils.ToString(feature)}. " +
							$"It is skipped.");
					continue;
				}
			}
			else if (shape == null)
			{
				shape = feature.GetShape();
			}

			resultGdbObjects.Add(ToGdbObjectMsg(feature, shape, omitDetailedShapeSpatialRef));
		}

		_msg.DebugStopTiming(watch, "Converted {0} features to DTOs", resultGdbObjects.Count);
	}

	/// <summary>
	/// Turns the specified row into a <see cref="GdbObjectReference"/> with a (virtually)
	/// unique 32-bit integer class id.
	/// </summary>
	/// <param name="row"></param>
	/// <returns></returns>
	public static GdbObjectReference ToObjectReferenceWithUniqueClassId([NotNull] Row row)
	{
		long uniqueClassId = GeometryProcessingUtils.GetUniqueClassId(row);

		return new GdbObjectReference(uniqueClassId, row.GetObjectID());
	}

	[NotNull]
	public static ObjectClassMsg ToObjectClassMsg(
		[NotNull] Table objectClass,
		long classHandle,
		bool includeFields = false,
		[CanBeNull] SpatialReference spatialRef = null)
	{
		esriGeometryType geometryType = TranslateAGPShapeType(objectClass);

		string name = objectClass.GetName();
		string aliasName = DatasetUtils.GetAliasName(objectClass);

		if (spatialRef == null && objectClass is FeatureClass fc)
		{
			spatialRef = fc.GetDefinition().GetSpatialReference();
		}

		ObjectClassMsg result =
			new ObjectClassMsg()
			{
				Name = name,
				Alias = aliasName ?? string.Empty,
				ClassHandle = classHandle,
				SpatialReference = ToSpatialReferenceMsg(
					spatialRef, SpatialReferenceMsg.FormatOneofCase.SpatialReferenceEsriXml),
				GeometryType = (int) geometryType,
				WorkspaceHandle = objectClass.GetDatastore().Handle.ToInt64()
			};

		if (includeFields)
		{
			List<FieldMsg> fieldMessages = new List<FieldMsg>();

			TableDefinition tableDefinition = objectClass.GetDefinition();

			foreach (Field field in tableDefinition.GetFields())
			{
				fieldMessages.Add(ToFieldMsg(field));
			}

			result.Fields.AddRange(fieldMessages);
		}

		return result;
	}

	private static ObjectClassMsg ToObjectClassMsg(
		[NotNull] string name,
		long classHandle,
		long datastoreHandle,
		esriGeometryType geometryType,
		string aliasName = null,
		IEnumerable<Field> fields = null,
		[CanBeNull] SpatialReference spatialRef = null)
	{
		ObjectClassMsg result =
			new ObjectClassMsg()
			{
				Name = name,
				Alias = aliasName ?? string.Empty,
				ClassHandle = classHandle,
				SpatialReference = ToSpatialReferenceMsg(
					spatialRef, SpatialReferenceMsg.FormatOneofCase.SpatialReferenceEsriXml),
				GeometryType = (int) geometryType,
				WorkspaceHandle = datastoreHandle
			};

		if (fields != null)
		{
			List<FieldMsg> fieldMessages = new List<FieldMsg>();

			foreach (Field field in fields)
			{
				fieldMessages.Add(ToFieldMsg(field));
			}

			result.Fields.AddRange(fieldMessages);
		}

		return result;
	}

	public static ObjectClassMsg ToRelationshipClassMsg(
		[NotNull] RelationshipClass relationshipClass)
	{
		long classId = relationshipClass.GetID();
		long gdbHandle = relationshipClass.GetDatastore().Handle.ToInt64();

		ObjectClassMsg relTableMsg;
		if (relationshipClass is AttributedRelationshipClass attributedRelClass)
		{
			// it's also a real table:
			AttributedRelationshipClassDefinition classDefinition =
				attributedRelClass.GetDefinition();

			string name = relationshipClass.GetName();
			string aliasName = classDefinition.GetAliasName() ?? string.Empty;

			IReadOnlyList<Field> fields = classDefinition.GetFields();

			relTableMsg = ToObjectClassMsg(name, classId, gdbHandle,
			                               esriGeometryType.esriGeometryNull, aliasName, fields);
		}
		else
		{
			// so far just the name is used
			relTableMsg =
				new ObjectClassMsg()
				{
					Name = relationshipClass.GetName(),
					ClassHandle = classId,
					WorkspaceHandle = gdbHandle,
					GeometryType = (int) esriGeometryType.esriGeometryNull
				};
		}

		return relTableMsg;
	}

	[NotNull]
	private static FieldMsg ToFieldMsg(Field field)
	{
		var result = new FieldMsg
		             {
			             Name = field.Name,
			             AliasName = field.AliasName ?? string.Empty,
			             Type = (int) field.FieldType,
			             Length = field.Length,
			             Precision = field.Precision,
			             Scale = field.Scale,
			             IsNullable = field.IsNullable,
			             IsEditable = field.IsEditable
		             };

		if (field.GetDomain()?.GetName() != null)
		{
			result.DomainName = field.GetDomain().GetName();
		}

		return result;
	}

	[NotNull]
	public static WorkspaceMsg ToWorkspaceRefMsg([NotNull] Datastore datastore,
	                                             bool includePath)
	{
		var result =
			new WorkspaceMsg
			{
				WorkspaceHandle = datastore.Handle.ToInt64(),
				WorkspaceDbType = (int) WorkspaceUtils.GetWorkspaceDbType(datastore)
			};

		Version defaultVersion = WorkspaceUtils.GetDefaultVersion(datastore);

		if (defaultVersion != null)
		{
			result.DefaultVersionName = defaultVersion.GetName();

			result.DefaultVersionCreationTicks = defaultVersion.GetCreatedDate().Ticks;
			result.DefaultVersionModificationTicks = defaultVersion.GetModifiedDate().Ticks;

			// Careful: Version.GetDescription() appears to be localized/translated if run in Pro with localized UI
			result.DefaultVersionDescription = defaultVersion.GetDescription() ?? string.Empty;
		}

		if (includePath)
		{
			// NOTE: The path is most useful. It is the actual FGDB path or a temporary sde file that can be used to re-open
			//       the data store. This shall work in every case if the service is local. If the service is remote
			//       the path is useless (unless it is an FGDB on a UNC path that can be seen by the server)
			// -> Use data-verification (if no unsupported tests are included)
			result.Path = datastore.GetPath().AbsoluteUri;
		}
		// The connection properties are useful, but the password is encrypted. Consider using the password
		// stored in the DDX - look it up by Instance/Database/User (or just Instance/Database) from all connection providers.
		// However, the child databases are typically local!
		//DatabaseConnectionProperties connectionProperties =
		//	datastore.GetConnector() as DatabaseConnectionProperties;

		return result;
	}

	private static esriGeometryType TranslateAGPShapeType(Table objectClass)
	{
		if (objectClass is FeatureClass fc)
		{
			var shapeType = fc.GetDefinition().GetShapeType();

			switch (shapeType)
			{
				case GeometryType.Point:
					return esriGeometryType.esriGeometryPoint;
				case GeometryType.Multipoint:
					return esriGeometryType.esriGeometryMultipoint;
				case GeometryType.Polyline:
					return esriGeometryType.esriGeometryPolyline;
				case GeometryType.Polygon:
					return esriGeometryType.esriGeometryPolygon;
				case GeometryType.Multipatch:
					return esriGeometryType.esriGeometryMultiPatch;
				case GeometryType.Envelope:
					return esriGeometryType.esriGeometryEnvelope;

				case GeometryType.GeometryBag:
					return esriGeometryType.esriGeometryBag;

				case GeometryType.Unknown:
					return esriGeometryType.esriGeometryAny;
			}
		}

		return esriGeometryType.esriGeometryNull;
	}

	[NotNull]
	private static Geometry FromEsriShapeBuffer([NotNull] byte[] byteArray,
	                                            [CanBeNull] SpatialReference spatialReference)
	{
		var shapeType = EsriShapeFormatUtils.GetShapeType(byteArray);

		if (byteArray.Length == 5 && shapeType == EsriShapeType.EsriShapeNull)
			// in case the original geometry was empty, ExportToWkb does not store byte order nor geometry type.
			throw new ArgumentException(
				"The provided byte array represents an empty geometry with no geometry type information. Unable to create geometry");

		Geometry result;

		var geometryType = EsriShapeFormatUtils.TranslateEsriShapeType(shapeType);

		switch (geometryType)
		{
			case ProSuiteGeometryType.Point:
				result = MapPointBuilderEx.FromEsriShape(byteArray, spatialReference);
				break;
			case ProSuiteGeometryType.Polyline:
				result = PolylineBuilderEx.FromEsriShape(byteArray, spatialReference);
				break;
			case ProSuiteGeometryType.Polygon:
				result = PolygonBuilderEx.FromEsriShape(byteArray, spatialReference);
				break;
			case ProSuiteGeometryType.Multipoint:
				result = MultipointBuilderEx.FromEsriShape(byteArray, spatialReference);
				break;
			case ProSuiteGeometryType.MultiPatch:
				result = MultipatchBuilderEx.FromEsriShape(byteArray, spatialReference);
				break;
			case ProSuiteGeometryType.Bag:
				result = GeometryBagBuilder.FromEsriShape(
					byteArray, spatialReference); // experimental
				break;

			default:
				throw new ArgumentOutOfRangeException(
					$"Unsupported geometry type {shapeType}");
		}

		return result;
	}

	[NotNull]
	private static ByteString GetWkbByteString(Multipatch multipatch)
	{
		List<Polyhedron> polyhedra = GeomConversionUtils.CreatePolyhedra(multipatch).ToList();

		WkbGeomWriter wkbWriter = new WkbGeomWriter
		                          {
			                          ReversePolygonWindingOrder = false
		                          };

		byte[] wkb = wkbWriter.WriteMultiSurface(polyhedra);

		// TODO: Consider using FromStream and avoid the extra copying step
		return ByteString.CopyFrom(wkb);
	}

	[NotNull]
	private static Geometry FromWkb([NotNull] byte[] wkb,
	                                [CanBeNull] SpatialReference spatialReference)
	{
		// TODO: Use ReadOnlyMemory<byte> to avoid extra copy step

		WkbGeomReader wkbReader = new WkbGeomReader();

		Stream memoryStream = new MemoryStream(wkb);
		IBoundedXY geom = wkbReader.ReadGeometry(memoryStream, out WkbGeometryType wkbType);

		// Currently the only types that cannot be transferred via esri shape are
		// WkbGeometryType.PolyhedralSurface and WkbGeometryType.MultiSurface

		if (wkbType != WkbGeometryType.PolyhedralSurface &&
		    wkbType != WkbGeometryType.MultiSurface)
		{
			throw new InvalidOperationException("Unexpected geometry types for wkb transfer");
		}

		if (geom is Polyhedron polyhedron)
		{
			return GeomConversionUtils.CreateMultipatch(polyhedron, spatialReference);
		}

		if (geom is MultiPolyhedron multiPolyhedron)
		{
			List<Multipatch> multipatches = new List<Multipatch>();

			int partId = 0;
			foreach (Polyhedron part in multiPolyhedron.Polyhedra)
			{
				multipatches.Add(
					GeomConversionUtils.CreateMultipatch(part, spatialReference, partId++));
			}

			return GeometryUtils.Union(multipatches);
		}

		throw new NotImplementedException();
	}
}
