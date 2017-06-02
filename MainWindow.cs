using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;



namespace PersistentTrails
{
    class MainWindow : Window<MainWindow>
    {
        //private TrackManager trackManager;
        ExplorerTrackBehaviour mainBehaviour;

        //private ReplayWindow currentReplayWindow;

        private Vector2 trackListScroll;

        Texture2D continueTex;
        Texture2D deleteTex    ;
        Texture2D visibleTex   ;
        Texture2D invisibleTex ;
        Texture2D editTex;
        Texture2D playbackTex;

        List<Texture2D> trackColorTextures;
        
        //private WindowResizer resizer = new WindowResizer(
        //            new Rect(50, 255, 380, 240),
        //            new Vector2(380, 240));
        //private Vector2 scroll = new Vector2();
        //private KeyBind captureTarget;

        public MainWindow(ExplorerTrackBehaviour mainBehaviour) : base("Persistent Trails " + GUIResources.VERSION) {
            this.mainBehaviour = mainBehaviour;    
            SetResizeX(true);
            SetResizeY(true);

            windowPos = new Rect(60, 60, 380, 300);

            continueTex = Utilities.LoadImage("continue_icon.png", 16, 16);
            deleteTex = Utilities.LoadImage("delete_icon.png", 16, 16);
            visibleTex = Utilities.LoadImage("visible_on_icon.png", 16, 16);
            invisibleTex = Utilities.LoadImage("visible_off_icon.png", 16, 16);
            editTex = Utilities.LoadImage("edit_icon.png", 16, 16);
            playbackTex = Utilities.LoadImage("play.png", 16, 16);
            
            trackColorTextures = new List<Texture2D>();
        }



        //protected override void DrawGUI() {
        //    resizer.Position = GUILayout.Window(
        //        windowId, resizer.Position, DoGUI,
        //        "ExplorerTrack MainWindow",
        //        resizer.LayoutMinWidth(),
        //        resizer.LayoutMinHeight());

        //    //Debug.Log("MainWindow DrawGUI()");
            
        //}

