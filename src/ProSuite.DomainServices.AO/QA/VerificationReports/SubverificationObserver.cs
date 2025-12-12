using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Progress;

namespace ProSuite.DomainServices.AO.QA.VerificationReports
{
	public class SubVerificationObserver : ISubVerificationObserver
	{
		public const string SubVeriIdField = "SubVeriId";
		public const string StatusField = "Status";
		public const string CreatedField = "Created";
		public const string UpdatedField = "Updated";
		public const string StartedField = "Started";
		public const string FinishedField = "Finished";
		public const string WorkerAdressField = "WorkerAdress";

		private readonly Dictionary<string, int> _idxs = new Dictionary<string, int>();

		[CanBeNull] private readonly ISpatialReference _spatialReference;
		private readonly IFeatureClass _fc;

		private readonly Dictionary<int, long> _subverIdOid;

		public SubVerificationObserver(
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] string featureClassName,
			[CanBeNull] ISpatialReference spatialReference)
		{
			_subverIdOid = new Dictionary<int, long>();

			_spatialReference = spatialReference;
			_fc = DatasetUtils.CreateSimpleFeatureClass(
				workspace, featureClassName, GetFields(spatialReference));
		}

		public void CreatedSubverification(
			int idSubverification, EnvelopeXY area)
		{
			IGeometry shape = area != null
				                  ? GeometryFactory.CreatePolygon(area, _spatialReference)
				                  : null;

			IFeature f = _fc.CreateFeature();
			f.Value[GetIdx(SubVeriIdField)] = idSubverification;
			f.Value[GetIdx(StatusField)] = -1;
			f.Value[GetIdx(CreatedField)] = DateTime.Now;
			f.Value[GetIdx(UpdatedField)] = DateTime.Now;

			f.Shape = shape;
			f.Store();

			_subverIdOid.Add(idSubverification, f.OID);
		}

		public void Started(int id, string workerAddress)
		{
#if ARCGIS_11_0_OR_GREATER
			if (_subverIdOid.TryGetValue(id, out long oid))
			{
				IFeature f = _fc.GetFeature(oid);
				f.Value[GetIdx(UpdatedField)] = DateTime.Now;

				f.Value[GetIdx(StartedField)] = DateTime.Now;
				f.Value[GetIdx(StatusField)] = (int) ServiceCallStatus.Running;
				f.Value[GetIdx(WorkerAdressField)] = workerAddress;
				f.Store();
			}
#endif
		}

		public void Finished(int id, ServiceCallStatus status)
		{
#if ARCGIS_11_0_OR_GREATER
			if (_subverIdOid.TryGetValue(id, out long oid))
			{
				IFeature f = _fc.GetFeature(oid);
				f.Value[GetIdx(UpdatedField)] = DateTime.Now;

				f.Value[GetIdx(FinishedField)] = DateTime.Now;
				f.Value[GetIdx(StatusField)] = (int) status;
				f.Store();
			}
#endif
		}

		[NotNull]
		private static IFields GetFields([CanBeNull] ISpatialReference spatialReference,
		                                 double gridSize1 = 0d,
		                                 double gridSize2 = 0d,
		                                 double gridSize3 = 0d)
		{
			return FieldUtils.CreateFields(
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateIntegerField(SubVeriIdField),
				FieldUtils.CreateIntegerField(StatusField),
				FieldUtils.CreateDateField(CreatedField),
				FieldUtils.CreateDateField(UpdatedField),
				FieldUtils.CreateDateField(StartedField),
				FieldUtils.CreateDateField(FinishedField),
				FieldUtils.CreateTextField(WorkerAdressField, 200),
				FieldUtils.CreateShapeField(esriGeometryType.esriGeometryPolygon,
				                            spatialReference ?? new UnknownCoordinateSystemClass(),
				                            gridSize1, gridSize2, gridSize3)
			);
		}

		private int GetIdx(string field)
		{
			if (! _idxs.TryGetValue(field, out int idx))
			{
				idx = _fc.FindField(field);
				_idxs.Add(field, idx);
			}

			return idx;
		}

		#region IDisposable

		public void Dispose()
		{
			if (_spatialReference != null)
			{
				Marshal.ReleaseComObject(_spatialReference);
			}

			if (_fc != null)
			{
				Marshal.ReleaseComObject(_fc);
			}
		}

		#endregion
	}
}
