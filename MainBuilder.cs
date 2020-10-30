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

    public int mapWidth;
    public int mapHeight;

    INoiseFilter biomeNoiseFilter;

    public List<Vector3> rainfall = new List<Vector3>();

    public void UpdateSettings(BiomeBuilder biomeBuilder, PlanetGen planet) {
        settings = biomeBuilder;
        surfaceMat = biomeBuilder.material;
        planetGen = planet;
        mapHeight = planet.mapHeight;
        mapWidth = planet.mapWidth;

        if (texture == null || texture.height != settings.biomeColorSettings.biomes.Length) {
            Debug.Log("Texture initialization beginning");
            texture = new Texture2D(textureResolution * 2, settings.biomeColorSettings.biomes.Length, TextureFormat.RGBA32, false);
        }
        biomeNoiseFilter = NoiseFactory.CreateNoiseFilter(settings.biomeColorSettings.legacyNoiseSettings);

        elevationMinMax = new MinMax();

    }

    public void CalculateElevation(Vector3 pointOnUnit) {
        float elevation = planetGen.shapeBuilder.Evaluate(pointOnUnit, 0);
        Vector3 elevate = pointOnUnit * (1 + elevation) * planetGen.size;
        elevationMinMax.AddValue(elevate.magnitude);
    }

    public void UpdateElevation() {
        if (surfaceMat == null) {
            Debug.LogWarning("This should not be possible. Critical Error in Material collection");
        }
        if (Application.isPlaying) {
            surfaceMat.SetVector("_elevationMinMax", new Vector4(elevationMinMax.Min, elevationMinMax.Max));
        }
    }

    public float CalculateRainfall(Vector3 point) {
        point = point.normalized;
        float rainfall = Mathf.Clamp01(planetGen.shapeBuilder.Evaluate(point, 1));
        return rainfall;
    }

    public float CalculateHeat(Vector3 point) {
        float oldRange = surfaceMat.GetVector("_elevationMinMax").y - surfaceMat.GetVector("_elevationMinMax").x;
        float oldMin = surfaceMat.GetVector("_elevationMinMax").x;
        float elevationNormal = (point.magnitude - oldMin) / oldRange;
        point = point.normalized;
        float heat = Mathf.Clamp01(planetGen.shapeBuilder.Evaluate(point, 2, elevationNormal));
        return heat;
    }

    public float BiomePoint(Vector3 pointOnUnit, PlanetGen planetGen) {
        float heightPercent = (pointOnUnit.y + 1) / 2f;
        heightPercent += (biomeNoiseFilter.Evaluate(pointOnUnit) - planetGen.biomeBuilder.biomeColorSettings.noiseOffset) * planetGen.biomeBuilder.biomeColorSettings.noiseStrength;

        float biomeIndex = 0;
        int numBiomes = planetGen.biomeBuilder.biomeColorSettings.biomes.Length;
        float blendRange = planetGen.biomeBuilder.biomeColorSettings.blendAmount / 2f + .001f;

        for (int i = 0; i < numBiomes; i++) {
            float dst = heightPercent - planetGen.biomeBuilder.biomeColorSettings.biomes[i].startHeight;
            float weight = Mathf.InverseLerp(-blendRange, blendRange, dst);
            biomeIndex *= (1 - weight);
            biomeIndex += i * weight;
        }
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
                if (settings.biomeColorSettings.tintToggle) {
                    colors[colorIndex] = gradientCol * (0) + tintCol * 1;
                }
                colorIndex++;
            }
        }
        texture.SetPixels(colors);
        texture.Apply();
        surfaceMat.SetTexture("_texture", texture);
        surfaceMat.SetFloat("_seaLevel", settings.oceanSettings.seaLevel);
    }
    public void UVMapBiomes(Mesh mesh) {
        
        Vector3[] verts = mesh.vertices;
        Vector2[] uv3 = new Vector2[mesh.vertices.Length];
        for (int i = 0; i < verts.Length; i++) {
            Vector3 point = verts[i];
            float rainAmount = CalculateRainfall(point);
            float heatAmount = CalculateHeat(point);
            uv3[i].x = heatAmount;
            uv3[i].y = rainAmount;
        }
        mesh.uv4 = uv3;
    }

    public Texture2D CreateMap() {
        float oldRange = surfaceMat.GetVector("_elevationMinMax").y - surfaceMat.GetVector("_elevationMinMax").x;
        float oldMin = surfaceMat.GetVector("_elevationMinMax").x;

        Texture2D newMap = new Texture2D(mapWidth, mapHeight,TextureFormat.RGBA32, false);

        Color[] colors = new Color[mapWidth * mapHeight];

        int quad = mapWidth / 4;
        int index = 0;
        float seaLevel = planetGen.biomeBuilder.oceanSettings.seaLevel;

        for (int i = 0; i < mapHeight; i++) {
            int axis = 0;
            for (int j = 0; j < mapWidth; j++) {
                float interHeight = i / (float)mapHeight;
                Vector3 upAxis = Vector3.Lerp(Vector3.down, Vector3.up, interHeight);
                Vector3 sideAxis = Vector3.zero;
                
                if (j <= quad) {

                    float interWidth = axis / (float)mapWidth;
                    interWidth *= 4;
                    sideAxis = Vector3.Slerp(Vector3.forward, Vector3.left, interWidth);

                } else if (j <= 2 * quad && quad < j) {

                    int NewAxis = axis - quad;
                    float interWidth = NewAxis / (float)mapWidth;
                    interWidth *= 4;
                    sideAxis = Vector3.Slerp(Vector3.left, Vector3.back, interWidth);

                } else if (j <= 3 * quad && quad * 2 < j) {

                    int NewAxis = axis - (2 * quad);
                    float interWidth = NewAxis / (float)mapWidth;
                    interWidth *= 4;
                    sideAxis = Vector3.Slerp(Vector3.back, Vector3.right, interWidth);

                } else if (j <= 4 * quad && quad * 3 < j) {

                    int NewAxis = axis - (3 * quad);
                    float interWidth = NewAxis / (float)mapWidth;
                    interWidth *= 4;
                    sideAxis = Vector3.Slerp(Vector3.right, Vector3.forward, interWidth);

                } else {
                    Debug.LogWarning("Nothing should get this far.");
                }

                float oneMinus = 1 - upAxis.magnitude;

                Vector3 positionOnSphere = upAxis + (sideAxis * oneMinus);
                Vector3 position = positionOnSphere.normalized;
                float elevation = planetGen.shapeBuilder.Evaluate(position, 0);
                Vector3 positionOnLand = position * (1 + elevation) * planetGen.size;

                float preY = BiomePoint(position.normalized, planetGen);
                preY = Mathf.Lerp(0, settings.biomeColorSettings.biomes.Length, preY);

                float newVal = ((positionOnLand.magnitude - oldMin) / oldRange);

                int yFinal = (int)preY;

                if (newVal > seaLevel) {
                    //Initial Color Gradient
                    Gradient landGrad = settings.biomeColorSettings.biomes[yFinal].gradient;
                    Color landCol = landGrad.Evaluate(newVal);
                    //Difference Between Actual and Initial
                    float testDif = yFinal - preY;

                    if (testDif > 0 && yFinal != 0) {
                        //May sample biome down if not lowest biome
                        Gradient landSample = settings.biomeColorSettings.biomes[yFinal - 1].gradient;
                        Color sampleColor = landSample.Evaluate(newVal);
                        landCol = Color.Lerp(landCol, sampleColor, Mathf.Abs(testDif));

                    } else if (testDif < 0 && yFinal != settings.biomeColorSettings.biomes.Length - 1) {
                        //May sample up if not highest biome
                        Gradient landSample = settings.biomeColorSettings.biomes[yFinal + 1].gradient;
                        Color sampleColor = landSample.Evaluate(newVal);
                        landCol = Color.Lerp(landCol, sampleColor, Mathf.Abs(testDif));

                    }
                    colors[index] = landCol;
                } else {
                    Gradient waterGrad = settings.oceanSettings.oceanColor;
                    Color waterCol = waterGrad.Evaluate(newVal);
                    colors[index] = waterCol;
                }
                axis++;
                index++;
            }
        }

        newMap.SetPixels(colors);
        newMap.Apply();
        newMap.wrapMode = TextureWrapMode.Clamp;
        newMap.filterMode = FilterMode.Bilinear;
        SaveTextureAsPNG(newMap, "C:\\Users\\ejhth\\Desktop\\Unity Components\\Testmap.png");
        return newMap;
    }

    public static void SaveTextureAsPNG(Texture2D _texture, string _fullPath) {
        byte[] _bytes = _texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(_fullPath, _bytes);
        Debug.Log(_bytes.Length / 1024 + "Kb was saved as: " + _fullPath);
    }

}
