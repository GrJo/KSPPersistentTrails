/**
 * Utilities.cs
 * 
 * Thunder Aerospace Corporation's library for the Kerbal Space Program, by Taranis Elsu
 * 
 * (C) Copyright 2013, Taranis Elsu
 * 
 * Kerbal Space Program is Copyright (C) 2013 Squad. See http://kerbalspaceprogram.com/. This
 * project is in no way associated with nor endorsed by Squad.
 * 
 * This code is licensed under the Attribution-NonCommercial-ShareAlike 3.0 (CC BY-NC-SA 3.0)
 * creative commons license. See <http://creativecommons.org/licenses/by-nc-sa/3.0/legalcode>
 * for full details.
 * 
 * Attribution — You are free to modify this code, so long as you mention that the resulting
 * work is based upon or adapted from this code.
 * 
 * Non-commercial - You may not use this work for commercial purposes.
 * 
 * Share Alike — If you alter, transform, or build upon this work, you may distribute the
 * resulting work only under the same or similar license to the CC BY-NC-SA 3.0 license.
 * 
 * Note that Thunder Aerospace Corporation is a ficticious entity created for entertainment
 * purposes. It is in no way meant to represent a real entity. Any similarity to a real entity
 * is purely coincidental.
 */

using System.IO;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PersistentTrails
{
    public class Utilities
    {
        public static String AppPath = KSPUtil.ApplicationRootPath.Replace("\\", "/");
        public static String PlugInDataPath = AppPath + "GameData/PersistentTrails/PluginData/";
        public static String TrackPath = PlugInDataPath + "Tracks/";
        public static String CraftPath = PlugInDataPath + "Craft/";
        public static int craftFileFormat = 1;
        public static int trackFileFormat = 1;
        //public static Vector2 DebugScrollPosition = Vector2.zero;

        //creates a unique track name
        // renaming = true: renaming existing track (ignore the first occurrence of that name)
        public static string makeUniqueTrackName(string name, ref List<Track> trackList, bool renaming)
        {
            int uniqueIndex = 1;
            string uniqueSuffix = "";
            //while (File.Exists(Utilities.TrackPath + name + "_" + uniqueIndex + ".trk"))
            //{
            //    ++uniqueIndex;
            //}
            while (!nameIsUnique(name + uniqueSuffix, ref trackList, renaming))
            {
                uniqueIndex++;
                uniqueSuffix = "_" + uniqueIndex;
            }
            return name + uniqueSuffix;
        }

        private static bool nameIsUnique(string name, ref List<Track> trackList, bool ignoreFirstOccurrence)
        {
            foreach (Track t in trackList)
            {

                if (t.TrackName == name)
                {
                    if (ignoreFirstOccurrence)
                        ignoreFirstOccurrence = false;
                    else
                        return false;
                }
            }//end check if unique

            return true;
        }

        public static void LoadTexture(ref Texture2D tex, string fileName)
        {
            if (File.Exists(PlugInDataPath + fileName))
            {
                Debug.Log(String.Format("Loading Texture - file://{0}{1}", PlugInDataPath, fileName));
                WWW img1 = new WWW(String.Format("file://{0}{1}", PlugInDataPath, fileName));
                //Debug.Log("image loaded");
                img1.LoadImageIntoTexture(tex);
                //Debug.Log("image in texture");
            }
            else {
                Debug.LogWarning("File does not exist: " + PlugInDataPath + fileName);
            }
        }

        public static Texture2D LoadImage(string filename, int width, int height)
        {
            if (File.Exists(PlugInDataPath + filename))
            {
                var bytes = File.ReadAllBytes(PlugInDataPath + filename);
                //Debug.Log("image bytes read");
                Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
                //Debug.Log("texture created");
                texture.LoadImage(bytes);
                //Debug.Log("image loaded to texture");
                return texture;
            }
            return null;
        }

        public static void SetTextureColor(Texture2D texture, int width, int height, Color color)
        {
            int i, j;
            for (i = 0; i < width; i++)
            {
                for (j = 0; j < height; j++)
                {
                    texture.SetPixel(i, j, color);
                }
            }
            texture.Apply();
        }

        public static CelestialBody CelestialBodyFromName(string searchName)
        {
            foreach (CelestialBody body in FlightGlobals.Bodies) {
                if (body.name == searchName)
                    return body;
            }
            Debug.Log("Invalid CelestialBodyName " + searchName + ", defaulting to Kerbin");
            return FlightGlobals.Bodies[0];
            //return FlightGlobals.Bodies.FirstOrDefault(a => a.bodyName == BodyName);
        }


        public static Color makeColor(string colorString)
        {
            string[] split = colorString.Split(';');

            double r, g, b, a;
            r = Double.Parse(split[0]);
            g = Double.Parse(split[1]);
            b = Double.Parse(split[2]);
            a = Double.Parse(split[3]);

            return new Color((float)r, (float)g, (float)b, (float)a);
        }



        public static Color ColorFromHSV(float h, float s, float v, float a = 1)
        {
            // no saturation, we can return the value across the board (grayscale)
            if (s == 0)
                return new Color(v, v, v, a);

            // which chunk of the rainbow are we in?
            float sector = h / 60;

            // split across the decimal (ie 3.87 into 3 and 0.87)
            int i = (int)sector;
            float f = sector - i;

            float p = v * (1 - s);
            float q = v * (1 - s * f);
            float t = v * (1 - s * (1 - f));

            // build our rgb color
            Color color = new Color(0, 0, 0, a);

            switch (i)
            {
                case 0:
                    color.r = v;
                    color.g = t;
                    color.b = p;
                    break;

                case 1:
                    color.r = q;
                    color.g = v;
                    color.b = p;
                    break;

                case 2:
                    color.r = p;
                    color.g = v;
                    color.b = t;
                    break;

                case 3:
                    color.r = p;
                    color.g = q;
                    color.b = v;
                    break;

                case 4:
                    color.r = t;
                    color.g = p;
                    color.b = v;
                    break;

                default:
                    color.r = v;
                    color.g = p;
                    color.b = q;
                    break;
            }

            return color;
        }

        public static void ColorToHSV(Color color, out float h, out float s, out float v)
        {
            float min = Mathf.Min(Mathf.Min(color.r, color.g), color.b);
            float max = Mathf.Max(Mathf.Max(color.r, color.g), color.b);
            float delta = max - min;

            // value is our max color
            v = max;

            // saturation is percent of max
            if (!Mathf.Approximately(max, 0))
                s = delta / max;
            else
            {
                // all colors are zero, no saturation and hue is undefined
                s = 0;
                h = -1;
                return;
            }

            // grayscale image if min and max are the same
            if (Mathf.Approximately(min, max))
            {
                v = max;
                s = 0;
                h = -1;
                return;
            }

            // hue depends which color is max (this creates a rainbow effect)
            if (color.r == max)
                h = (color.g - color.b) / delta;            // between yellow & magenta
            else if (color.g == max)
                h = 2 + (color.b - color.r) / delta;                // between cyan & yellow
            else
                h = 4 + (color.r - color.g) / delta;                // between magenta & cyan

            // turn hue into 0-360 degrees
            h *= 60;
            if (h < 0)
                h += 360;
        }

        public static string distanceString(float distanceMeters)
        {
            if (distanceMeters > 1000)
                return "" + Math.Round(distanceMeters / 1000) + " km";
            else
                return "" + Math.Round(distanceMeters) + " m";

        }

        public static Quaternion parseQuaternion(string inString)
        {
            string[] floatStrings = inString.Split(';');
            Quaternion result = new Quaternion();
            if (floatStrings.Length == 4)
            {
                float.TryParse(floatStrings[0], out result.x);
                float.TryParse(floatStrings[1], out result.y);
                float.TryParse(floatStrings[2], out result.z);
                float.TryParse(floatStrings[3], out result.w);
            }
            else
            {
                Debug.Log("Couldn't parse " + inString + " to Quaternion");
            }
            return result;
        }

        public static Vector3 parseVector3(string inString)
        {            
            string[] floatStrings = inString.Split(';');            
            Vector3 result = Vector3.zero;
            if (floatStrings.Length == 3)
            {                
                float.TryParse(floatStrings[0], out result.x);
                float.TryParse(floatStrings[1], out result.y);
                float.TryParse(floatStrings[2], out result.z);
            }
            else
            {
                Debug.Log("Couldn't parse " + inString + " to Vector3");
            }            
            return result;
        }

        public static string Vector3ToString(Vector3 inValue)
        {
            return (String.Concat(inValue.x.ToString(), ";", inValue.y.ToString(), ";", inValue.z.ToString()));
        }

        public static string QuaternionToString(Quaternion inValue)
        {
            return (String.Concat(inValue.x.ToString(), ";", inValue.y.ToString(), ";", inValue.z.ToString(), ";", inValue.w.ToString()));
        }

    }

    public static class GUIResources
    {
        public static string VERSION = "v1.0";
        public static Texture2D IconRecording = new Texture2D(32, 32, TextureFormat.ARGB32, false);
        public static Texture2D IconNoRecording = new Texture2D(32, 32, TextureFormat.ARGB32, false);

        public static GUIStyle WindowStyle;
        public static GUIStyle IconStyle;
        public static GUIStyle styleTooltipStyle;
        public static GUIStyle ButtonToggledStyle;
        public static GUIStyle ButtonToggledRedStyle;
        public static GUIStyle ButtonStyle;
        public static GUIStyle LabelStyle;
        public static GUIStyle ScrollareaStyle;
        public const string ChrTimes = "\u00d7";

        private const int WinButtonSize = 25;

        private static bool intialized = false;
        public static Texture2D txtTooltipBackground = new Texture2D(9, 9);

        //public static GUIStyle DeleteButtonStyle;
        //public static GUIStyle DisabledButtonStyle;
        //public static GUIStyle LinkButtonStyle;
        //public static GUIStyle UnpaddedButtonStyle;
        //public static GUIStyle WindowButtonStyle;


        public static Rect mainButtonPosition = new Rect(300, 5, 32, 32);

        public static void SetupGUI()
        {
            //Debug.Log("Resources.SetupGUI()");
            GUI.skin = HighLogic.Skin;

            SetStyles();
        }

        public static void LoadAssets()
        {
            Utilities.LoadTexture(ref IconNoRecording, "Main-NoRecording.png");
            Utilities.LoadTexture(ref IconRecording, "Main-Recording.png");
        }

        public static void SetStyles()
        {
            if (intialized)
            {
                return;
            }
            intialized = true;

            Debug.Log("Resources.SetStyles()");
            WindowStyle = new GUIStyle(GUI.skin.window);
            IconStyle = new GUIStyle();

            ButtonToggledStyle = new GUIStyle(GUI.skin.button);
            ButtonToggledStyle.normal.textColor = Color.green;
            ButtonToggledStyle.normal.background = ButtonToggledStyle.onActive.background;

            ButtonToggledRedStyle = new GUIStyle(ButtonToggledStyle);
            ButtonToggledRedStyle.normal.textColor = Color.red;

            ButtonStyle = new GUIStyle(GUI.skin.button);
            ButtonStyle.normal.textColor = Color.white;


            LabelStyle = new GUIStyle(GUI.skin.label);

            ScrollareaStyle = new GUIStyle(GUI.skin.scrollView);
            //TODO add border frame



            var activeTxt = MakeConstantTexture(new Color(1f, 1f, 1f, 0.3f));
            var litTxt = MakeConstantTexture(new Color(1f, 1f, 1f, 0.2f));
            var normalTxt = MakeConstantTexture(new Color(1f, 1f, 1f, 0.1f));
            var clearTxt = MakeConstantTexture(Color.clear);

            styleTooltipStyle = new GUIStyle(LabelStyle);
            styleTooltipStyle.fontSize = 12;
            styleTooltipStyle.normal.textColor = new Color32(207, 207, 207, 255);
            styleTooltipStyle.stretchHeight = true;
            styleTooltipStyle.wordWrap = true;
            styleTooltipStyle.normal.background = MakeConstantTexture(Color.black);//txtTooltipBackground;
            //Extra border to prevent bleed of color - actual border is only 1 pixel wide
            styleTooltipStyle.border = new RectOffset(3, 3, 3, 3);
            styleTooltipStyle.padding = new RectOffset(4, 4, 6, 4);
            styleTooltipStyle.alignment = TextAnchor.MiddleCenter;
        }

        private static Texture2D MakeConstantTexture(Color fill)
        {
            const int size = 32;
            Texture2D txt = new Texture2D(size, size);
            for (int row = 0; row < size; row++)
            {
                for (int col = 0; col < size; col++)
                {
                    txt.SetPixel(col, row, fill);
                }
            }
            txt.Apply();
            txt.Compress(false);
            return txt;
        }
    


        public static Rect EnsureVisible(Rect pos, float min = 16.0f)
        {
            float xMin = min - pos.width;
            float xMax = Screen.width - min;
            float yMin = min - pos.height;
            float yMax = Screen.height - min;

            pos.x = Mathf.Clamp(pos.x, xMin, xMax);
            pos.y = Mathf.Clamp(pos.y, yMin, yMax);

            return pos;
        }

        public static Rect EnsureCompletelyVisible(Rect pos)
        {
            float xMin = 0;
            float xMax = Screen.width - pos.width;
            float yMin = 0;
            float yMax = Screen.height - pos.height;

            pos.x = Mathf.Clamp(pos.x, xMin, xMax);
            pos.y = Mathf.Clamp(pos.y, yMin, yMax);

            return pos;
        }

        public static Rect ClampToScreenEdge(Rect pos)
        {
            float topSeparation = Math.Abs(pos.y);
            float bottomSeparation = Math.Abs(Screen.height - pos.y - pos.height);
            float leftSeparation = Math.Abs(pos.x);
            float rightSeparation = Math.Abs(Screen.width - pos.x - pos.width);

            if (topSeparation <= bottomSeparation && topSeparation <= leftSeparation && topSeparation <= rightSeparation)
            {
                pos.y = 0;
            }
            else if (leftSeparation <= topSeparation && leftSeparation <= bottomSeparation && leftSeparation <= rightSeparation)
            {
                pos.x = 0;
            }
            else if (bottomSeparation <= topSeparation && bottomSeparation <= leftSeparation && bottomSeparation <= rightSeparation)
            {
                pos.y = Screen.height - pos.height;
            }
            else if (rightSeparation <= topSeparation && rightSeparation <= bottomSeparation && rightSeparation <= leftSeparation)
            {
                pos.x = Screen.width - pos.width;
            }

            return pos;
        }

        public static Texture2D LoadImage(string filename)
        {
            if (File.Exists(filename))
            {
                var bytes = File.ReadAllBytes(filename);
                Texture2D texture = new Texture2D(16, 16, TextureFormat.ARGB32, false);
                texture.LoadImage(bytes);
                return texture;
            }
            else
            {
                return null;
            }
        }

        public static bool GetValue(ConfigNode config, string name, bool currentValue)
        {
            bool newValue;
            if (config.HasValue(name) && bool.TryParse(config.GetValue(name), out newValue))
            {
                return newValue;
            }
            else
            {
                return currentValue;
            }
        }

        public static float GetValue(ConfigNode config, string name, float currentValue)
        {
            float newValue;
            if (config.HasValue(name) && float.TryParse(config.GetValue(name), out newValue))
            {
                return newValue;
            }
            else
            {
                return currentValue;
            }
        }

        public static double GetValue(ConfigNode config, string name, double currentValue)
        {
            double newValue;
            if (config.HasValue(name) && double.TryParse(config.GetValue(name), out newValue))
            {
                return newValue;
            }
            else
            {
                return currentValue;
            }
        }

        public static double ShowTextField(double currentValue, int maxLength, GUIStyle style, params GUILayoutOption[] options)
        {
            double newDouble;
            string result = GUILayout.TextField(currentValue.ToString(), maxLength, style, options);
            if (double.TryParse(result, out newDouble))
            {
                return newDouble;
            }
            else
            {
                return currentValue;
            }
        }

        // Gets connected resources to a part. Note fuel lines are NOT reversible! Add flow going TO the constructing part!
        // Safe to pass any string for name - if specified resource does not exist, a Partresource with amount 0 will be returned
        public static List<PartResource> GetConnectedResources(Part part, String resourceName)
        {
            var resources = new List<PartResource>();
            // Only check for connected resources if a Resource Definition for that resource exists
            if (PartResourceLibrary.Instance.resourceDefinitions.Contains(resourceName) == true)
            {
                PartResourceDefinition res = PartResourceLibrary.Instance.GetDefinition(resourceName);
                part.GetConnectedResources(res.id, resources);
            }
            // Do not return an empty list - if none of the resource found, create resource item and set amount to 0
            if (resources.Count < 1)
            {
                PartResource p = new PartResource();
                p.resourceName = resourceName;
                p.amount = (double)0;
                resources.Add(p);
            }
            return resources;
        }

    }
}
