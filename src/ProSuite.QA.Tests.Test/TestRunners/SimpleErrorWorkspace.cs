using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Test.TestRunners
{
	public class SimpleErrorWorkspace
	{
		private class ErrorTable
		{
			private readonly IFeatureWorkspace _ws;
			private readonly string _name;
			private readonly esriGeometryType _geometryType;
			private ITable _table;

			private QaError _currentError;

			public ErrorTable(
				[NotNull] IFeatureWorkspace ws,
				[NotNull] string name,
				esriGeometryType geometryType)
			{
				_ws = ws;
				_name = name;
				_geometryType = geometryType;
			}

			public void Add(QaError qaError)
			{
				((IWorkspaceEdit) _ws).StartEditing(false);
				_currentError = qaError;
				IRow row = Table.CreateRow();
				if (qaError.Geometry != null)
				{
					((IFeature) row).Shape = qaError.Geometry;
				}

				row.set_Value(1, qaError.Description);
				row.Store();
				((IWorkspaceEdit) _ws).StopEditing(true);
			}

			private ITable Table
			{
				get
				{
					if (_table == null)
					{
						IFieldsEdit fields = new FieldsClass();
						fields.AddField(FieldUtils.CreateOIDField());
						fields.AddField(FieldUtils.CreateTextField("Description", 1024));

						if (_currentError.Geometry != null)
						{
							ISpatialReference sr =
								_currentError.Geometry.SpatialReference ??
								SpatialReferenceUtils.CreateSpatialReference
								((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
								 true);
							fields.AddField(FieldUtils.CreateShapeField(
								                "Shape", _geometryType, sr, 1000, true, false));

							_table =
								(ITable) DatasetUtils.CreateSimpleFeatureClass(
									_ws, _name, fields, null);
						}
						else
						{
							_table = DatasetUtils.CreateTable(_ws, _name, null, fields);
						}

						((IWorkspaceEdit) _ws).StartEditing(false);
						((IWorkspaceEdit) _ws).StopEditing(true);
					}

					return _table;
				}
			}
		}

		private readonly IFeatureWorkspace _ws;
		private readonly Dictionary<esriGeometryType, ErrorTable> _errorTables;

		[CLSCompliant(false)]
		public SimpleErrorWorkspace([NotNull] IFeatureWorkspace ws)
		{
			_ws = ws;
			_errorTables = new Dictionary<esriGeometryType, ErrorTable>();
		}

		public void TestContainer_QaError(object sender, QaErrorEventArgs e)
		{
			esriGeometryType geomType;

			if (e.QaError.Geometry != null)
			{
				geomType = e.QaError.Geometry.GeometryType;
			}
			else
			{
				geomType = esriGeometryType.esriGeometryNull;
			}

			ErrorTable errorTable;
			if (! _errorTables.TryGetValue(geomType, out errorTable))
			{
				errorTable = new ErrorTable(_ws, geomType.ToString(), geomType);
				_errorTables.Add(geomType, errorTable);
			}

			errorTable.Add(e.QaError);
		}
	}
}
