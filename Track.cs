using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

namespace PersistentTrails
{
    public class Waypoint
    {
        public double latitude;
        public double longitude;
        public double altitude;
        public double recordTime;
        public Quaternion orientation;
        public Vector3 velocity;

        public Waypoint(Vessel v)
        {        
            longitude = v.longitude;
            latitude = v.latitude;
            altitude = v.altitude;

            velocity = v.GetSrfVelocity();
            orientation = v.rigidbody.rotation;

            recordTime = Planetarium.GetUniversalTime();
        }

        public Waypoint(Waypoint other)//copy constructor
        {
            longitude = other.longitude;
            latitude = other.latitude;
            altitude = other.altitude;

            velocity = other.velocity;
            orientation = other.orientation;

            recordTime = other.recordTime;
        }

        public Waypoint(double latitude, double longitude, double altitude, Quaternion orientation, Vector3 velocity, double recordTime) {
            this.latitude = latitude;
            this.longitude = longitude;
            this.altitude = altitude;
            this.recordTime = recordTime;
            this.orientation = orientation;
            this.velocity = velocity;
        }
    };

    public class LogEntry : Waypoint {
        public String label;
        public String description;
        public GameObject gameObject;
        public GUIText guiLabel;
        public Vector3 unityPos;

        public LogEntry(String label, String description, Vessel v) : base(v)
        { 
            this.label = label;
            this.description = description;
            this.gameObject = null;
        }

        public LogEntry(double latitude, double longitude, double altitude, Quaternion orientation, Vector3 velocity, double recordTime, String label, String description)
            : base(latitude, longitude, altitude, orientation, velocity, recordTime)
        {
            this.label = label;
            this.description = description;
            this.gameObject = null;
        }
    };

    public class Track
    {
        private bool isVisible;

        private List<Waypoint> waypoints ;
        private List<LogEntry> logEntries;
        public Vessel SourceVessel{ get; set; }
        private CelestialBody referenceBody;
        public CelestialBody ReferenceBody { get { return referenceBody; } }
        //private bool modified;//for persitence saving
        
        Mesh directionMarkerMesh;
        List<GameObject> directionMarkers;

        LineRenderer lineRenderer;
        //LineRenderer mapLineRenderer;

        private GameObject drawnPathObj;
        //private GameObject mapModeObj;
        //private List<Vector3> renderCoords;
        //private Material lineMaterial;


        // -------- Properties --------
        public bool Modified { get; set;}

        public bool Visible { set { isVisible = value; setupRenderer(); } get { return isVisible && FlightGlobals.ActiveVessel.mainBody == this.referenceBody; } }

        //Setters for access from GUI
        public Color LineColor { get; set;  }
        public float LineWidth { get; set; }
        public float ConeRadiusToLineWidthFactor { get; set;  }
        public int NumDirectionMarkers { get; set; }
        public int SamplingFactor { get; set; }
        public String TrackName {get; set; }
        public String VesselName { get; set; }
        public String Description { get; set; }

        public enum EndActions { STOP, LOOP, OFFRAILS, DELETE }

        public EndActions EndAction { get; set; }
        public int LoopClosureTime { get; set; }
        
        public Track() 
        {
            initDefaultValues();

            TrackName = FlightGlobals.ActiveVessel.vesselName;
            VesselName = TrackName;
            //description = FlightGlobals.
            referenceBody = FlightGlobals.ActiveVessel.mainBody;
            SourceVessel = FlightGlobals.ActiveVessel;

            Visible = true;
            Modified = true;
        }

        ~Track() {
            //Debug.Log("Track " + Name + " Destructor");
            Visible = false;


       }

