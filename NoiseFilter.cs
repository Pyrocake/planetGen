using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A lot of this code comes from Sebastian Lague, credit goes to him
[CreateAssetMenu]
public class NoiseFilter : ScriptableObject {
    Noise noise = new Noise();

    public PlateSettings plateSettings;

    public float strength = 1;
    [Range(1, 8)] public int octaves = 1;
    public float baseRoughness = 1;
    public float roughness = 2;
    public float persistance = .5f;
    [Range(0f,.75f)] public float altitudeCrusher = .35f;
    [Range(2,16)] public int driftFactor = 4;
    [Header("WARNING: Higher values may compound")]
    [Range(0.01f, .2f)] public float driftOffset = .1f;
    public Vector3 center;

    public PlanetGen planetScript;
    public MainBuilder mainBuilder;

    [System.Serializable]
    public class PlateSettings {
        public float majorPlateCount = 10;
        public float minorPlateCount = 15;
        public float microPlateCount = 30;
    }



    /// <summary>
    /// Pseudo random noise generator, Mode 0 is rough, Mode 1 is rain, and Mode 2 is heat.
    /// </summary>
    /// <param name="point"></param>
    /// <param name="mode"></param>
    /// <returns>The noise value at point, cubed, times strength (Mode 0)</returns>
    public float Evaluate(Vector3 point, int mode) {
        float noiseValue = 0;
        float noiseDrift = 0;
        float frequency = baseRoughness;
        float amplitude = 1;
        float centerChange = driftOffset;
        Vector3 centerOff = new Vector3(center.x + centerChange, center.y + centerChange, center.z + centerChange);

        if (mode == 0) {

            for (int i = 0; i < octaves; i++) {
                float v = noise.Evaluate(point * frequency + center);
                float drift = noise.Evaluate(point * (frequency/ driftFactor) + centerOff);
                noiseValue += v * amplitude;
                noiseDrift += drift * amplitude;
                frequency *= roughness;
                amplitude *= persistance;

                centerChange += driftOffset;
                centerOff = new Vector3(center.x + centerChange, center.y + centerChange, center.z + centerChange);
            }
            noiseValue = Mathf.Abs(noiseValue);
            noiseDrift = Mathf.Abs(noiseDrift);
            float defaultElevation = 1 - noiseValue;
            float modifier = Mathf.Abs(altitudeCrusher * noiseDrift * defaultElevation);
            float elevation = Mathf.Max(noiseDrift, 0.1f) * (1 - (noiseValue + modifier)) - 0.0001f;

            return elevation * elevation * elevation * strength;
        } else if (mode == 1) {
            //Chunkier Noise
            Vector3 off = new Vector3(3, 3, 3);
            off += center;
            float ratio = 1 - Mathf.Abs(point.normalized.y);
            float blur = Mathf.Max(octaves/2 -1, 1);
            float plus;
            for (int i = 0; i < blur; i++) {
                float v = noise.Evaluate(point * (frequency*2) + center);
                plus = noise.Evaluate(point * (frequency) + off);
                noiseValue += v * amplitude;
                noiseDrift += plus * amplitude;
                frequency *= roughness;
                amplitude *= persistance;
            }
            float amount = Mathf.Clamp01(noiseValue);
            return ratio * (1 - (amount * amount * .75f)) * (1 - (noiseDrift * noiseDrift * .25f));
        } else {
            Vector3 off = new Vector3(8, 8, 8);
            off += center;
            float ratio = 1 - Mathf.Abs(point.normalized.y);
            float blur = Mathf.Max(octaves / 2 - 1, 1);
            float plus;
            for (int i = 0; i < blur; i++) {
                float v = noise.Evaluate(point * (frequency) + center);
                plus = noise.Evaluate(point * (frequency) + off);
                noiseValue += v * amplitude;
                noiseDrift += plus * amplitude;
                frequency *= roughness;
                amplitude *= persistance;
            }
            float amount = Mathf.Clamp01(noiseValue);
            amount = ratio * (1 - (amount * amount * .25f)) * (1 + (noiseDrift * noiseDrift * .5f)) + (.1f * ratio);
            return Mathf.Clamp01(amount);
        }
        
    }

    /// <summary>
    /// Variant rule where higher altitudes are colder
    /// </summary>
    /// <param name="point"></param>
    /// <param name="mode"></param>
    /// <param name="altitude"></param>
    /// <returns>The noise value at point, cubed, times strength (Mode 0)</returns>
    public float Evaluate(Vector3 point, int mode, float altitude) {
        float noiseValue = 0;
        float noiseDrift = 0;
        float frequency = baseRoughness;
        float amplitude = 1;
        float centerChange = driftOffset;
        Vector3 centerOff = new Vector3(center.x + centerChange, center.y + centerChange, center.z + centerChange);

        if (mode == 0) {

            for (int i = 0; i < octaves; i++) {
                float v = noise.Evaluate(point * frequency + center);
                float drift = noise.Evaluate(point * (frequency / driftFactor) + centerOff);
                noiseValue += v * amplitude;
                noiseDrift += drift * amplitude;
                frequency *= roughness;
                amplitude *= persistance;

                centerChange += driftOffset;
                centerOff = new Vector3(center.x + centerChange, center.y + centerChange, center.z + centerChange);
            }
            noiseValue = Mathf.Abs(noiseValue);
            noiseDrift = Mathf.Abs(noiseDrift);
            float defaultElevation = 1 - noiseValue;
            float modifier = Mathf.Abs(altitudeCrusher * noiseDrift * defaultElevation);
            float elevation = Mathf.Max(noiseDrift, 0.1f) * (1 - (noiseValue + modifier)) - 0.0001f;

            return elevation * elevation * elevation * strength;
        } else if (mode == 1) {
            //Chunkier Noise
            Vector3 off = new Vector3(3, 3, 3);
            off += center;
            float ratio = 1 - Mathf.Abs(point.normalized.y);
            float blur = Mathf.Max(octaves / 2 - 1, 1);
            float plus;
            for (int i = 0; i < blur; i++) {
                float v = noise.Evaluate(point * (frequency * 2) + center);
                plus = noise.Evaluate(point * (frequency) + off);
                noiseValue += v * amplitude;
                noiseDrift += plus * amplitude;
                frequency *= roughness;
                amplitude *= persistance;
            }
            float amount = Mathf.Clamp01(noiseValue);
            return ratio * (1 - (amount * amount * .75f)) * (1 - (noiseDrift * noiseDrift * .25f));
        } else {
            Vector3 off = new Vector3(8, 8, 8);
            off += center;
            float ratio = 1 - Mathf.Abs(point.normalized.y);
            float blur = Mathf.Max(octaves / 2 - 1, 1);
            float plus;
            for (int i = 0; i < blur; i++) {
                float v = noise.Evaluate(point * (frequency) + center);
                plus = noise.Evaluate(point * (frequency) + off);
                noiseValue += v * amplitude;
                noiseDrift += plus * amplitude;
                frequency *= roughness;
                amplitude *= persistance;
            }
            float amount = Mathf.Clamp01(noiseValue);
            amount = ratio * (1 - (amount * amount * .25f)) * (1 + (noiseDrift * noiseDrift * .5f)) + (.1f * ratio) - (altitude * .5f);
            return Mathf.Clamp01(amount);
        }

    }
}
