extern alias EsriGeodatabase;
extern alias EsriSystem;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.ArcGIS.Geodatabase.AO
{
	public class ArcSet : ISet
	{
		private readonly EsriSystem::ESRI.ArcGIS.esriSystem.ISet _aoSet;

		public  ArcSet(EsriSystem::ESRI.ArcGIS.esriSystem.ISet aoSet)
		{
			_aoSet = aoSet;
		}

		public EsriSystem::ESRI.ArcGIS.esriSystem.ISet AoSet => _aoSet;

		#region Implementation of ISet

		public void Add(object unk)
		{
			_aoSet.Add(unk);
		}

		public void Remove(object unk)
		{
			_aoSet.Remove(unk);
		}

		public void RemoveAll()
		{
			_aoSet.RemoveAll();
		}

		public bool Find(object unk)
		{
			return _aoSet.Find(unk);
		}

		public object Next()
		{
			return _aoSet.Next();
		}

		public void Reset()
		{
			_aoSet.Reset();
		}

		public int Count => _aoSet.Count;

		#endregion
	}
}