        private void initDefaultValues() {
            TrackName = "unnamed track";
            Description = "";
            waypoints  = new List<Waypoint>();
            logEntries  = new List<LogEntry>();
            directionMarkers = new List<GameObject>();

            SamplingFactor = 1;
            LineColor = Color.green;
            LineWidth = 0.2f;
            NumDirectionMarkers = 0;
            ConeRadiusToLineWidthFactor = 30;

            //this.renderCoords = new List<Vector3>();

            //init mesh and directionmarkes
            directionMarkerMesh = MeshFactory.createCone(1,2,12);

            EndAction = EndActions.STOP;
            LoopClosureTime = 0;

            for (int i = 0; i < 20; ++i)
            {
                GameObject marker = MeshFactory.makeMeshGameObject(ref directionMarkerMesh, "cone");
                marker.renderer.material = new Material(Shader.Find("KSP/Emissive/Diffuse"));
                marker.renderer.castShadows = false;
                marker.renderer.receiveShadows = false;
                marker.renderer.enabled = false;

                directionMarkers.Add(marker);
            }

        }

        private bool sufficientChange(Waypoint newNode, RecordingThresholds thresholds)
        {
            if (waypoints.Count == 0)
                return true; //always add first waypoint

            //SourceVessel.GetSrfVelocity().sqrMagnitude < 0.5f

            if (Quaternion.Angle(waypoints.Last().orientation, newNode.orientation) > thresholds.minOrientationAngleChange )
            {
                //Debug.Log("orientation fulfilled");
                return true;
            }
            if (Vector3.Angle(waypoints.Last().velocity.normalized, newNode.velocity.normalized) > thresholds.minVelocityAngleChange)
            {
                //accept velocity direction changes only if we are actually moving
                if (newNode.velocity.sqrMagnitude > 0.5f)
                {
                    //Debug.Log("velocity fulfilled");
                    return true;
                }
            }
            float relativeSpeedChange = waypoints.Last().velocity.magnitude / newNode.velocity.magnitude;

            if (Mathf.Abs(1 - relativeSpeedChange) > thresholds.minSpeedChangeFactor)
            {
                //Debug.Log("speed fulfilled");
                return true;
            }


            return false;
        }

        public void tryAddWaypoint(RecordingThresholds thresholds) //check thresholds and maybe add new node if sufficient change happened
        {
            //Debug.Log("Track.addWaypoint");
            //Debug.Log("TrackDump: + " + serialized());
            Waypoint newNode = new Waypoint(SourceVessel);

            //only record if vessel is moving
            if (!sufficientChange(newNode, thresholds))
                return;


            //Vector3 velocity = SourceVessel.rigidbody.velocity;
            //Quaternion orientation = SourceVessel.rigidbody.rotation;

            //Debug.Log("adding waypoint to list");
            waypoints.Add(newNode);
            Modified = true;
            
            //add new point to renderer
            if (Visible && waypoints.Count % SamplingFactor == 0){
                int index = waypoints.Count / SamplingFactor - 1;
                lineRenderer.SetVertexCount(index + 1);

                Vector3 currentPos = this.referenceBody.GetWorldSurfacePosition(SourceVessel.latitude, SourceVessel.longitude, SourceVessel.altitude);
                lineRenderer.SetPosition(index, currentPos);

                //mapLineRenderer.SetVertexCount(index + 1);
                //mapLineRenderer.SetPosition(index, ScaledSpace.LocalToScaledSpace(currentPos));
                //this.renderCoords.Add(currentPos);     
            }

            //Debug.Log("done");
        }
        
        public void addLogEntry(string label, string description)
        {
            LogEntry logEntry = new LogEntry(label, description, this.SourceVessel);
            logEntries.Add(logEntry);

            //add new point to renderer
            if (Visible)
            {
                setupLogEntryLabel(logEntry);
            }

            Modified = true;
        }


