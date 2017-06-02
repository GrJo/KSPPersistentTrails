using UnityEngine;

namespace PersistentTrails
{
    class LogEntryWindow : Window<LogEntryWindow>
    {
        private TrackManager trackManager;
        private string labelText;
        private string descriptionText;

        public LogEntryWindow(TrackManager trackManager) : base ("Create new Log Entry")
        {
            this.trackManager = trackManager;
            labelText = "Log Entry";
            descriptionText = "something happened here";
            windowPos = new Rect(150, 350, 300, 100);
        }

        protected override void DrawWindowContents(int windowID)
        {
            GUILayout.BeginVertical(); // BEGIN outer container

            GUILayout.BeginHorizontal();
            GUILayout.Label("Label:");
            labelText = GUILayout.TextField(labelText);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Description:");
            descriptionText = GUILayout.TextField(descriptionText);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("OK")) {
                trackManager.AddLogEntry(labelText, descriptionText);
                Save(new ConfigNode(GetConfigNodeName()));
                SetVisible(false);
            }

            if (GUILayout.Button("Cancel"))
                SetVisible(false);

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
    }
    
}
