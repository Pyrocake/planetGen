using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class PlanetGen : MonoBehaviour {

    [SerializeField, HideInInspector]
    MeshFilter[] meshFilters;
    TerrainInstance[] terrainInstances;

    [Header("Testing Bools")]
    public bool lagSwitch;

    public bool killUpdates;

    [HideInInspector]
    public float cullingMinAngle = 1.45f;

    public float size = 2000;

    //[HideInInspector]
    public float[] detailLevelDistances = new float[] {
        Mathf.Infinity,
        3000f,
        2000f,
        1100f,
        500f,
        210f,
        100f,
        40f,
    };

    public Material surfaceMat;

    [Header("Miscellaneous")]
    public bool spawnFakeOcean = true;

    public Transform player;

    [HideInInspector]
    public float distanceToPlayer;
    [HideInInspector]
    public float distanceToPlayerAdjusted;

    public BiomeBuilder biomeBuilder;
    public NoiseFilter shapeBuilder;

    MainBuilder mainBuilder = new MainBuilder();

    bool unFixed = true;

    private void Awake() {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        distanceToPlayer = Vector3.Distance(transform.position, player.position);
    }
    private void Start() {
        Initialize();
        GenerateMesh();
        StopAllCoroutines();
        StartCoroutine(PlanetGenerationLoop());

        StartCoroutine(stupidFix());
    }

    private void Update() {
        distanceToPlayer = Vector3.Distance(transform.position, player.position);
        distanceToPlayerAdjusted = distanceToPlayer * distanceToPlayer;
    }

    private IEnumerator stupidFix() {
        while (unFixed) {
            yield return new WaitForSeconds(1f);
            player.gameObject.GetComponent<Rigidbody>().MovePosition(transform.forward * -100f);
            player.gameObject.GetComponent<Rigidbody>().MovePosition(transform.forward * 100f);
            unFixed = false;
        }
        StopCoroutine(stupidFix());
    }

    private IEnumerator PlanetGenerationLoop() {
        while (true) {
            yield return new WaitForSeconds(0.5f);
            if (transform.hasChanged) {
                
                transform.hasChanged = false;
            }
            UpdateMesh();
        }
    }

    void Initialize() {

        mainBuilder.UpdateSettings(biomeBuilder, this);

        if (meshFilters == null || meshFilters.Length == 0) {
            meshFilters = new MeshFilter[6];
        }

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
            int scalerOcean = ((int)size * 2) + 3;
            Vector3 scaleOcean = new Vector3(scalerOcean, scalerOcean, scalerOcean);
            ocean.transform.localScale = scaleOcean;
        }

        UpdateCollider();

    }

    public void UpdateCollider() {
        for (int i = 0; i < 6; i++) {
            GameObject meshOb = meshFilters[i].gameObject;
            if (meshOb.GetComponent<MeshCollider>() == null) {
                meshOb.AddComponent<MeshCollider>();
            }
            meshOb.GetComponent<MeshCollider>().sharedMesh = null;
            meshOb.GetComponent<MeshCollider>().sharedMesh = terrainInstances[i].mesh;
            meshOb.layer = 10;
        }
    }

    public void UpdateUV(Mesh mesh) {

        Vector2[] uv = new Vector2[mesh.vertices.Length];
        Vector3[] points = mesh.vertices;
        float oldRange = mainBuilder.elevationMinMax.Max - mainBuilder.elevationMinMax.Min;
        float oldMin = mainBuilder.elevationMinMax.Min;
        for (int i = 0; i < points.Length; i++) {
            if (points[i] != null) {
                uv[i].x = mainBuilder.BiomePoint(points[i].normalized, this);
                float newVal = ((points[i].magnitude - oldMin) / oldRange);

                uv[i].y = newVal;

                if (uv[i].y < 0 || uv[i].y > 1) {
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

    void UpdateMesh() {
        foreach (TerrainInstance face in terrainInstances) {
            face.UpdateTree();
        }

        if (lagSwitch) {
            mainBuilder.UpdateElevation();
        }
        UpdateCollider();
    }

    void GenerateColors() {
        mainBuilder.UpdateColors();
    }

    public void OnBiomeSettingsUpdated() {
        if (Application.isPlaying) {
            foreach (TerrainInstance face in terrainInstances) {
                UpdateUV(face.mesh);
            }
            mainBuilder.UpdateElevation();
            mainBuilder.UpdateColors();
        }
    }
    public void OnShapeSettingsUpdated() {
        if (Application.isPlaying) {
            this.Start();
        }
    }
}
