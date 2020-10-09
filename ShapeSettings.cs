﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class ShapeSettings : ScriptableObject {

    public NoiseLayer[] noiseLayers;

    [System.Serializable]
    public class NoiseLayer {
        public bool enabled = true;
        public bool useFirstLayerAsMask;
        public LegacyNoiseSettings noiseSettings;

    }

}
