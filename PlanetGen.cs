using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

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

    [Range(2, 32)] public int colliderSize = 4;

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

    //Not Implemented
    //public Erosion erosion;

    [HideInInspector]
    public float distanceToPlayer;
    [HideInInspector]
    public float distanceToPlayerAdjusted;

    public BiomeBuilder biomeBuilder;
    public NoiseFilter shapeBuilder;

    MainBuilder mainBuilder = new MainBuilder();

    bool unFixed = true;

    public int mapWidth = 1600;
    public int mapHeight = 900;
    public Texture2D map;

    private void Awake() {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        distanceToPlayer = Vector3.Distance(transform.position, player.position);
    }
    private void Start() {
        Initialize();
        GenerateMesh();
        
        StopAllCoroutines();
        StartCoroutine(PlanetGenerationLoop());

        StartCoroutine(StupidFix());

    }

    private void Update() {
        distanceToPlayer = Vector3.Distance(transform.position, player.position);
        distanceToPlayerAdjusted = distanceToPlayer * distanceToPlayer;
        LocalCollider();
    }

    private IEnumerator StupidFix() {
        while (unFixed) {
            yield return null;
            player.gameObject.GetComponent<Rigidbody>().MovePosition(transform.forward * -100f);
            player.gameObject.GetComponent<Rigidbody>().MovePosition(transform.forward * 100f);
            unFixed = false;
        }
        StopCoroutine(StupidFix());
    }

    private IEnumerator PlanetGenerationLoop() {
        while (true) {
            yield return new WaitForSeconds(0.5f);
            UpdateMesh();
        }
    }

    private void OnDrawGizmos() {
        if (Application.isPlaying) {
            
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
        
        //UpdateCollider();

    }

    //Deprecated, collider is entire planet instead of localized.
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

    public void LocalCollider() {
        Mesh mesh = new Mesh();

        if (!GameObject.Find("Collider")) {
            GameObject colObj = new GameObject("Collider");
            colObj.transform.parent = transform;
            colObj.AddComponent<MeshFilter>();
            colObj.AddComponent<MeshCollider>();
            colObj.layer = 10;
        }

        GameObject collideObj = GameObject.Find("Collider");

        Vector3 fakeZero = player.transform.position - transform.position;

        //Old Calculations for Virtual Axis
        //Vector3 collideAxisA = new Vector3(fakeZero.normalized.y, fakeZero.normalized.z, fakeZero.normalized.x);
        //Vector3 collideAxisB = Vector3.Cross(fakeZero.normalized, collideAxisA);

        int gridSize = colliderSize;
        float offset = gridSize / 2;

        Vector3 collideAxisA = player.forward / offset;
        Vector3 collideAxisB = player.right / offset;

        Vector3[] verts = new Vector3[(gridSize+1) * (gridSize+1)];
        int index = 0;
        //Debug.Log("New Run");
        for (int x = 0; x < gridSize+1; x++) {
            for (int y = 0; y < gridSize+1; y++) {
                //Pseudocode: The vert Vector at index = the relative x times x + the relative y times y + player position, minus the offset x and y to center it
                Vector3 positionOnLand = (collideAxisA * x) + (collideAxisB * y) + fakeZero + (-collideAxisA * offset) + (-collideAxisB * offset);
                positionOnLand = positionOnLand.normalized;
                float elevation =  shapeBuilder.Evaluate(positionOnLand, 0);
                positionOnLand = positionOnLand * (1 + elevation) * size;
                verts[index] = positionOnLand;
                index++;
            }
        }

        int[] tris = new int[verts.Length * 6];
        int triangle = 0;
        for (int x = 0; x < gridSize; x++) {
            for (int y = 0; y < gridSize; y++) {

                //offsets vert # by row length, per loop
                int level = x * (gridSize + 1);

                tris[triangle + 0] = (level + y); //Assuming vertex 0,
                tris[triangle + 1] = (level + y + (gridSize + 1)); //This would be 5,
                tris[triangle + 2] = (level + y + 1); //and 1

                tris[triangle + 3] = (level + y + 1); //1 again
                tris[triangle + 4] = (level + y + (gridSize + 1)); //5 again
                tris[triangle + 5] = (level + y + (gridSize + 1) + 1); //6, completing a square.

                triangle += 6;
            }
        }
        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = tris;
        collideObj.GetComponent<MeshFilter>().sharedMesh = mesh;
        collideObj.GetComponent<MeshCollider>().sharedMesh = mesh;

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
        //UpdateCollider();
        //LocalCollider();
    }

    void GenerateColors() {
        mainBuilder.UpdateColors();
    }

    public void GenerateRain(Mesh mesh) {
        mainBuilder.UVMapBiomes(mesh);
    }

    public void OnBiomeSettingsUpdated() {
        if (Application.isPlaying) {
            foreach (TerrainInstance face in terrainInstances) {
                UpdateUV(face.mesh);
            }
            mainBuilder.UpdateElevation();
            mainBuilder.UpdateColors();
            switch ((int)biomeBuilder.viewMode) {
                case 0:
                    surfaceMat.SetFloat("_showVars", 0);
                    break;
                case 1:
                    surfaceMat.SetFloat("_showVars", 1);
                    surfaceMat.SetFloat("_showRain", 0);
                    break;
                case 2:
                    surfaceMat.SetFloat("_showVars", 1);
                    surfaceMat.SetFloat("_showRain", 1);
                    break;
            }
        }
    }
    public void OnShapeSettingsUpdated() {
        if (Application.isPlaying) {
            this.Start();
        }
    }

    public Texture2D CreateTheMap() {
        //This isn't in MainBuilder because the editor tab doesn't like that one for some reason
        Debug.Log("Map Creation Initializing");
        map = mainBuilder.CreateMap();
        return map;
    }
}
