using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainBuilder {
    public PlanetGen planetGen;
    public Material surfaceMat;

    public MinMax elevationMinMax;

    public Texture2D texture;
    const int textureResolution = 50;

    public BiomeBuilder settings;

    INoiseFilter biomeNoiseFilter;

    public void UpdateSettings(BiomeBuilder biomeBuilder, PlanetGen planet) {
        settings = biomeBuilder;
        surfaceMat = biomeBuilder.material;
        planetGen = planet;
        
        if (texture == null || texture.height != settings.biomeColorSettings.biomes.Length) {
            Debug.Log("Texture initialization beginning");
            texture = new Texture2D(textureResolution * 2, settings.biomeColorSettings.biomes.Length, TextureFormat.RGBA32, false);
        }
        //noiseFilter = new NoiseFilter();
        biomeNoiseFilter = NoiseFactory.CreateNoiseFilter(settings.biomeColorSettings.legacyNoiseSettings);

        elevationMinMax = new MinMax();
        
    }

    public void CalculateElevation(Vector3 pointOnUnit) {
        float elevation = planetGen.noiseFilter.Evaluate(pointOnUnit, 1);
        Vector3 elevate = pointOnUnit * (1 + elevation) * planetGen.size;
        elevationMinMax.AddValue(elevate.magnitude);
    }

    public void UpdateElevation() {
        if (surfaceMat == null) {
            Debug.LogWarning("This should not be possible. Critical Error in Material collection");
        }
        if (Application.isPlaying)
        {
            surfaceMat.SetVector("_elevationMinMax", new Vector4(elevationMinMax.Min, elevationMinMax.Max));
        }
        //Debug.Log(elevation.Min);
    }

    public float BiomePoint(Vector3 pointOnUnit, PlanetGen planetGen) {
        float heightPercent = (pointOnUnit.y + 1) / 2f;
        //Debug.Log(heightPercent);
        heightPercent += (biomeNoiseFilter.Evaluate(pointOnUnit) - planetGen.biomeBuilder.biomeColorSettings.noiseOffset) * planetGen.biomeBuilder.biomeColorSettings.noiseStrength;
        
        float biomeIndex = 0;
        int numBiomes = planetGen.biomeBuilder.biomeColorSettings.biomes.Length;
        //Debug.Log(numBiomes);
        float blendRange = planetGen.biomeBuilder.biomeColorSettings.blendAmount / 2f + .001f;

        for (int i = 0; i < numBiomes; i++) {
            float dst = heightPercent - planetGen.biomeBuilder.biomeColorSettings.biomes[i].startHeight;
            float weight = Mathf.InverseLerp(-blendRange, blendRange, dst);
            biomeIndex *= (1 - weight);
            biomeIndex += i * weight;
        }
        //Debug.Log("Biome Index: " + (biomeIndex / Mathf.Max(1, numBiomes - 1)) + ", Vector Input Y: " + pointOnUnit.y);
        return biomeIndex / Mathf.Max(1, numBiomes - 1);
    }

    public void UpdateColors() {
        if (texture == null || texture.height != settings.biomeColorSettings.biomes.Length) {
            texture = new Texture2D(textureResolution * 2, settings.biomeColorSettings.biomes.Length, TextureFormat.RGBA32, false);
        }
        Color[] colors = new Color[texture.width * texture.height];
        int colorIndex = 0;
        foreach (var biome in settings.biomeColorSettings.biomes) {
            for (int i = 0; i < textureResolution * 2; i++) {
                Color gradientCol;
                if (i < textureResolution) {
                    gradientCol = settings.oceanSettings.oceanColor.Evaluate(i / (textureResolution - 1f));
                } else {
                    gradientCol = biome.gradient.Evaluate((i - textureResolution) / (textureResolution - 1f));
                }
                Color tintCol = biome.tint;
                colors[colorIndex] = gradientCol * (1 - biome.tintPercent) + tintCol * biome.tintPercent;
                colorIndex++;
            }
        }
        texture.SetPixels(colors);
        texture.Apply();
        surfaceMat.SetTexture("_texture", texture);
        surfaceMat.SetFloat("_seaLevel", settings.oceanSettings.seaLevel);
    }
        
}
