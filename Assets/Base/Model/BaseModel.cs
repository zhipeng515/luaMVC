using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;

[System.Serializable]
public class BaseModel
{

    public void Save(string filename)
    {
        string path = Application.persistentDataPath + "/" + this.GetType().Name + "/";
        if (!System.IO.Directory.Exists(path))
        {
            System.IO.Directory.CreateDirectory(path);
        }
        path += filename;

#if UNITY_EDITOR
        string payload = JsonConvert.SerializeObject(this);
        System.IO.File.WriteAllText(path, payload);
#else
        FileStream fs = new FileStream (path, FileMode.Create);
        BinaryFormatter formatter = new BinaryFormatter ();
        formatter.Serialize (fs, this);
        fs.Close ();
#endif
    }

    public static bool Load<T>(string filename, ref T outModel) where T : BaseModel
    {
        string path = Application.persistentDataPath + "/" + typeof(T).Name + "/" + filename;
        if (System.IO.File.Exists(path))
        {
#if UNITY_EDITOR
            string jsonPayload = System.IO.File.ReadAllText(path);
            outModel = (T)JsonConvert.DeserializeObject(jsonPayload, outModel.GetType());
#else
            FileStream fs = new FileStream (path, FileMode.Open);
            BinaryFormatter formatter = new BinaryFormatter ();
            outModel = (T)formatter.Deserialize (fs);
            fs.Close ();
#endif
            return true;
		}

		return false;
	}
	public static bool Remove<T>(string filename) where T : BaseModel
	{
		string path = Application.persistentDataPath + "/" + typeof(T).Name + "/" + filename;
		if (System.IO.File.Exists (path)) {
			System.IO.File.Delete (path);
			return true;
		}

		return false;
	}
}
