﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class BiomeBuilder : ScriptableObject {

    public Material material;
    public enum ViewMode {
        Default,
        Heat,
        Rain
    }
    public ViewMode viewMode;

    public BiomeColorSettings biomeColorSettings;
    public BiomeRules biomeRules;
    public OceanSettings oceanSettings;

    [System.Serializable]
    public class BiomeColorSettings {
        
        public bool tintToggle = false;
        public Biome[] biomes;
        //public Noise noise;
        public float noiseOffset;
        public float noiseStrength;
        [Range(0, 1)]
        public float blendAmount;
        public LegacyNoiseSettings legacyNoiseSettings;

        [System.Serializable]
        public class Biome {
            public Gradient gradient;
            public Color tint;
            [Range(0, 1)]
            public float startHeight;
            [Range(0, 1)]
            public float tintPercent;
        }
    }
    [System.Serializable]
    public class BiomeRules {
        [Header("Heat Variables")]
        public float longitudePenalty = 50f;

        [Header("Water Variables")]
        [Range(0,500)] public float rainShadow = 100f;
        public float idealDistance = 200;
    }

    [System.Serializable]
    public class OceanSettings {
        public Gradient oceanColor;
        [Range(0, 1)]
        public float seaLevel;
    }
}
