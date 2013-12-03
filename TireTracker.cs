using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PersistentTrails
{
    class ModuleTireTracker : PartModule
    {
        [KSPField(isPersistant = false, guiActive = true, guiName = "Tire Width")]
        public float tireWidth;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Tire Radius")]
        public float tireRadius;

        //[KSPField(isPersistant = true)]
        //public string textureName;
    }

    class TireRecorder
    {
        private TireRecorder()
        {
            Debug.Log("TireRecorder constructor");
            //tires = new Dictionary<ModuleTireTracker, GameObject>();
        }
        public static readonly TireRecorder Instance = new TireRecorder();

        private TireTrail currentTrack;


        //private Dictionary<ModuleTireTracker, GameObject> tires;
        //PartModule trackingModule;

        public void update()
        {
            //Debug.Log("TireRecorder Update");

            if (FlightGlobals.ActiveVessel == null)
                return;

            if (currentTrack == null)
            {
                currentTrack = new TireTrail(FlightGlobals.ActiveVessel.transform);
                Debug.Log("created new track");
            }

            currentTrack.updateMesh(FlightGlobals.ActiveVessel.transform.position);
            ////find all TireTracker modules on current vessel
            //foreach (Part p in FlightGlobals.ActiveVessel.Parts)
            //{
            //    //Debug.Log("part in activeVessel: " + p.name);

            //    foreach (PartModule module in p.Modules.OfType<ModuleTireTracker>())
            //    {
            //        ModuleTireTracker tireModule = module as ModuleTireTracker;

            //        updateForTire(tireModule);
            //    }

            //}
        }

    }
}
