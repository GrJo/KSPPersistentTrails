using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tac;
using UnityEngine;
using UnityEngineInternal;

namespace PersistentTrails
{
    class ReplayBehaviour : MonoBehaviour {

        private GameObject ghost;
        public double currentReplayTime;
        public Track track;

        public double trackStartUT;

        public double replayStartUT;
        
        public double totalReplayTime;

        public int playbackFactor;
        public double lastUpdateUT;

        public void OnAwake() {
            Debug.Log("ReplayBehaviour awake");
        }


        public void initialize(Track track, GameObject ghost) {
            this.track = track;
            this.ghost = ghost;
            trackStartUT = track.GetStartTime();
            totalReplayTime = track.GetEndTime() - track.GetStartTime();
            currentReplayTime = 0;


            replayStartUT = Planetarium.GetUniversalTime();
            lastUpdateUT = replayStartUT;

            setGhostToPlaybackAt(trackStartUT + currentReplayTime);
            Vector3 trackPos;
            Quaternion orientation;
            Vector3 velocity;
            track.evaluateAtTime(trackStartUT + currentReplayTime, out trackPos, out orientation, out velocity);
            ghost.transform.position = trackPos;
            ghost.transform.rotation = orientation;

            playbackFactor = 0;
            Debug.Log("initialized replayBehaviour, ghost at trackPos =" + trackPos.ToString());

        }

        public void Update() {
            //Debug.Log("Replay OnUpdate: evaluating track after t = " + currentReplayTime + "s, UT= " + (trackStartUT + currentReplayTime));
            double currentTimeUT = Planetarium.GetUniversalTime();

            //increment replayTime
            currentReplayTime += playbackFactor * (currentTimeUT - lastUpdateUT);

            setGhostToPlaybackAt(trackStartUT + currentReplayTime);

            lastUpdateUT = currentTimeUT;
        }

        private void setGhostToPlaybackAt(double time)
        {
            Vector3 trackPos;
            Quaternion orientation;
            Vector3 velocity;
            track.evaluateAtTime(trackStartUT + currentReplayTime, out trackPos, out orientation, out velocity);
            ghost.transform.position = trackPos;
            ghost.transform.rotation = orientation;
        }
    }


    class ReplayWindow : Tac.Window<ReplayWindow>
    {

        ReplayBehaviour behaviour;
        private GameObject ghost;

        Texture2D playTex;
        Texture2D pauseTex;
        Texture2D ffTex;
        Texture2D stopTex;

        public ReplayWindow(Track track) : base("Replay Track: " + track.Name) {
            Mesh sphere = MeshFactory.createSphere();
            bool loadCraft = true;
            if (loadCraft)
            {
                try
                {
                    ghost = CraftLoader.assembleCraft(Utilities.CraftPath + "beech.crf"); // --- add the craft file listed in the path, or selected from a menu ---
                }
                catch
                {
                    loadCraft = false;
                }
            }
            
            if (!loadCraft)
            {
                ghost = MeshFactory.makeMeshGameObject(ref sphere, "Track playback sphere");
                ghost.transform.localScale = new Vector3(track.ConeRadiusToLineWidthFactor * track.LineWidth, track.ConeRadiusToLineWidthFactor * track.LineWidth, track.ConeRadiusToLineWidthFactor * track.LineWidth);
                //ghost.collider.enabled = false;
                ghost.renderer.material = new Material(Shader.Find("KSP/Emissive/Diffuse"));
                ghost.renderer.material.SetColor("_EmissiveColor", track.LineColor);
            }

            behaviour = ghost.AddComponent<ReplayBehaviour>();
            behaviour.initialize(track, ghost);
            behaviour.enabled = true;
            
            this.windowPos = new Rect(600f, 50f, 300f, 100f);

            playTex = Utilities.LoadImage("play.png", 24, 24);
            pauseTex = Utilities.LoadImage("pause.png", 24, 24); ;
            ffTex = Utilities.LoadImage("ff2.png", 24, 24); ;
            stopTex = Utilities.LoadImage("stop.png", 24, 24);
        }

        public override void SetVisible(bool newValue) {            
            base.SetVisible(newValue);
            if (!newValue) {
                GameObject.Destroy(ghost);
            }
        }


        protected override void DrawWindowContents(int windowID)
        {
            GUIResources.SetupGUI();
            GUILayout.BeginVertical();
            behaviour.currentReplayTime = GUILayout.HorizontalSlider((float)behaviour.currentReplayTime, 0, (float)behaviour.totalReplayTime);
            
            GUILayout.BeginHorizontal(); // BEGIN outer container
            if (GUILayout.Button(playTex))
            {
                behaviour.playbackFactor = 1;
            }
            if (GUILayout.Button(ffTex))
            {
                behaviour.playbackFactor+=1;
            }
            if (GUILayout.Button(pauseTex))
            {
                behaviour.playbackFactor = 0;
            }
            if (GUILayout.Button(stopTex))
            {
                behaviour.playbackFactor = 0;
                behaviour.currentReplayTime = 0;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
    }
}
