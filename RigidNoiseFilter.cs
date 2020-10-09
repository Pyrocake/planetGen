using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RigidNoiseFilter : INoiseFilter{

    LegacyNoiseSettings.RigidNoiseSettings settings;
    Noise noise = new Noise();

    public RigidNoiseFilter(LegacyNoiseSettings.RigidNoiseSettings settings)
    {
        this.settings = settings;
    }

    public float Evaluate(Vector3 point)
    {
        float noiseValue = 0;
        float frequency = settings.baseRoughness;
        float amplititude = 1;
        float weight = 1;

        for (int i = 0; i < settings.numLayers; i++)
        {
            float v = 1-Mathf.Abs(noise.Evaluate(point * frequency + settings.center));
            v *= v;
            v *= weight;
            weight = Mathf.Clamp01(v * settings.weightMultiplier);
            noiseValue += v * amplititude;
            frequency *= settings.roughness;
            amplititude *= settings.persistance;
        }
        noiseValue -= settings.minValue;
        return noiseValue * settings.strength;
    }

}
