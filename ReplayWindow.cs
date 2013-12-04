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

        public Rigidbody rbody;
        public Vector3 currentVelocity = Vector3.zero;
        private bool _isOffRails = false;
        private bool offRailsInitiliazed = false;
        private OffRailsObject offRailsObject;       

        public bool isOffRails
        {
            get { return _isOffRails; }
            set
            {
                if (value)                
                    goOffRails();                
                else                
                    goOnRails();                
            }
        }

        public void OnAwake() {
            Debug.Log("ReplayBehaviour awake");
        }


        public void initialize(Track track, GameObject ghost) {
            this.track = track;
            this.ghost = ghost;
            trackStartUT = track.GetStartTime();
            
            
            totalReplayTime = track.GetEndTime() - track.GetStartTime();
            if (track.EndAction == Track.EndActions.LOOP)
            {
                totalReplayTime += track.LoopClosureTime;
            }

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

        public void goOffRails()
        {
            _isOffRails = true;
            if (!offRailsInitiliazed) // run once on each offRails activation
            {
                if (rbody == null)
                    setupRigidBody();                
                if (rbody == null) return;
                rbody.isKinematic = false;
                rbody.velocity = currentVelocity;                
                offRailsInitiliazed = true;
                CraftLoader.setColliderStateInChildren(ghost, true);
                if (offRailsObject == null)
                    offRailsObject = ghost.AddComponent<OffRailsObject>();
                else
                    offRailsObject.enabled = true;
            }            
        }

        public void goOnRails()
        {
            _isOffRails = false;
            offRailsInitiliazed = false;
            Destroy(rbody);
            CraftLoader.setColliderStateInChildren(ghost, false);
            if (offRailsObject != null)
            {
                offRailsObject.enabled = false;
            }
        }

        public void setupRigidBody()
        {
            Debug.Log("Adding rigidbody to playback ghost");
            rbody = gameObject.AddComponent<Rigidbody>();
            rbody.mass = 3.0f;
            rbody.drag = 0.01f;
            rbody.useGravity = true;
            rbody.isKinematic = false;
        }

        public void Update() {
            //Debug.Log("Replay OnUpdate: evaluating track after t = " + currentReplayTime + "s, UT= " + (trackStartUT + currentReplayTime));
            double currentTimeUT = Planetarium.GetUniversalTime();

            //increment replayTime
            currentReplayTime += playbackFactor * (currentTimeUT - lastUpdateUT);

            if (!isOffRails)
                setGhostToPlaybackAt(trackStartUT + currentReplayTime);

            lastUpdateUT = currentTimeUT;

            //check end reached
            if (currentReplayTime >= totalReplayTime)
            {
                if (track.EndAction == Track.EndActions.LOOP)
                    currentReplayTime = 0;
                else if (track.EndAction == Track.EndActions.OFFRAILS)
                {
                    isOffRails = true;                    
                }
                // TODO: add DELETE ghost handling
            }
        }

        private void setGhostToPlaybackAt(double time)
        {
            Vector3 trackPos;
            Quaternion orientation;
            Vector3 velocity;
            track.evaluateAtTime(trackStartUT + currentReplayTime, out trackPos, out orientation, out velocity);
            ghost.transform.position = trackPos;
            ghost.transform.rotation = orientation;
            currentVelocity = velocity;

            //Debug.Log("Replay: Ghost at " + trackPos.ToString() + " with ori=" + orientation.ToString());
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

        public ReplayWindow(Track track) : base("Replay Track: " + track.TrackName) {
            
            bool loadCraft = true;
            if (loadCraft)
            {
                try
                {
                    ghost = CraftLoader.assembleCraft(Utilities.CraftPath + track.VesselName + ".crf", track.ReplayColliders); // --- add the craft file listed in the path, or selected from a menu ---
                }
                catch
                {
                    //Debug.Log("ERROR LOADING CRF, FALLING BACK TO SPHERE");
                    loadCraft = false;
                }
            }
            
            if (!loadCraft)
            {
                Mesh sphere = MeshFactory.createSphere();
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
                behaviour.isOffRails = false;
                behaviour.playbackFactor = 1;
            }
            if (GUILayout.Button(ffTex))
            {
                behaviour.isOffRails = false;
                behaviour.playbackFactor+=1;
            }
            if (GUILayout.Button(pauseTex))
            {
                behaviour.isOffRails = false;
                behaviour.playbackFactor = 0;
            }
            if (GUILayout.Button(stopTex))
            {
                behaviour.isOffRails = false;
                behaviour.playbackFactor = 0;
                behaviour.currentReplayTime = 0;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
    }
}