        public void setupRenderer() {
            if (Visible)
            {
                if (drawnPathObj == null) {
                    drawnPathObj = new GameObject("PersistentTrails Track");

                    lineRenderer = drawnPathObj.AddComponent<LineRenderer>();

                    lineRenderer.material = new Material(Shader.Find("KSP/Emissive/Diffuse"));
                    lineRenderer.renderer.castShadows = false;
                    lineRenderer.renderer.receiveShadows = false;

                    //Map mode
                    //mapModeObj = new GameObject();
                    //mapModeObj.layer = 10;
                    //mapModeObj.transform.parent = ScaledSpace.Instance.scaledSpaceTransforms.Single(t => t.name == this.referenceBody.name); 

                    //mapLineRenderer = mapModeObj.AddComponent<LineRenderer>();

                    //mapLineRenderer.material = new Material(Shader.Find("KSP/Emissive/Diffuse"));
                    //mapLineRenderer.renderer.castShadows = false;
                    //mapLineRenderer.renderer.receiveShadows = false;
                }
                
                //lineMaterial.SetColor("_EmissiveColor", LineColor);
                lineRenderer.material.color = LineColor;
                lineRenderer.material.SetColor("_EmissiveColor", LineColor);

                //lineRenderer.SetColors(LineColor, LineColor);
                lineRenderer.SetWidth(LineWidth, LineWidth);

                //mapLineRenderer.material.color = LineColor;
                //mapLineRenderer.material.SetColor("_EmissiveColor", LineColor);
                //mapLineRenderer.SetWidth(LineWidth / 6000, LineWidth / 6000);

                calculateLineVertices();


                foreach (LogEntry logEntry in logEntries)
                {
                    setupLogEntryLabel(logEntry);

                }

                updateDirectionMarkers();

                
            }
            else
            {
                //renderCoords.Clear();

                if (drawnPathObj != null)
                {
                    Debug.Log("Removing Renderer for Path " + TrackName);
                    lineRenderer.SetVertexCount(0);
                    lineRenderer.enabled = false;
                    GameObject.Destroy(drawnPathObj);
                    GameObject.Destroy(lineRenderer);
                    drawnPathObj = null;
                    lineRenderer = null;

                    //GameObject.Destroy(mapModeObj);
                    //GameObject.Destroy(mapLineRenderer);

                    foreach (LogEntry entry in logEntries) {
                        if (entry.gameObject != null)
                            GameObject.Destroy(entry.gameObject);
                    }

                    foreach (GameObject obj in directionMarkers)
                        obj.renderer.enabled = false;
                }
            }
        }

        public void toggleVisibility()
        {
            Visible = !Visible;
            Modified = true;
        }

        //public void drawGL() {
        //    if (!Visible)
        //        return;

        //    //Render Track
        //    // Set your materials

        //    GL.PushMatrix();
        //    // yourMaterial.SetPass( );
        //    lineMaterial.SetPass(0);
        //    //GL.Color(new Color(1, 0, 0));
        //    //TODO quads with thickness 
        //    GL.Begin(GL.LINES);
        //    foreach (Vector3 pos in this.renderCoords)
        //        GL.Vertex(pos);
        //    GL.End();

        //    GL.PopMatrix();
        //}

        private void calculateLineVertices()
        {
            int waypointIndex = -1;
            int currentRenderIndex = 0;
            lineRenderer.SetVertexCount((int)waypoints.Count / SamplingFactor);

            //mapLineRenderer.SetVertexCount((int)waypoints.Count / SamplingFactor);

            foreach (Waypoint waypoint in waypoints)
            {
                waypointIndex++;

                if (waypointIndex % SamplingFactor > 0)
                    continue;

                //Debug.Log("adding waypoint#" + waypoint + " to renderer at index = " + currentRenderIndex);
                //Transform LAT/LON/ALT to unity coordinates
                Vector3 unityPos = this.referenceBody.GetWorldSurfacePosition(waypoint.latitude, waypoint.longitude, waypoint.altitude);

                lineRenderer.SetPosition(currentRenderIndex, unityPos);

                //mapLineRenderer.SetPosition(currentRenderIndex, ScaledSpace.LocalToScaledSpace(unityPos));
                currentRenderIndex++;
                //this.renderCoords.Add(unityPos);

            }
        }



