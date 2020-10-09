using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class PlanetGen : MonoBehaviour {

    public Stopwatch timer;

    [SerializeField, HideInInspector]
    MeshFilter[] meshFilters;
    TerrainInstance[] terrainInstances;

    public bool lagSwitch;

    public bool killUpdates;

    [HideInInspector]
    public float cullingMinAngle = 1.45f;
    [HideInInspector]
    public float size = 1000;

    [HideInInspector]
    public float[] detailLevelDistances = new float[] {
        Mathf.Infinity,
        3000f,
        1100f,
        500f,
        210f,
        100f,
        40f,
    };

    public Material surfaceMat;

    public NoiseFilter noiseFilter;

    [Header("Miscellaneous")]
    public bool spawnFakeOcean = true;

    public Transform player;

    [HideInInspector]
    public float distanceToPlayer;
    [HideInInspector]
    public float distanceToPlayerAdjusted;


    public BiomeBuilder biomeBuilder;

    //ShapeGenerator shapeGenerator = new ShapeGenerator();
    MainBuilder mainBuilder = new MainBuilder();

    private void Awake() {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        distanceToPlayer = Vector3.Distance(transform.position, player.position);
    }

    private void Start() {
        timer = new Stopwatch();
        timer.Start();
        //Will clear shader on Start if this isn't here
        Initialize();
        GenerateMesh();
        timer.Stop();
        Debug.Log("Initialized. Time elapsed: " + timer.ElapsedMilliseconds + " ms");
        if (!killUpdates)
        {
            StartCoroutine(PlanetGenerationLoop());
        }
    }

    private void Update() {
        distanceToPlayer = Vector3.Distance(transform.position, player.position);
        distanceToPlayerAdjusted = distanceToPlayer * distanceToPlayer;
    }

    private IEnumerator PlanetGenerationLoop() {
        while (true) {
            yield return new WaitForSeconds(0.5f);
            if (transform.hasChanged) {
                UpdateMesh();
                transform.hasChanged = false;
            }
            //mainBuilder.UpdateColors();
        }
    }

    void Initialize() {

        mainBuilder.UpdateSettings(biomeBuilder, this);

        if (meshFilters == null || meshFilters.Length == 0) {
            meshFilters = new MeshFilter[6];
        }
        
            //gameObject.AddComponent<NoiseFilter>();

        terrainInstances = new TerrainInstance[6];

        Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
        string[] directionsEnglish = { "Up", "Down", "Left", "Right", "Forward", "Back" };

        //TODO: Consider the need for object face mesh seperation

        for (int i = 0; i < 6; i++) {

            if (meshFilters[i] == null) {
                GameObject meshObj = new GameObject(directionsEnglish[i]);
                meshObj.transform.parent = transform;

                meshObj.AddComponent<MeshRenderer>();
                meshFilters[i] = meshObj.AddComponent<MeshFilter>();
                meshFilters[i].sharedMesh = new Mesh();
            }
            meshFilters[i].GetComponent<MeshRenderer>().sharedMaterial = surfaceMat;
            terrainInstances[i] = new TerrainInstance(meshFilters[i].sharedMesh, directions[i], size, this, mainBuilder);
            //Debug.Log("No faults so far, Instance " + (i+1) + " initialized");
        }

        if (spawnFakeOcean) {
            GameObject ocean = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ocean.transform.parent = gameObject.transform;
            int scalerOcean = ((int)size * 2) + 25;
            Vector3 scaleOcean = new Vector3(scalerOcean, scalerOcean, scalerOcean);
            ocean.transform.localScale = scaleOcean;
        }
        

    }

    public void UpdateUV(Mesh mesh) {

        Vector2[] uv = new Vector2[mesh.vertices.Length];
        Vector3[] points = mesh.vertices;
        float oldRange = mainBuilder.elevationMinMax.Max - mainBuilder.elevationMinMax.Min;
        float oldMin = mainBuilder.elevationMinMax.Min;
        for (int i = 0; i < points.Length; i++) {
            if (points[i] != null) {
                uv[i].x = mainBuilder.BiomePoint(points[i].normalized,this);
                float newVal = ((points[i].magnitude - oldMin)/ oldRange);

                uv[i].y = newVal;

                if (uv[i].y < 0|| uv[i].y > 1)
                {
                    Debug.Log("This Y is being funky, don't like it: " + uv[i].y);
                }
            } else {
                Debug.LogWarning("This should not be possible");
            }

        }
        //God I hate you, UV
        mesh.uv = uv;
    }

    void GenerateMesh() {
        foreach (TerrainInstance face in terrainInstances) {
            face.BuildTileTree();
            
        }
        if (lagSwitch) {
            mainBuilder.UpdateElevation();
            mainBuilder.UpdateColors();
        }
       
        
    }

    void UpdateMesh()
    {
        foreach (TerrainInstance face in terrainInstances) {
            face.UpdateTree();
            //face.mesh.uv = UpdateUV(face.mesh);
        }

        if (lagSwitch) {
            mainBuilder.UpdateElevation();
        }
        //mainBuilder.UpdateColors(surfaceMat);

    }

    void GenerateColors() {
        mainBuilder.UpdateColors();
    }

    public void OnBiomeSettingsUpdated() {
        if (Application.isPlaying)
        {
            foreach (TerrainInstance face in terrainInstances)
            {
                UpdateUV(face.mesh);
            }
            mainBuilder.UpdateElevation();
            mainBuilder.UpdateColors();
        }
    }
    public void OnShapeSettingsUpdated() {

    }
}
