using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using Path = System.IO.Path;

namespace ProSuite.QA.Tests
{
	public class QaExportTables : ContainerTest
	{
		[NotNull] private readonly string _fileGdbPath;

		private IList<ITable> _exportTables;
		private IList<Dictionary<int, int>> _fieldMappings;

		public QaExportTables(
			[NotNull] IList<ITable> tables,
			[NotNull] string fileGdbPath) : base(tables)
		{
			_fileGdbPath = fileGdbPath;
		}

		protected override int CompleteTileCore(TileInfo tileInfo)
		{
			if (tileInfo.State == TileState.Initial)
			{
				if (File.Exists(_fileGdbPath))
				{
					File.Delete(_fileGdbPath);
				}

				if (Directory.Exists(_fileGdbPath))
				{
					Directory.Delete(_fileGdbPath, recursive: true);
				}

				IWorkspaceName wsName = WorkspaceUtils.CreateFileGdbWorkspace(
					Assert.NotNull(Path.GetDirectoryName(_fileGdbPath)),
					Path.GetFileName(_fileGdbPath));
				var ws = (IFeatureWorkspace)((IName)wsName).Open();

				_exportTables = new List<ITable>();
				_fieldMappings = new List<Dictionary<int, int>>();
				int i = 0;
				foreach (ITable involvedTable in InvolvedTables)
				{
					string name = $"table_{i}";
					_exportTables.Add(CreateTable(ws, involvedTable, name,
																				out Dictionary<int, int> fieldMappings));
					_fieldMappings.Add(fieldMappings);
					i++;
				}
			}

			return base.CompleteTileCore(tileInfo);
		}

		private string GetValidFieldName(string name, Dictionary<string, IField> fieldDict)
		{
			string validRoot = name.Replace(".", "_");
			if (!fieldDict.ContainsKey(validRoot))
			{
				return validRoot;
			}

			int idx = Enumerable.Range(1, int.MaxValue)
													.First(x => !fieldDict.ContainsKey($"{validRoot}_{x}"));
			string validIndexed = $"{validRoot}_{idx}";
			return validIndexed;
		}

		private ITable CreateTable(IFeatureWorkspace ws, ITable involvedTable, string name,
															 out Dictionary<int, int> fieldMappings)
		{
			IFields sourceFields = involvedTable.Fields;

			Dictionary<string, IField> fieldDict =
				new Dictionary<string, IField>(StringComparer.InvariantCultureIgnoreCase);
			for (int iField = 0; iField < sourceFields.FieldCount; iField++)
			{
				IField sourceField = sourceFields.Field[iField];
				fieldDict.Add(GetValidFieldName(sourceField.Name, fieldDict), sourceField);
			}

			IFeatureClass fc = involvedTable as IFeatureClass;
			string shapeFieldName = fc?.ShapeFieldName;

			string oidFieldName = GetValidFieldName("OBJECTID", fieldDict);

			List<IField> exportFields = new List<IField>();
			exportFields.Add(FieldUtils.CreateOIDField(oidFieldName));
			foreach (var pair in fieldDict)
			{
				IField sourceField = pair.Value;
				IField exportField = null;

				string exportName = pair.Key;
				esriFieldType fieldType = sourceField.Type;
				if (sourceField.Type == esriFieldType.esriFieldTypeOID)
				{
					fieldType = esriFieldType.esriFieldTypeInteger;
				}
				else if (sourceField.Type ==
								 esriFieldType.esriFieldTypeGlobalID)
				{
					fieldType = esriFieldType.esriFieldTypeGUID;
				}
				else if (sourceField.Type ==
								 esriFieldType.esriFieldTypeGeometry)
				{
					if (!sourceField.Name.Equals(fc?.ShapeFieldName,
																				StringComparison.InvariantCultureIgnoreCase))
					{
						//
						exportField = FieldUtils.CreateTextField(exportName, 50);
					}
					else
					{
						exportField = sourceField;
						if (exportField.Name != exportName)
						{
							exportField = (IField)((IClone)exportField).Clone();
							((IFieldEdit)exportField).Name_2 = exportName;

							if (sourceField.Name == fc?.ShapeFieldName)
							{
								shapeFieldName = exportName;
							}
						}
					}
				}
				else if (sourceField.Type == esriFieldType.esriFieldTypeBlob &&
								 sourceField.Name.IndexOf(InvolvedRowUtils.BaseRowField,
																					StringComparison.InvariantCultureIgnoreCase) >= 0)
				{
					exportName = GetValidFieldName(exportName, fieldDict);
					exportField = FieldUtils.CreateTextField(exportName, 250);
				}

				exportFields.Add(exportField ?? FieldUtils.CreateField(exportName, fieldType));
			}

			ITable created;
			if (fc != null)
			{
				created = (ITable)DatasetUtils.CreateSimpleFeatureClass(
					ws, name, FieldUtils.CreateFields(exportFields),
					shapeFieldName: shapeFieldName);
			}
			else
			{
				created = DatasetUtils.CreateTable(ws, name, null,
																					 FieldUtils.CreateFields(exportFields));
			}

			var mappings = new Dictionary<int, int>();
			for (int iField = 1; iField < exportFields.Count; iField++) // Ignore first field : OID-Field !
			{
				int mapped = created.FindField(exportFields[iField].Name);
				mappings.Add(iField, mapped);
			}

			fieldMappings = mappings;
			return created;
		}

		protected override int ExecuteCore(IRow row, int tableIndex)
		{
			ITable target = _exportTables[tableIndex];
			IRow targetRow = target.CreateRow();
			for (int iSourceField = 0; iSourceField < row.Fields.FieldCount; iSourceField++)
			{
				if (! _fieldMappings[tableIndex].TryGetValue(
					    iSourceField + 1, out int iTargetField))
				{
					continue;
				}

				if (iTargetField < 0)
				{
					continue;
				}
				object sourceValue = row.Value[iSourceField];
				object targetValue = sourceValue;

				if (sourceValue is IGeometry)
				{
					if (targetRow.Fields.Field[iTargetField].Type !=
							esriFieldType.esriFieldTypeGeometry)
					{
						targetValue = "additional geometry value (supressed)";
					}
				}
				else if (sourceValue is IList<IRow> baseRows)
				{
					InvolvedRows involveds = InvolvedRowUtils.GetInvolvedRows(baseRows);
					string fullList = string.Concat(involveds.Select(x => $"{x};"));
					int maxLength = target.Fields.Field[iTargetField].Length;
					if (fullList.Length > maxLength)
					{
						targetValue = fullList.Substring(0, maxLength - 4) + "...";
					}
					else
					{
						targetValue = fullList;
					}
				}

				bool ignore = true;
				try
				{
					targetRow.Value[iTargetField] = targetValue;
				}
				catch (Exception e)
				{
					if (!ignore)
					{
						throw e;
					}
				}
			}

			targetRow.Store();
			return 0;
		}
	}
}