        private void setupLogEntryLabel(LogEntry logEntry) {
            if (logEntry.gameObject == null)
            {
                Debug.Log("setting up entry node sphere and label");

                logEntry.gameObject = new GameObject("logentry");
                logEntry.guiLabel = logEntry.gameObject.AddComponent<GUIText>();
                
                //onclick, onhover
                logEntry.guiLabel.fontSize = 20;
                logEntry.guiLabel.pixelOffset = new Vector2(-5, 10);
            }


            //logEntry.renderNode.renderer.material.SetColor("_EmissiveColor", LineColor);
            logEntry.guiLabel.material.color = LineColor;

            logEntry.unityPos = this.referenceBody.GetWorldSurfacePosition(logEntry.latitude, logEntry.longitude, logEntry.altitude);


            Vector2 screenPos = FlightCamera.fetch.cameras[0].WorldToViewportPoint(logEntry.gameObject.transform.position);
            //Vector3 orthoDir = Vector3.Cross(FlightGlobals.camera_position, entry.renderNode.transform.position).normalized;
            //Vector2 screenPosSphereEdge = FlightCamera.fetch.cameras[0].WorldToViewportPoint(
            //    entry.renderNode.transform.position + orthoDir * entry.renderNode.transform.localScale.x);

            //float offsetPixels = Math.Abs(screenPos.x - screenPosSphereEdge.x) * Screen.width * 1.2f;
            //Debug.Log("LogEntry node at " +logEntry.unityPos+ ", screenLabel at " + screenPos);

            logEntry.guiLabel.text = "[o]   " + logEntry.label;
        }

        public void updateDirectionMarkers() {

            //Debug.Log("updating directionMarkers");

            for (int i = 0; i < NumDirectionMarkers; ++i)
            {
                float relValue = (i + 1) * 1.0f / (NumDirectionMarkers + 1);
                Vector3 direction = new Vector3();
                double lat = 0;
                double lon = 0;

                //calc positions along the path in evenly spaced distances
                Vector3 position = evaluateAt(relValue, out direction, out lat, out lon);


                GameObject cone = directionMarkers[i];
                cone.renderer.enabled = true;
                cone.renderer.material.SetColor("_EmissiveColor", LineColor);

                cone.transform.position = position;
                float scale = LineWidth * this.ConeRadiusToLineWidthFactor;
                cone.transform.localScale = new Vector3(scale, scale, scale);
                //Orientation:
                // get surface normal/up vector
                Vector3 up = this.referenceBody.GetSurfaceNVector(lat, lon);
                //construct rotation so that the cones local Z-Axis matches the direction axis, and its X-Axis matches the up-vector
                Quaternion rotation = Quaternion.LookRotation(direction, up);
                cone.transform.rotation = rotation;

            }

            //Debug.Log("hiding otherr markers");

            //hide all other gameobjects
            for (int i = NumDirectionMarkers; i < 20; ++i)
            {

                GameObject cone = directionMarkers[i];
                cone.renderer.enabled = false;
            }
        }

        public void updateNodeLabelPositions() {
            if (Visible)
            {

                foreach (LogEntry entry in logEntries)
                {
                    
                    Vector3 screenPos = FlightCamera.fetch.cameras[0].WorldToViewportPoint(entry.unityPos);

                    //check if screenpos xy is valid and in front of the camera (z)
                    if (screenPos.z > 0
                         && screenPos.x >= 0 && screenPos.x <= 1
                         && screenPos.y >= 0 && screenPos.y <= 1)
                    {
                        entry.guiLabel.enabled = true;
                    }
                    else
                    {
                        entry.guiLabel.enabled = false;
                        continue;
                    }
                    //also hide gui label if out of viewport x-y

                    //Debug.Log("LogEntry Label at " + screenPos);
                    
                    entry.guiLabel.transform.position = screenPos;
                }
            }
        }

