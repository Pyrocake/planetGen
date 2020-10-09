using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A lot of this code comes from Sebastian Lague, credit goes to him
public class NoiseFilter : MonoBehaviour {
    Noise noise = new Noise();

    public float strength = 1;
    [Range(1, 8)] public int octaves = 1;
    public float baseRoughness = 1;
    public float roughness = 2;
    public float persistance = .5f;
    public Vector3 center;

    //public Texture2D[] heightMap;
    //public Texture2D[] normalMap;

    public PlanetGen planetScript;
    public MainBuilder mainBuilder;

    /// <summary>
    /// This only finds base elevation properly if you supply seaLevel as != 0, seaLevel is not currently implemented
    /// </summary>
    /// <param name="point"></param>
    /// <param name="seaLevel"></param>
    /// <returns>The noise value at point, cubed, times strength</returns>
    public float Evaluate(Vector3 point,float seaLevel) {
        float noiseValue = 0;
        float frequency = baseRoughness;
        float amplitude = 1;

        if (seaLevel != 0) {

            for (int i = 0; i < octaves; i++) {
                float v = noise.Evaluate(point * frequency + center);
                noiseValue += v * amplitude;
                frequency *= roughness;
                amplitude *= persistance;
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
        float elevation = 1 - Mathf.Abs(noiseValue);

        return elevation * elevation * elevation * strength;
    }
}
