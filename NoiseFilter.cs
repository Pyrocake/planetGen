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
    //This Factor is currently unutilized
    [Tooltip("Unused")]
    [Range(0f,.5f)] public float mysteryFactor = .25f;
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
    /// This only finds base elevation properly if you supply seaLevel as != 0, seaLevel is not currently implemented
    /// </summary>
    /// <param name="point"></param>
    /// <param name="seaLevel"></param>
    /// <returns>The noise value at point, cubed, times strength</returns>
    public float Evaluate(Vector3 point, float seaLevel) {
        float noiseValue = 0;
        float noiseDrift = 0;
        float frequency = baseRoughness;
        float amplitude = 1;
        float centerChange = .01f;
        Vector3 centerOff = new Vector3(center.x + centerChange, center.y + centerChange, center.z + centerChange);

        if (seaLevel != 0) {

            for (int i = 0; i < octaves; i++) {
                float v = noise.Evaluate(point * frequency + center);
                float drift = noise.Evaluate(point * (frequency/4f) + centerOff);
                noiseValue += v * amplitude;
                noiseDrift += drift * amplitude;
                frequency *= roughness;
                amplitude *= persistance;

                centerChange += .01f;
                centerOff = new Vector3(center.x + centerChange, center.y + centerChange, center.z + centerChange);
            }
        } else {
            for (int i = 0; i < octaves; i++) {
                float v = noise.Evaluate(point * frequency + center);
                noiseValue += v * amplitude;
                frequency *= roughness;
                amplitude *= persistance;
            }
            return 1 - Mathf.Abs(noiseValue);
        }
        //This may be useless, we'll see
        noiseValue = Mathf.Abs(noiseValue);
        noiseDrift = Mathf.Abs(noiseDrift);
        float defaultElevation = 1 - noiseValue;
        float modifier = Mathf.Abs(mysteryFactor * noiseDrift * defaultElevation);
        float elevation = noiseDrift * (1 - (noiseValue + modifier)) - 0.0001f;
        //elevation = Mathf.Clamp01(elevation);

        return elevation * elevation * elevation * strength;
    }
}