        public float length()
        {
            float totalLength = 0;
            for (int i = 0; i < waypoints.Count - 1; ++i)
            {
                //find out how much relPos is covered by this segment
                Vector3 start = referenceBody.GetWorldSurfacePosition(waypoints[i].latitude, waypoints[i].longitude, waypoints[i].altitude);
                Vector3 end = referenceBody.GetWorldSurfacePosition(waypoints[i + 1].latitude, waypoints[i + 1].longitude, waypoints[i + 1].altitude);
                totalLength += (end - start).magnitude;

            }

            return totalLength;
        }

        //Evaluate at relative position (0..1) of total length
        private Vector3 evaluateAt(float relValue, out Vector3 direction, out double lat, out double lon) {
            float length = this.length();

            float currentRelpos = 0;
            for (int i = 0; i < waypoints.Count; ++i)
            {
                //find out how much relPos is covered by this segment
                Vector3 start = referenceBody.GetWorldSurfacePosition(waypoints[i].latitude, waypoints[i].longitude, waypoints[i].altitude);
                Vector3 end = referenceBody.GetWorldSurfacePosition(waypoints[i+1].latitude, waypoints[i+1].longitude, waypoints[i+1].altitude);
                Vector3 segment = end - start;

                float relAmount = segment.magnitude / length;
                if (currentRelpos + relAmount > relValue)
                { 
                    //evaluate on the center of this segment THIS IS A SIMPLIFICATION
                    //float evalAt = 1.0;

                    direction = segment;

                    //lat = waypoints[i].latitude;
                    //lon = waypoints[i].longitude;
                    //double alt = waypoints[i].altitude;
                    //return start;

                    lat = (waypoints[i].latitude + waypoints[i + 1].latitude) / 2;
                    lon = (waypoints[i].longitude + waypoints[i + 1].longitude) / 2;
                    double alt = (waypoints[i].altitude + waypoints[i + 1].altitude) / 2;

                    return referenceBody.GetWorldSurfacePosition(lat, lon, alt);
                }
                currentRelpos += relAmount;
            }

            Debug.LogWarning("Track::evaluateAt(relValue = "+relValue+") failed!");
            direction = new Vector3();
            lat = 0;
            lon = 0;
            return new Vector3();

        }

        public void evaluateAtTime(double ut, out Vector3 position, out Quaternion orientation, out Vector3 velocity)
        {
            position = new Vector3();
            orientation = new Quaternion();
            velocity = new Vector3();

            if (waypoints.Count == 0)
                return;

            closeLoop(); //only does anything at all if EndAction == LOOP

            //Debug.Log("Track.evaluateAt ut=" + ut + ", track starts at " + waypoints.First().recordTime + " and ends at " + waypoints.Last().recordTime);

            
            if (ut <= waypoints.First().recordTime)
            {
                position = referenceBody.GetWorldSurfacePosition(waypoints.First().latitude, waypoints.First().longitude, waypoints.First().altitude);
                orientation = waypoints.First().orientation;
                velocity = waypoints.First().velocity;                
            }
            else if (ut >= waypoints.Last().recordTime)
            {
                position = referenceBody.GetWorldSurfacePosition(waypoints.Last().latitude, waypoints.Last().longitude, waypoints.Last().altitude);
                orientation = waypoints.Last().orientation;
                velocity = waypoints.Last().velocity;
            }
            else
            { //intermediate point

                for (int i = 0; i < waypoints.Count - 1; ++i)
                {

                    //find out how much relPos is covered by this segment
                    //Vector3 segment = end - start;

                    double timeThis = waypoints[i].recordTime;
                    double timeNext = waypoints[i + 1].recordTime;

                    if (timeNext > ut)
                    {
                        //evaluate on this segment
                        Vector3 start = referenceBody.GetWorldSurfacePosition(waypoints[i].latitude, waypoints[i].longitude, waypoints[i].altitude);
                        Vector3 end = referenceBody.GetWorldSurfacePosition(waypoints[i + 1].latitude, waypoints[i + 1].longitude, waypoints[i + 1].altitude);

                        double timeOnSegment = timeNext - timeThis;

                        //Debug.Log(string.Format("found segment at i={0}  from {1} to {2}, evaluating at relvalue {3}", i, start.ToString(), end.ToString(), (float)(ut - timeThis) / (float) timeOnSegment));
                        float progress = (float)(ut - timeThis) / (float)timeOnSegment;
                        position = Vector3.Lerp(start, end, progress);
                        orientation = Quaternion.Lerp(waypoints[i].orientation, waypoints[i + 1].orientation, progress); //Lerp is faster, maybe use the more accurate Slerp for better results?
                        velocity = Vector3.Lerp(waypoints[i].velocity, waypoints[i + 1].velocity, progress); //Lerp is faster, maybe use the more accurate Slerp for better results?
                        break;
                    }
                }
            }

            openLoop();
        }