        protected override void DrawWindowContents(int windowID)
        {
            GUIResources.SetupGUI();

            GUILayout.BeginVertical(); // BEGIN outer container
            GUILayout.BeginHorizontal();

            GUI.enabled = TrackManager.Instance.isRecordingAllowed();
            string recordText = "create new Track";
            if (!GUI.enabled)
                recordText    = "[ - NO SIGNAL! -]";

            if (GUILayout.Button(recordText)){
                TrackManager.Instance.startNewTrack();              
            }
            GUI.enabled = true;

            if (GUILayout.Button("Stop recording")){
                TrackManager.Instance.stopRecording();
            }

            GUILayout.EndHorizontal();
                
            if (GUILayout.Button("Add Log Entry to current Path")){
                if (TrackManager.Instance.IsRecording)
                {
                    ExplorerTrackBehaviour.Instance.logEntryWindow = new LogEntryWindow(TrackManager.Instance);
                    ExplorerTrackBehaviour.Instance.logEntryWindow.SetVisible(true);
                }
                else {
                    Debug.Log("Cannot add a Log Entry - no track is active/beeing recorded");
                }
            }

            
            createTrackList();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Tracks"))
            {
                TrackManager.Instance.saveTracks();
                Save(new ConfigNode(GetConfigNodeName()));
            }
            if (GUILayout.Button("Restore from Files"))
            {
                TrackManager.Instance.restoreTracksFromFile();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("High Precision", (mainBehaviour.RecordingInterval == 0.2f) ? GUIResources.ButtonToggledStyle : GUIResources.ButtonStyle))
            {
                mainBehaviour.RecordingInterval = 0.2f;
                TrackManager.Instance.ChangeThresholds = new RecordingThresholds(2, 2, 0.05f);
            }
            if (GUILayout.Button("Medium Precision", (mainBehaviour.RecordingInterval == 1) ? GUIResources.ButtonToggledStyle : GUIResources.ButtonStyle))
            {
                mainBehaviour.RecordingInterval = 1.0f;
                TrackManager.Instance.ChangeThresholds = new RecordingThresholds(10, 10, 0.1f);
            }
            if (GUILayout.Button("Low Precision", (mainBehaviour.RecordingInterval == 5) ? GUIResources.ButtonToggledStyle : GUIResources.ButtonStyle))
            {
                mainBehaviour.RecordingInterval = 5.0f;
                TrackManager.Instance.ChangeThresholds = new RecordingThresholds(20, 20, 0.25f);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            SetTooltipText();
            DrawToolTip();
        }

        public void updateColorTextures()
        {
            for (int i = 0; i < TrackManager.Instance.allTracks.Count; ++i)
            {
                Utilities.SetTextureColor(trackColorTextures[i], 20, 20, TrackManager.Instance.allTracks[i].LineColor);
            }
        }

        private void createTrackList() {
            //Debug.Log("creating track list object");
            //var noExpandWidth = GUILayout.ExpandWidth(false);

            //But the basic way of showing a tooltip would be, in effect, to just do GUILayout.Box(new Rect(mouseX,mouseY, someWidth, someHeight),GUI.tooltip) to have a hovertip.
            trackListScroll = GUILayout.BeginScrollView(trackListScroll, false, true);
            for (int i = 0; i < TrackManager.Instance.allTracks.Count; ++i)
            {
                Track track = TrackManager.Instance.allTracks.ElementAt(i);

                GUILayout.BeginHorizontal(/*noExpandWidth*/); // BEGIN path widgets
                //Name //ShowToggle Delete Continue
                //GUIStyle trackColorFontStyle = new GUIStyle();
                //trackColorFontStyle.normal.textColor = track.LineColor;

                GUILayout.Label(TrackManager.Instance.allTracks.ElementAt(i).TrackName, GUIResources.LabelStyle, GUILayout.ExpandWidth(true), GUILayout.Height(26));
                
                //track length is only calculated when we are on the same planet to prevent floating point issues
                string lengthString;
                if (FlightGlobals.currentMainBody == TrackManager.Instance.allTracks.ElementAt(i).ReferenceBody)
                    lengthString = Utilities.distanceString(TrackManager.Instance.allTracks.ElementAt(i).length());
                else
                    lengthString = "on " + TrackManager.Instance.allTracks.ElementAt(i).ReferenceBody.bodyName;

                GUILayout.Label(lengthString, GUIResources.LabelStyle, GUILayout.Width(75), GUILayout.Height(26));

                if (trackColorTextures.Count < i + 1)
                {
                    //create new texture
                    Texture2D colorTex = new Texture2D(20, 20);
                    Utilities.SetTextureColor(colorTex, 20, 20, track.LineColor);
                    trackColorTextures.Add(colorTex);
                }

                
                GUILayout.Label(trackColorTextures[i], GUILayout.Width(26), GUILayout.Height(26));

                Texture2D showIconTex;
                if (track.Visible)
                    showIconTex = visibleTex;
                else
                    showIconTex = invisibleTex;

                if (GUILayout.Button(new GUIContent(showIconTex, "toggle visibility"), GUIResources.ButtonStyle, GUILayout.Width(26), GUILayout.Height(26)))
                {
                    track.toggleVisibility();
                }


                if (GUILayout.Button(new GUIContent(deleteTex, "delete track"), GUIResources.ButtonStyle, GUILayout.Width(26), GUILayout.Height(26)))
                {
                    TrackManager.Instance.deleteTrack(ref track);
                    trackColorTextures.RemoveAt(i);
                    if (i >= TrackManager.Instance.allTracks.Count)
                    {
                        break;
                    }
                }

                GUI.enabled = TrackManager.Instance.isRecordingAllowed();            
                if (GUILayout.Button(new GUIContent(continueTex, "resume track with current vessel"), GUIResources.ButtonStyle, GUILayout.Width(26), GUILayout.Height(26)))
                {
                    TrackManager.Instance.continueTrack(track);
                }
                GUI.enabled = true;

                if (GUILayout.Button(new GUIContent(editTex, "edit track properties"), GUIResources.ButtonStyle, GUILayout.Width(26), GUILayout.Height(26)))
                {
                    if (ExplorerTrackBehaviour.Instance.currentlyOpenTrackEditWindow != null)
                        ExplorerTrackBehaviour.Instance.currentlyOpenTrackEditWindow.SetVisible(false);

                    ExplorerTrackBehaviour.Instance.currentlyOpenTrackEditWindow = new TrackEditWindow(track, this);
                    ExplorerTrackBehaviour.Instance.currentlyOpenTrackEditWindow.SetVisible(true);
                }

                if (GUILayout.Button(new GUIContent(playbackTex, "playback track"), GUIResources.ButtonStyle, GUILayout.Width(26), GUILayout.Height(26)))
                {

                    ExplorerTrackBehaviour.Instance.replaywindow = new ReplayWindow(track);
                    ExplorerTrackBehaviour.Instance.replaywindow.SetVisible(true);
                }
                
                GUILayout.EndHorizontal(); // END path widgets
            }
            GUILayout.EndScrollView();
            
        }


        //Tooltip variables
        //Store the tooltip text from throughout the code
        string strToolTipText = "";
        string strLastTooltipText = "";
        //is it displayed and where
        bool blnToolTipDisplayed = false;
        Rect rectToolTipPosition;
        int intTooltipVertOffset = 12;
        int intTooltipMaxWidth = 250;
        //timer so it only displays for a preriod of time
        float fltTooltipTime = 0f;
        float fltMaxToolTipTime = 5f;


        private void DrawToolTip()
        {
            if (strToolTipText != "" && (fltTooltipTime < fltMaxToolTipTime))
            {
                GUIContent contTooltip = new GUIContent(strToolTipText);
                if (!blnToolTipDisplayed || (strToolTipText != strLastTooltipText))
                {
                    //reset display time if text changed
                    fltTooltipTime = 0f;
                    //Calc the size of the Tooltip
                    rectToolTipPosition = new Rect(Event.current.mousePosition.x - 25, Event.current.mousePosition.y + intTooltipVertOffset, 0, 0);
                    float minwidth, maxwidth;
                    GUIResources.styleTooltipStyle.CalcMinMaxWidth(contTooltip, out minwidth, out maxwidth); // figure out how wide one line would be
                    rectToolTipPosition.width = Math.Min(intTooltipMaxWidth - GUIResources.styleTooltipStyle.padding.horizontal, maxwidth); //then work out the height with a max width
                    rectToolTipPosition.height = GUIResources.styleTooltipStyle.CalcHeight(contTooltip, rectToolTipPosition.width); // heers the result
                    //clamp tooltip to window frame
                    float rightEdge = rectToolTipPosition.x + rectToolTipPosition.width;
                    if (rightEdge > windowPos.width)
                        rectToolTipPosition.x -= rightEdge - windowPos.width;

                }
                //Draw the Tooltip
                GUI.Label(rectToolTipPosition, contTooltip, GUIResources.styleTooltipStyle);
                //On top of everything
                GUI.depth = 0;

                //update how long the tip has been on the screen and reset the flags
                fltTooltipTime += Time.deltaTime;
                blnToolTipDisplayed = true;
            }
            else
            {
                //clear the flags
                blnToolTipDisplayed = false;
            }
            if (strToolTipText != strLastTooltipText) fltTooltipTime = 0f;
            strLastTooltipText = strToolTipText;
        }

        public void SetTooltipText()
        {
            if (Event.current.type == EventType.Repaint)
            {
                strToolTipText = GUI.tooltip;
            }
        }
    }

    
}
