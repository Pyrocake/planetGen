using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LegacyNoiseSettings{

    public enum FilterType {Legacy, Rigid};
    public FilterType filterType;

    [ConditionalHide("filterType", 0)]
    public LegacySimpleNoiseSettings legacySimpleNoiseSettings;
    [ConditionalHide("filterType", 1)]
    public RigidNoiseSettings rigidNoiseSettings;

    [System.Serializable]
    public class LegacySimpleNoiseSettings {
        public float strength = 1;
        [Range(1, 10)]
        public int numLayers = 1;
        public float persistance = .5f;
        public float baseRoughness = .5f;
        public float roughness = 1;
        public Vector3 center;
        public float minValue;
    }
    
    [System.Serializable]
    public class RigidNoiseSettings : LegacySimpleNoiseSettings {
        public float weightMultiplier = .8f;
    }

}