        private void closeLoop()
        {
            if (EndAction == EndActions.LOOP)
            {
                double endTime = waypoints.Last().recordTime + LoopClosureTime;
                //Debug.Log("closing loop-adding first waypoint to end at time " + endTime);
                waypoints.Add(new Waypoint(waypoints.First()));
                waypoints.Last().recordTime = endTime;
            }
        }

        private void openLoop()
        {
            if (EndAction == EndActions.LOOP)
                waypoints.RemoveAt(waypoints.Count-1); //pop last element
        }


        public double GetStartTime()
        {
            if (waypoints.Count == 0)
                return 0;

            return waypoints.First().recordTime;
        }

        public double GetEndTime()
        {
            if (waypoints.Count == 0)
                return 0;

            return waypoints.Last().recordTime;
        }

        public String serialized()
        {
            string header = "VERSION:" + Utilities.trackFileFormat + "\n"
                + "[HEADER]\n"
                + "VESSELNAME:" + VesselName + "\n"
                + "DESCRIPTION:" + Description + "\n"
                + "VISIBLE:" + (isVisible ? "1" : "0") + "\n"
                + "MAINBODY:" + referenceBody.GetName() + "\n"
                + "SAMPLING:" + SamplingFactor + "\n"
                + "LINECOLOR:" + LineColor.r + ";" + LineColor.g + ";" + LineColor.b + ";" + LineColor.a + "\n"
                + "LINEWIDTH:" + LineWidth + "\n"
                + "CONERADIUSFACTOR:" + ConeRadiusToLineWidthFactor + "\n"
                + "NUMDIRECTIONMARKERS:" + NumDirectionMarkers + "\n"
                + "END:" + EndAction.ToString("F") + (EndAction == EndActions.LOOP ? ":" + LoopClosureTime.ToString() : "") + "\n"; //"F" makes creates a string literal

            
            string points= "[WAYPOINTS]\n";
            foreach (Waypoint waypoint in waypoints) {
                points +=
                      waypoint.recordTime + ";"
                    + waypoint.latitude + ";"
                    + waypoint.longitude + ";"
                    + waypoint.altitude + ";"
                    + waypoint.orientation.x + ";" + waypoint.orientation.y + ";" + waypoint.orientation.z + ";" + waypoint.orientation.w + ";"
                    + waypoint.velocity.x + ";" + waypoint.velocity.y + ";" + waypoint.velocity.z + "\n";
            }

            string logs = "[LOGENTRIES]\n";
            foreach (LogEntry entry in logEntries)
            {
                logs += entry.recordTime + ";"
                    + entry.latitude + ";"
                    + entry.longitude + ";"
                    + entry.altitude + ";"
                    + entry.orientation.x + ";" + entry.orientation.y + ";" + entry.orientation.z + ";" + entry.orientation.w + ";"
                    + entry.velocity.x + ";" + entry.velocity.y + ";" + entry.velocity.z + ";"
                    + entry.label + ";"
                    + entry.description + "\n";
            }

            return header + points + logs;
        
        }

