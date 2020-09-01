using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.AGP.Storage;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProSuite.AGP.WorkList.Test
{
	public class IssuesStateJsonRepository : JsonRepository<GdbRowIdentity>
	{
		private readonly string _issueStatePath;

		public IssuesStateJsonRepository(string filename) : base( filename, new JsonGdbIdentityConverter())
		//public IssuesStateJsonRepository(string filename) : base(filename)
		{
			_issueStatePath = filename;
		}
	}

	public class JsonGdbIdentityConverter : JsonConverter<IList<GdbRowIdentity>>
	{
		public override bool CanWrite => false;

		public override void WriteJson(JsonWriter writer, IList<GdbRowIdentity> value, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}

		public override IList<GdbRowIdentity> ReadJson(JsonReader reader, Type objectType, IList<GdbRowIdentity> existingValue, bool hasExistingValue,
		                                               JsonSerializer serializer)
		{
			var rowIdentities = new List<GdbRowIdentity>();
			var jArray = JArray.Load(reader);

			foreach (var jToken in jArray)
				rowIdentities.Add(CreateGdbRowIdentity(jToken));

			return rowIdentities;
		}

		// temporary parsing - via annotations is besser 
		private GdbRowIdentity CreateGdbRowIdentity(JToken token)
		{
			long id = -1;
			string tableName = null;
			long tableId = -1;
			string wksPath, wksInstance, wksUser, wksversion;
			wksPath = wksInstance = wksUser = wksversion = "";

			var prop = token?.Children().First();
			while (prop != null)
			{
				var objectProperty = prop.ToObject<JProperty>();
				if (objectProperty?.Name == "ObjectId")
					id = objectProperty.Value.ToObject<long>();

				if (objectProperty?.Name == "Table")
				{
					var tableProps = GetTokenProperties(objectProperty.Value);
					if (tableProps.TryGetValue("Id", out object objValue ))
					{
						tableId = long.Parse(objValue.ToString());
					}
					if (tableProps.TryGetValue("Name", out object objNameValue))
					{
						tableName = objNameValue.ToString();
					}
					if (tableProps.TryGetValue("Workspace", out object objWksValue))
					{
						var workspaceProps = GetTokenProperties(objWksValue as JToken);
						if (workspaceProps.TryGetValue("_path", out object objPathValue))
						{
							wksPath = objPathValue.ToString();
						}
						if (workspaceProps.TryGetValue("_user", out object objUserValue))
						{
							wksUser = objUserValue.ToString();
						}
						if (workspaceProps.TryGetValue("_version", out object objVersionValue))
						{
							wksversion = objVersionValue.ToString();
						}
						if (workspaceProps.TryGetValue("_instance", out object objInstanceValue))
						{
							wksInstance = objInstanceValue.ToString();
						}

					}
				}
				prop = prop.Next;
			}
			return new GdbRowIdentity(
				id,
				new GdbTableIdentity(
					tableName, tableId, new GdbWorkspaceIdentity(wksPath, wksInstance, wksUser, wksversion)));
		}

		private Dictionary<string, object> GetTokenProperties(JToken token)
		{
			var retVals = new Dictionary<string, object>();

			var tokenChilds = token.Children();
			foreach (var child in tokenChilds)
			{
				var childProps = child.ToObject<JProperty>();
				if (childProps != null)
					retVals.Add(childProps.Name , childProps.Value);	
			}
			return retVals;
		}

		//public override void WriteJson<T>(JsonWriter writer, T value, JsonSerializer serializer)
		//{
		//	//writer.WriteStartObject();
		//	//writer.WritePropertyName("$" + value.ObjectId);
		//	//writer.WriteStartObject();
		//	//writer.WritePropertyName("Table");
		//	//serializer.Serialize(writer, value.Table);
		//	//writer.WriteEndObject();
		//	//writer.WriteEndObject();
		//	throw new NotImplementedException();
		//}

	}

}
