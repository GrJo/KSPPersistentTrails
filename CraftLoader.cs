using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using UnityEngine;
using System.IO;

namespace PersistentTrails
{
    public static class CraftLoader
    {
        public static List<PartValue> getParts(Vessel vessel, bool fetchModel)
        {
            List<PartValue> partList = new List<PartValue>();
            Vector3 rootPosition;
            Quaternion rootRotation;
            Transform referenceFrame = new GameObject().transform;
            Transform localTransform = new GameObject().transform;
            localTransform.parent = referenceFrame;
            if (vessel != null)
            {
                if (vessel.parts.Count > 0)
                {
                    Debug.Log("vessel parts: " + vessel.parts.Count);
                    rootPosition = vessel.parts[0].transform.position;
                    rootRotation = vessel.parts[0].transform.rotation;
                    Quaternion worldUp = Quaternion.Euler((vessel.rigidbody.position - vessel.mainBody.position).normalized);
                    referenceFrame.position = vessel.transform.position;
                    referenceFrame.rotation = vessel.transform.rotation;
                    foreach (Part part in vessel.parts)
                    {
                        PartValue newPartValue = new PartValue();
                        newPartValue.scale = 1f / part.scaleFactor;
                        localTransform.rotation = part.transform.rotation;
                        localTransform.position = part.transform.position;
                        newPartValue.position = localTransform.localPosition;
                        newPartValue.rotation = localTransform.localRotation;
                        newPartValue.partName = part.name.Split(' ')[0];
                        if (fetchModel) newPartValue.model = findPartModel(newPartValue.partName);
                        partList.Add(newPartValue);
                    }
                    Debug.Log("partList count: " + partList.Count);
                }
            }
            return partList;
        }

        private static string serialize()
        {
            string output = String.Concat(Utilities.craftFileFormat, "\n");
            foreach (PartValue value in getParts(FlightGlobals.ActiveVessel, false))
            {
                output = String.Concat(output, value.partName, "\n");
                output = String.Concat(output, value.position, "\n");
                output = String.Concat(output, value.rotation, "\n");
                output = String.Concat(output, value.scale, "\n");
            }
            output = String.Concat(output, "[EOF]");
            return output;
        }        

        public static void saveCraftToFile()
        {
            StreamWriter stream = new StreamWriter(Utilities.CraftPath + FlightGlobals.ActiveVessel.vesselName + ".crf");
            stream.WriteLine(serialize());
            stream.Close(); 
        }

        public static List<PartValue> loadCraftFromFile(string fileName)
        {
            List<PartValue> loadedList = new List<PartValue>();
            PartValue newValue = new PartValue();
            StreamReader stream = new StreamReader(fileName);
            string newLine = string.Empty;
            int craftFileFormat = 0;
            int.TryParse(stream.ReadLine(), out craftFileFormat);
            Debug.Log(String.Concat("Loading crf file, format ", craftFileFormat, ", ", fileName));
            try
            {
                while (!stream.EndOfStream && !(newLine == "[EOF]"))
                {
                    newLine = stream.ReadLine();
                    newValue.partName = newLine;                    
                    newValue.position = Utilities.parseVector3(stream.ReadLine());
                    newValue.rotation = Utilities.parseQuaternion(stream.ReadLine());
                    float.TryParse(stream.ReadLine(), out newValue.scale);
                    newValue.model = findPartModel(newValue.partName);
                    loadedList.Add(newValue.clone());
                }                
            }
            catch (Exception e)
            {
                Debug.Log("load craft file error: " + e.ToString());
            }
            return loadedList;
        }

        public static GameObject assembleCraft(string craftName) // --- craftName not actually used yet. This should take a saved craft file name as input ---
        {
            GameObject craft = new GameObject();
            //List<PartValue> pvList = getParts(FlightGlobals.ActiveVessel, true); // load the craft file here into a partValue list
            List<PartValue> pvList = loadCraftFromFile(Utilities.CraftPath + "beech.crf"); // ---- test! ----
            foreach (PartValue pv in pvList)
            {                
                pv.model.SetActive(true);
                pv.model.transform.parent = craft.transform;
                pv.model.transform.localPosition = pv.position;
                pv.model.transform.localRotation = pv.rotation;
                if (pv.scale > 10f) pv.scale /= 100f;
                pv.model.transform.localScale = new Vector3(pv.scale, pv.scale, pv.scale);
                Debug.Log("Part: " + pv.partName + "Scale: " + pv.scale + "/" + pv.model.transform.localScale);
                //Debug.Log("Part: " + pv.position);
                //Debug.Log("Part: " + pv.rotation);
                //Debug.Log("Part: " + pv.scale);
            }
            return craft;
        }

        public static GameObject findPartModel(string partName)
        {
            UrlDir.UrlConfig[] cfg = GameDatabase.Instance.GetConfigs("PART");
            //Debug.Log("looping through " + cfg.Length);
            for (int i = 0; i < cfg.Length; i++)
            {
                if (partName == cfg[i].name)
                {
                    //Debug.Log("found this part: " + cfg[i].url);
                    //float scale = 0.1337f;
                    //float.TryParse(cfg[i].config.GetValue("scale"), out scale);
                    //Debug.Log("scale: " + scale);
                    string modelPath = cfg[i].parent.parent.url + "/" + "model";
                    //Debug.Log("model path: " + modelPath);
                    GameObject newModel = GameDatabase.Instance.GetModel(modelPath);
                    if (newModel == null)
                    {
                        //Debug.Log("model load error, fetching first model available");
                        newModel = GameDatabase.Instance.GetModelIn(cfg[i].parent.parent.url);
                        return newModel;
                        //return new PartValue(newModel, scale);
                    }
                    else
                    {
                        //Debug.Log("newModel not null");
                        return newModel;
                    }
                }
            }
            Debug.Log("Finding model " + partName + " failed, returning blank GameObject");
            return new GameObject();
        }
    }  

    public class PartValue
    {
        public string partName;
        public GameObject model;
        public Vector3 position;
        public Quaternion rotation;
        //public Quaternion attachRotation;
        public float scale;

        public PartValue(GameObject _model, float _scale)
        {
            model = _model;
            scale = _scale;
        }

        public PartValue()
        {
        }

        public PartValue clone()
        {
            PartValue cloneValue = new PartValue();
            cloneValue.partName = partName;
            cloneValue.position = position;
            cloneValue.rotation = rotation;
            cloneValue.scale = scale;
            return cloneValue;
        }
    }
}