        public Track(string filename)
        {
            initDefaultValues();

            StreamReader reader = new StreamReader(filename);

            try
            {
                string line = reader.ReadLine(); //VERSION:X for new format, [HEADER] for legacy
                //check if we need the legacy parser
                if (!line.StartsWith("VERSION"))
                {
                    parseLegacyTrack(reader);
                    return;
                }

                //Debug.Log("parsing Track");

                int fileVersion = int.Parse(line.Split(':')[1]);

                Debug.Log("parsing Track Version = " + fileVersion);

                line = reader.ReadLine(); //[HEADER]

                //Debug.Log("Filename=" + filename);
                Char separator = '/'; //Path.DirectorySeparatorChar doesnt give the correct one here...?
                //Debug.Log("DirectorySeparator=" + separator + "last index in filename = " + filename.LastIndexOf(separator));
                TrackName = filename.Substring(filename.LastIndexOf(separator) + 1); //cut path
                //Debug.Log("cut-down filename = " + TrackName);
                TrackName = TrackName.Substring(0, TrackName.Length - 4); //cut .trk
                //Debug.Log("final trackname = " + TrackName);
                
                Debug.Log(line);
                bool makeVisible = false;

                while (line != "[WAYPOINTS]" && !reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    //Debug.Log("HeaderLine:" + line);
                    String[] lineSplit = line.Split(':');

                    if (lineSplit[0].Equals("VESSELNAME")) {
                        VesselName = lineSplit[1];
                    } 
                    else if (lineSplit[0].Equals("DESCRIPTION")) { 
                        Description = lineSplit[1];
                    }  
                    else if (lineSplit[0].Equals("VISIBLE")) {
                        makeVisible = lineSplit[1].Equals("1");
                    }  
                    else if (lineSplit[0].Equals("MAINBODY")) { 
                        this.referenceBody = Utilities.CelestialBodyFromName(lineSplit[1]);
                    } 
                    else if (lineSplit[0].Equals("SAMPLING")) { 
                        this.SamplingFactor = int.Parse(lineSplit[1]);
                    }  
                    else if (lineSplit[0].Equals("LINECOLOR")) { 
                        LineColor = Utilities.makeColor(lineSplit[1]);
                    }
                    else if (lineSplit[0].Equals("LINEWIDTH"))
                    { 
                        LineWidth = float.Parse(lineSplit[1]);
                    }
                    else if (lineSplit[0].Equals("CONERADIUSFACTOR"))
                    {
                        ConeRadiusToLineWidthFactor = float.Parse(lineSplit[1]);
                    }
                    else if (lineSplit[0].Equals("NUMDIRECTIONMARKERS"))
                    {
                        NumDirectionMarkers = int.Parse(lineSplit[1]);
                    }
                    else if (lineSplit[0].Equals("END"))
                    {
                        //Debug.Log("Parsing END-TAG="+lineSplit[1]);
                        EndAction = (EndActions)Enum.Parse(typeof(EndActions), lineSplit[1]);
                        //Debug.Log("EndAction = " + EndAction.ToString("F"));

                        if (EndAction == EndActions.LOOP)
                        {
                            //Debug.Log("LOOP endaction: Parsing string for looptime=" + lineSplit[2]); 
                            LoopClosureTime = int.Parse(lineSplit[2]);
                        }
                    } 

                    
                } //End Header

                line = reader.ReadLine(); //first actual waypoint
                while (line != "[LOGENTRIES]" && !reader.EndOfStream)
                {
                    
                    //Debug.Log("Waypointline = " + line); 
                    string[] split = line.Split(';');
                    double lat, lon, alt, time;
                    float oriX, oriY, oriZ, oriW, vX, vY, vZ;
                    Double.TryParse(split[0], out time);
                    Double.TryParse(split[1], out lat);
                    Double.TryParse(split[2], out lon);
                    Double.TryParse(split[3], out alt);
                    float.TryParse(split[4], out oriX);
                    float.TryParse(split[5], out oriY);
                    float.TryParse(split[6], out oriZ);
                    float.TryParse(split[7], out oriW);
                    float.TryParse(split[8], out vX);
                    float.TryParse(split[9], out vY);
                    float.TryParse(split[10], out vZ);
                    
                    waypoints.Add(new Waypoint(lat, lon, alt, new Quaternion(oriX, oriY, oriZ, oriW), new Vector3(vX, vY, vZ), time));
                    line = reader.ReadLine();
                }

                Debug.Log("reading logentries");
                line = reader.ReadLine();//first entry
                while (!reader.EndOfStream)
                {
                    string trimmed = line;
                    trimmed = trimmed.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        //Debug.Log("reading logentryline = " + line);
                        string[] split = line.Split(';');
                        double lat, lon, alt, time;
                        Double.TryParse(split[0], out lat);
                        Double.TryParse(split[1], out lon);
                        Double.TryParse(split[2], out alt);
                        Double.TryParse(split[3], out time);
                        logEntries.Add(new LogEntry(lat, lon, alt, new Quaternion(), new Vector3(), time, split[4], split[5]));
                    }
                    line = reader.ReadLine();
                }

                Debug.Log("Created track from file containing " + waypoints.Count + "waypoints and " + logEntries.Count + " log entries");
                Visible = makeVisible; //set visible at end only

            }
            catch (Exception e) {
                Debug.Log(e.ToString());
            }
        }


