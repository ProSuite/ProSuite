using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	internal static class GdbDatasetNameFactory
	{
		[NotNull]
		public static IDatasetName CreateDatasetName([NotNull] GdbWorkspace workspace,
		                                             [NotNull] GdbDatasetMoniker moniker)
		{
			if (moniker.IsFeatureClass)
			{
				return new GdbFeatureClassNameProxy(workspace, moniker);
			}

			return new GdbTableNameProxy(workspace, moniker);
		}
	}

	internal class GdbDatasetNameEnum : IEnumDatasetName
	{
		private readonly IList<IDatasetName> _datasetNames;
		private int _currentIndex;

		public GdbDatasetNameEnum([NotNull] IEnumerable<IDatasetName> datasetNames)
		{
			_datasetNames = datasetNames.ToList();
			_currentIndex = 0;
		}

		public IDatasetName Next()
		{
			if (_currentIndex >= _datasetNames.Count)
			{
				return null;
			}

			return _datasetNames[_currentIndex++];
		}

		public void Reset()
		{
			_currentIndex = 0;
		}
	}

	internal class GdbDatasetNameProxy : IName, IDatasetName
	{
		[NotNull] private readonly GdbWorkspace _workspace;
		[NotNull] protected readonly GdbDatasetMoniker Moniker;

		public GdbDatasetNameProxy([NotNull] GdbWorkspace workspace,
		                           [NotNull] GdbDatasetMoniker moniker)
		{
			_workspace = workspace;
			Moniker = moniker;
		}

		public string NameString
		{
			get => Moniker.Name;
			set { }
		}

		public virtual object Open()
		{
			return _workspace.OpenTable(Moniker.Name);
		}

		public string Name
		{
			get => Moniker.Name;
			set { }
		}

		public esriDatasetType Type => Moniker.DatasetType;

		public string Category
		{
			get => Moniker.Category;
			set { }
		}

		public IWorkspaceName WorkspaceName
		{
			get => (IWorkspaceName) ((IDataset) _workspace).FullName;
			set { }
		}

		public IEnumDatasetName SubsetNames => null;
	}

	internal class GdbTableNameProxy : GdbDatasetNameProxy, IObjectClassName, ITableName
	{
		public GdbTableNameProxy([NotNull] GdbWorkspace workspace,
		                         [NotNull] GdbDatasetMoniker moniker)
			: base(workspace, moniker) { }

		public int ObjectClassID => Moniker.ObjectClassId;
	}

	internal class GdbFeatureClassNameProxy : GdbTableNameProxy, IFeatureClassName
	{
		public GdbFeatureClassNameProxy([NotNull] GdbWorkspace workspace,
		                                [NotNull] GdbDatasetMoniker moniker)
			: base(workspace, moniker) { }

		public override object Open()
		{
			return ((IFeatureWorkspace) ((IName) ((IDatasetName) this).WorkspaceName).Open())
				.OpenFeatureClass(Moniker.Name);
		}

		esriGeometryType IFeatureClassName.ShapeType
		{
			get => Moniker.ShapeType;
			set { }
		}

		IDatasetName IFeatureClassName.FeatureDatasetName
		{
			get => null;
			set { }
		}

		esriFeatureType IFeatureClassName.FeatureType
		{
			get => Moniker.FeatureType;
			set { }
		}

		string IFeatureClassName.ShapeFieldName
		{
			get => Moniker.ShapeFieldName;
			set { }
		}
	}
}
