using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public static class ModelElementUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[CLSCompliant(false)]
		public static bool UseCaseSensitiveSql([NotNull] ITable table,
		                                       SqlCaseSensitivity caseSensitivity)
		{
			switch (caseSensitivity)
			{
				case SqlCaseSensitivity.CaseInsensitive:
					if (_msg.IsVerboseDebugEnabled)
					{
						_msg.VerboseDebugFormat("{0}: not case sensitive",
						                        DatasetUtils.GetName(table));
					}

					return false;

				case SqlCaseSensitivity.CaseSensitive:
					if (_msg.IsVerboseDebugEnabled)
					{
						_msg.VerboseDebugFormat("{0}: case sensitive",
						                        DatasetUtils.GetName(table));
					}

					return true;

				case SqlCaseSensitivity.SameAsDatabase:
					var sqlSyntax = DatasetUtils.GetWorkspace(table) as ISQLSyntax;
					bool result = sqlSyntax != null && sqlSyntax.GetStringComparisonCase();

					if (_msg.IsVerboseDebugEnabled)
					{
						_msg.VerboseDebugFormat(sqlSyntax == null
							                        ? "{0}: database case sensitivity: UNKNOWN (use {1})"
							                        : "{0}: database case sensitivity: {1}",
						                        DatasetUtils.GetName(table), result);
					}

					return result;

				default:
					throw new InvalidOperationException(
						string.Format("Unsupported SqlCaseSensitivity: {0}", caseSensitivity));
			}
		}
	}
}