        private void parseLegacyTrack(StreamReader reader)
        {
            Debug.Log("parsing legacy track");

            this.TrackName = reader.ReadLine();
            this.Description = reader.ReadLine();
            string visString = reader.ReadLine();

            string refBodyName = reader.ReadLine();
            //Debug.Log("reading celestialbody = " + refBodyName);
            this.referenceBody = Utilities.CelestialBodyFromName(refBodyName);
            //Debug.Log("reading + parsing samplingFactor");
            this.SamplingFactor = int.Parse(reader.ReadLine());
            //Debug.Log("samplingString = " + samplingString + ", parsed to samplingFactor = " + samplingFactor);

            string colorString = reader.ReadLine();
            this.LineColor = Utilities.makeColor(colorString);
            LineWidth = float.Parse(reader.ReadLine());

            string numString = reader.ReadLine();
            this.ConeRadiusToLineWidthFactor = float.Parse(numString);
            numString = reader.ReadLine();
            this.NumDirectionMarkers = int.Parse(numString);

            //Debug.Log("Header reading complete");



            //Debug.Log("read waypoints");
            reader.ReadLine();//WAYPOINTS
            string line = reader.ReadLine(); //first waypoint
            while (line != "[LOGENTRIES]" && !reader.EndOfStream)
            {
                //Debug.Log("reading waypointline = " + line); 
                string[] split = line.Split(';');
                double lat, lon, alt, time;
                Double.TryParse(split[0], out lat);
                Double.TryParse(split[1], out lon);
                Double.TryParse(split[2], out alt);
                Double.TryParse(split[3], out time);
                waypoints.Add(new Waypoint(lat, lon, alt, new Quaternion(), new Vector3(), time));
                line = reader.ReadLine();
            }


            //Debug.Log("read logentries");
            line = reader.ReadLine();//first entry
            while (!reader.EndOfStream)
            {
                string trimmed = line;
                trimmed = trimmed.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {

                    //Debug.Log("reading logentryline = " + line);
                    string[] split = line.Split(';');
                    double lat, lon, alt, time;
                    Double.TryParse(split[0], out lat);
                    Double.TryParse(split[1], out lon);
                    Double.TryParse(split[2], out alt);
                    Double.TryParse(split[3], out time);
                    logEntries.Add(new LogEntry(lat, lon, alt, new Quaternion(), new Vector3(), time, split[4], split[5]));
                }
                line = reader.ReadLine();
            }



            Debug.Log("Created track from file containing " + waypoints.Count + "waypoints and " + logEntries.Count + " log entries");
            Visible = (visString == "1");
            Modified = true; //legacy tracks are marked as modified for conversion
        }

    }
}
