using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PersistentTrails
{
    class OffRailsObject : MonoBehaviour
    {
        public float maxDistanceFromPlayer = 3000f;
        public float timeOut = 600f;
        public bool destroyIfTimewarp = false;
        public float buoyancyForce = 50f;
        public float buoyancyRange = 2f;
        public bool destructionAllowed = true; //should be false when spawnign from replay window to avoid nullreffing. Closing the replay will kill the object
        /// <summary>
        /// Max speed to float up through the water
        /// </summary>
        public float maxVerticalSpeed = 5f;
        public float dragInWater = 1f;
        public float dragInAir = 0.01f;
        private bool destroyThis = false;
        private Vector3 lastMainBodyPosition = Vector3.zero;
        private Vector3 referenceFrameCorrection = Vector3.zero;
        private CelestialBody mainBody;
        private Vessel activeVessel;

        private void performDestruction()
        {
            Utilities.debug.debugMessage("Destroying ghost");
            Destroy(gameObject);
        }

        public void Start()
        {
            Utilities.debug.debugMessage("Added OffRailsObject module");
            if (FlightGlobals.ActiveVessel != null)
            {
                lastMainBodyPosition = FlightGlobals.ActiveVessel.mainBody.position;
                activeVessel = FlightGlobals.ActiveVessel;
                mainBody = activeVessel.mainBody;
            }
        
        }

        public void Update()
        {
            activeVessel = FlightGlobals.ActiveVessel;
            mainBody = activeVessel.mainBody;
            if (destroyThis)
                Utilities.debug.debugMessage("Unexpected OffrailsObject Update call from destroyed object");

            //Destruction checks
            if (destructionAllowed)
            {
                if (destroyIfTimewarp)
                {
                    if (TimeWarp.CurrentRate > 1f)
                        if (TimeWarp.WarpMode == TimeWarp.Modes.HIGH)
                        {
                            Utilities.debug.debugMessage("Ghost time warp destruction triggered");
                            destroyThis = true;
                        }
                }
                if (Vector3.Distance(activeVessel.transform.position, gameObject.transform.position) > maxDistanceFromPlayer)
                {
                    Utilities.debug.debugMessage("Ghost distance destruction triggered");
                    destroyThis = true;
                }
                timeOut -= Time.deltaTime;
                if (timeOut < 0f)
                {
                    Utilities.debug.debugMessage("Ghost timeout destruction triggered");
                    destroyThis = true;
                }

                if (destroyThis)
                {
                    performDestruction();
                    return;
                }
            }

            //Reference frame correction
            if (FlightGlobals.ActiveVessel != null)
            {
                referenceFrameCorrection = mainBody.position - lastMainBodyPosition;
                lastMainBodyPosition = mainBody.position;
                if (referenceFrameCorrection.magnitude > 1f)
                {
                    Utilities.debug.debugMessage("Reference Frame shift: " + referenceFrameCorrection);
                    gameObject.transform.position -= referenceFrameCorrection;      // + or -?              
                }
            }            
        }

        public void FixedUpdate()
        {
            //Ocean float code
            if (mainBody == null)
            {
                //Utilities.debug.debugMessage("No main body");
                return;
            }
            if (buoyancyForce > 0f && mainBody.ocean && rigidbody != null)
            {
                float seaAltitude = Vector3.Distance(mainBody.position, gameObject.transform.position) - (float)mainBody.Radius;
                if (seaAltitude < 0f)
                {
                    rigidbody.drag = dragInWater;
                    float floatMultiplier = Mathf.Max(0f, -Mathf.Max(seaAltitude, -buoyancyRange)) / buoyancyRange;
                    if (floatMultiplier > 0f)
                    {
                        Vector3 up = (gameObject.transform.position - mainBody.position).normalized;
                        Vector3 upLift = up * buoyancyForce * floatMultiplier;                        

                        float verticalSpeed = Vector3.Dot(gameObject.rigidbody.velocity, up) * gameObject.rigidbody.velocity.magnitude;

                        if (verticalSpeed < maxVerticalSpeed)
                        {
                            gameObject.rigidbody.AddForce(upLift * Time.deltaTime * 50f); // *50 compensates for the deltaTime reduction, so I can use familiar values
                        }
                    }
                }
                else
                {
                    rigidbody.drag = dragInAir;
                }
            }
        }
    }
}
