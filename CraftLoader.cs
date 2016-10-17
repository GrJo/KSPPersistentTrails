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
                    //Debug.Log("vessel parts: " + vessel.parts.Count);
                    rootPosition = vessel.parts[0].transform.position;
                    rootRotation = vessel.parts[0].transform.rotation;
                    Quaternion worldUp = Quaternion.Euler((vessel.GetComponent<Rigidbody>().position - vessel.mainBody.position).normalized);
                    referenceFrame.position = vessel.transform.position;
                    referenceFrame.rotation = vessel.transform.rotation;
                    foreach (Part part in vessel.parts)
                    {
                        if (part.name == "launchClamp1" || part.partName == "StrutConnector" || part.partName == "FuelLine")
                        {
                            Utilities.debug.debugMessage("Excluding part from crf file: " + part.name);                            
                        }
                        else
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
                    }                    
                }
            }
            return partList;
        }        

        private static string serialize()
        {
            StringBuilder output = new StringBuilder();
            output.AppendLine(Utilities.craftFileFormat.ToString());
            foreach (PartValue value in getParts(FlightGlobals.ActiveVessel, false))
            {
                output.AppendLine(value.partName);
                output.AppendLine(Utilities.Vector3ToString(value.position));
                output.AppendLine(Utilities.QuaternionToString(value.rotation));
                output.AppendLine(value.scale.ToString());
            }
            output.Append("[EOF]");
            return output.ToString();
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
            StreamReader stream = new StreamReader(fileName); // exceptions handled by assembleCraft
            string newLine = string.Empty;
            int craftFileFormat = 0;
            int.TryParse(stream.ReadLine(), out craftFileFormat);
            Utilities.debug.debugMessage(String.Concat("Loading crf file, format ", craftFileFormat, ", ", fileName));
            try
            {
                while (!stream.EndOfStream && !(newLine == "[EOF]"))
                {
                    newLine = stream.ReadLine();
                    newValue.partName = newLine;                    
                    newValue.position = Utilities.parseVector3(stream.ReadLine());
                    newValue.rotation = Utilities.parseQuaternion(stream.ReadLine());
                    float.TryParse(stream.ReadLine(), out newValue.scale);
                    //Debug.Log("finding model " + newValue.partName);
                    newValue.model = findPartModel(newValue.partName);
                    //Debug.Log("model null? " + (newValue.model == null));
                    loadedList.Add(newValue.clone());
                }                
            }
            catch (Exception e)
            {
                //Utilities.debug.debugMessage("load craft file error: " + e.ToString());
                Utilities.debug.debugMessage("Got expected exception from streamreader, part list done");
            }
            return loadedList;
        }

        public static GameObject assembleCraft(string craftName, bool collidersOn) // --- craftName not actually used yet. This should take a saved craft file name as input ---
        {
            GameObject craft = new GameObject();
            Utilities.debug.debugMessage("asembling craft " + craftName);
            List<PartValue> pvList;
            //List<PartValue> pvList = getParts(FlightGlobals.ActiveVessel, true); // load the craft file here into a partValue list
            try
            {
                pvList = loadCraftFromFile(craftName);
            }
            catch
            {
                throw new FileNotFoundException("error loading craft from file", craftName);
            }
            foreach (PartValue pv in pvList)
            {
                //Debug.Log("pv.partName is " + pv.partName);
                pv.model.SetActive(true);
                //Debug.Log("pv.model exists");
                pv.model.transform.parent = craft.transform;
                pv.model.transform.localPosition = pv.position;
                pv.model.transform.localRotation = pv.rotation;
                if (pv.scale > 7f) pv.scale /= 10f;
                if (pv.scale > 7f) pv.scale /= 10f; // twice to catch both 0.01 scale parts, and 0.1 scales. Gotta find a better way. Need to read the part cfg scale
                pv.model.transform.localScale = new Vector3(pv.scale, pv.scale, pv.scale);
                //Debug.Log("Part: " + pv.partName + "Scale: " + pv.scale + "/" + pv.model.transform.localScale);
                //Debug.Log("Part: " + pv.position);
                //Debug.Log("Part: " + pv.rotation);
                //Debug.Log("Part: " + pv.scale);
            }
            setColliderStateInChildren(craft, collidersOn);
            //setLightStateInChildren(craft, false);
            //setLadderStateInChildren(craft, false);
            return craft;
        }

        public static GameObject findPartModel(string partName)
        {
            UrlDir.UrlConfig[] cfg = GameDatabase.Instance.GetConfigs("PART");
            
            for (int i = 0; i < cfg.Length; i++)
            {
                string modfiedPartName = partName.Replace('.', '_');
                if (modfiedPartName == cfg[i].name)
                {
                    Utilities.debug.debugMessage("found this part: " + cfg[i].url);
                    string modelpath = "";
                    string meshname = "";
                    cfg[i].config.TryGetValue("mesh", ref meshname);
                    if (meshname != "")
                    {
                        //Utilities.debug.debugMessage("Found mesh field");
                        int dotlocation = meshname.LastIndexOf(".");

                        if (dotlocation == -1)
                        {
                            //no ".mu"
                            //Utilities.debug.debugMessage("no .mu");
                            modelpath = cfg[i].parent.parent.url + "/" + meshname;
                        }
                        else
                        {
                            //has ".mu"
                            //Utilities.debug.debugMessage("has .mu");
                            meshname = meshname.Substring(0, dotlocation);
                            modelpath = cfg[i].parent.parent.url + "/" + meshname;
                        }
                    }
                    else
                    {
                        //Utilities.debug.debugMessage("No mesh field try MODEL node");
                        ConfigNode node = new ConfigNode();
                        if (cfg[i].config.TryGetNode("MODEL", ref node))
                        {
                            //Utilities.debug.debugMessage("MODEL node found");
                            if (node.TryGetValue("model", ref meshname))
                            {
                                //Debug.LogUtilities.debug.debugMessage("model field found");
                                modelpath = meshname;
                            }
                        }
                    }
                    //float scale = 0.1337f;
                    //float.TryParse(cfg[i].config.GetValue("scale"), out scale);
                    //Utilities.debug.debugMessage("scale: " + scale);
                    //string modelPath = cfg[i].parent.parent.url + "/" + "model";
                    //string modelPath = cfg[i].parent.url;
                    Utilities.debug.debugMessage("model path: " + modelpath);
                    GameObject newModel = null;
                    if (modelpath != "")
                        newModel = GameDatabase.Instance.GetModel(modelpath);
                    if (newModel == null)
                    {
                        Utilities.debug.debugMessage("model load error, fetching first model available");
                        newModel = GameDatabase.Instance.GetModelIn(cfg[i].parent.parent.url);
                        return newModel;
                        //return new PartValue(newModel, scale);
                    }
                    else
                    {
                        //Utilities.debug.debugMessage("newModel not null");
                        return newModel;
                    }
                }
            }
            Utilities.debug.debugMessage("Finding model " + partName + " failed, returning blank GameObject");
            return new GameObject();
        }

        public static void setColliderStateInChildren(GameObject rootObject, bool newValue)
        {
            //disable colliders by setting them to isTrigger so you can still run code on them
            Collider[] colliders = rootObject.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].isTrigger = !newValue;
                if (!(colliders[i] is WheelCollider))
                    colliders[i].material = getPhysicMaterial();
            }
        }

        public static void setLightStateInChildren(GameObject rootObject, bool newValue)
        {
            Light[] lights = rootObject.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i].enabled = newValue;                
            }
            ModuleAnimateGeneric[] lights2 = rootObject.GetComponentsInChildren<ModuleAnimateGeneric>(true);
            for (int i = 0; i < lights2.Length; ++i)
            {
                if (lights2[i].defaultActionGroup == KSPActionGroup.Light)
                {
                    lights2[i].Toggle();
                }
            }
        }

        public static void setLadderStateInChildren(GameObject rootObject, bool newValue)
        {
            RetractableLadder[] ladders = rootObject.GetComponentsInChildren<RetractableLadder>(true);
            for (int i = 0; i < ladders.Length; i++)
            {
                if (newValue)
                    ladders[i].Extend();
                else
                    ladders[i].Retract();
            }
        }

        private static PhysicMaterial ghostPhysicMaterial;
        public static PhysicMaterial getPhysicMaterial()
        {
            if (ghostPhysicMaterial == null)
            {
                ghostPhysicMaterial = new PhysicMaterial("ghostPhysicMat");
                ghostPhysicMaterial.dynamicFriction = 0.3f;
                //ghostPhysicMaterial.dynamicFriction2 = 0.3f;
                ghostPhysicMaterial.frictionCombine = PhysicMaterialCombine.Average;
                ghostPhysicMaterial.staticFriction = 0.3f;
                //ghostPhysicMaterial.staticFriction2 = 0.3f;
            }
            return ghostPhysicMaterial;
        }

        //public static void setColliderState(GameObject targetObject)
        //{

        //}
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
            cloneValue.model = model;
            return cloneValue;
        }
    }
}
