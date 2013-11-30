using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentTrails
{
    public struct RecordingThresholds //if one of the thresholds is exceeded, a new node will be added to the path
    {
        public RecordingThresholds(float minOrientationAngleChange, float minVelocityAngleChange, float minSpeedChangeFactor)
        {
            this.minOrientationAngleChange = minOrientationAngleChange;
            this.minVelocityAngleChange = minVelocityAngleChange;
            this.minSpeedChangeFactor = minSpeedChangeFactor;
        }

        public float minOrientationAngleChange; //angle in degrees!
        public float minVelocityAngleChange; //angle in degrees!
        public float minSpeedChangeFactor; // percentage (0.2 for 20% change)
    }
}
