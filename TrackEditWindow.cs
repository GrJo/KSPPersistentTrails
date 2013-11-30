using System;
using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Tac;
using System.IO;

namespace PersistentTrails
{

    class TrackEditWindow : Tac.Window<TrackEditWindow>
    {
        //private WindowResizer resizer = new WindowResizer(
        //    new Rect(350, 255, 380, 240),
        //    new Vector2(380, 240));

        private Track track;
        private List<Track> trackList;

        //Data entry fields
        private string newName;
        private string newDescription;
        private UnityEngine.Color newColor;
        float sampling;
        float lineWidth;
        float markerRadiusFactor;
        float numMarkers;
        private int selectedActionIndex;
        private float loopTime;
        Texture2D colorTex;
        MainWindow mainWindow;

        public TrackEditWindow(Track track, MainWindow mainWindow) : base ("Track detail editor") {
            this.mainWindow = mainWindow;
            this.track = track;
            this.trackList = TrackManager.Instance.allTracks;
            newName = track.TrackName;
            newDescription = track.Description;
            newColor = track.LineColor;
            sampling = track.SamplingFactor;
            lineWidth = track.LineWidth;
            markerRadiusFactor = track.ConeRadiusToLineWidthFactor;
            numMarkers = track.NumDirectionMarkers;
            loopTime = track.LoopClosureTime;
            selectedActionIndex = (int) track.EndAction;
            SetResizeX(true);
            SetResizeY(true);

            colorTex = new Texture2D(32, 32);

            this.windowPos = new Rect(500f, 40f, 380f, 200f);
        }

        //protected override void DrawGUI() {
        //resizer.Position = GUILayout.Window(
        //    windowId, resizer.Position, DoGUI,
        //    "Track Detail Editor",
        //    resizer.LayoutMinWidth(),
        //    resizer.LayoutMinHeight());

        //Debug.Log("MainWindow DrawGUI()");
            
        //}


        public void OnColorPicked(Color newColor)
        {
            this.newColor = newColor;
        }

        private float sliderPosToLineWidth(float sliderPos) {
            int pos = (int)sliderPos;

            switch (pos) {
                case 1: return 0.1f;
                case 2: return 0.5f;
                case 3: return 2;
                case 4: return 5;
                case 5: return 10;
                case 6: return 20;
                case 7: return 50;
                case 8: return 100;
                case 9: return 200;
                case 10: return 500;
                case 11: return 1000;
                case 12: return 5000;
                default: return 1;
            }
        }
        private float lineWidthToSliderPos(float lineWidth)
        {
            if (lineWidth < 0.5f)
                return 1;

            if (lineWidth < 2)
                return 2;

            if (lineWidth < 5)
                return 3;

            if (lineWidth < 10)
                return 4;

            if (lineWidth < 20)
                return 5;

            if (lineWidth < 50)
                return 6;
            if (lineWidth < 100)
                return 7;
            if (lineWidth < 200)
                return 8;

            if (lineWidth < 500)
                return 9;

            if (lineWidth < 1000)
                return 10;

            if (lineWidth < 5000)
                return 11;

            return 12;
        }
        protected override void DrawWindowContents(int windowID)
        {
            GUILayout.BeginVertical(); // BEGIN outer container

            GUILayout.BeginHorizontal();
            GUILayout.Label("Track Name:");
            newName = GUILayout.TextField(newName);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Description:");
            newDescription = GUILayout.TextField(newDescription);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Color");

            GUILayout.BeginVertical();
            float h, s, v;
            Utilities.ColorToHSV(newColor, out h, out s, out v);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Hue");
            h = GUILayout.HorizontalSlider(h, 0, 360);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Sat.");
            s = GUILayout.HorizontalSlider(s, 0, 1);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Val.");
            v = GUILayout.HorizontalSlider(v, 0, 1);
            newColor = Utilities.ColorFromHSV(h, s, v);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            Utilities.SetTextureColor(colorTex, 32, 32, this.newColor);
            //this.colorStyle = new GUIStyle();
            //colorStyle.normal.textColor = this.newColor;
            //GUIStyle generic_style = new GUIStyle();
            //GUI.skin.box = generic_style;
            //GUI.Box(new Rect(x, y, w, h), rgb_texture);

            if (GUILayout.Button(colorTex))
            {
                // Show the color dialog.
                ColorPicker colorDlg = new ColorPicker(this);
                colorDlg.SetVisible(true);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Draw Sampling:");
            sampling = GUILayout.HorizontalSlider(sampling, 1, 10);
            GUILayout.Label("" + (int)sampling +"x");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Line Width (m):");
            float sliderPos = lineWidthToSliderPos(lineWidth);
            sliderPos = GUILayout.HorizontalSlider(sliderPos, 1, 12);
            lineWidth = sliderPosToLineWidth(sliderPos);
            GUILayout.Label("" + lineWidth + "m");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Direction marker radius:");
            markerRadiusFactor = GUILayout.HorizontalSlider(markerRadiusFactor, 2, 50);
            GUILayout.Label("" + lineWidth * markerRadiusFactor + "m");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Number of direction markers:");
            numMarkers = GUILayout.HorizontalSlider(numMarkers, 0, 20);
            GUILayout.Label("" + (int)numMarkers);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Replay End Action:");
            selectedActionIndex = GUILayout.SelectionGrid(selectedActionIndex, new string[] { "Stop", "Loop", "OffRails", "Delete" }, 4);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Loop closure time:");
            loopTime = GUILayout.HorizontalSlider(loopTime, 0, 100);
            GUILayout.Label("" + (int)loopTime + "s");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("OK")) {
                
                string newUniqueName = Utilities.makeUniqueTrackName(newName, ref trackList, true);
                if (File.Exists(Utilities.TrackPath + track.TrackName + ".trk"))
                    File.Move(Utilities.TrackPath + track.TrackName + ".trk", Utilities.TrackPath + newUniqueName + ".trk");

                track.TrackName = newUniqueName;
                track.Description = newDescription;
                track.LineColor = newColor;
                track.SamplingFactor = (int) sampling;
                track.LineWidth = lineWidth;
                track.ConeRadiusToLineWidthFactor = markerRadiusFactor;
                track.NumDirectionMarkers = (int)numMarkers;
                track.LoopClosureTime = (int)loopTime;
                track.EndAction = (Track.EndActions)selectedActionIndex;
                
                track.Modified = true;

                track.setupRenderer();
                mainWindow.updateColorTextures();
                SetVisible(false);
                Save(new ConfigNode(GetConfigNodeName()));//Does nothing...
            }

            if (GUILayout.Button("Cancel"))
                SetVisible(false);

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
    }


}
