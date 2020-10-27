using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPlacer : MonoBehaviour {

    public PlanetGen planetGen;

    public bool scatterObjects = true;

    [Range(0,10000)] public int primCount = 1000;
    [Range(0, 1000)] public int drawDistance = 250;

    public Material material;

    private void Start() {
        if (scatterObjects) {
            GameObject collector = new GameObject("Scattered Object Collector");
            collector.transform.parent = planetGen.gameObject.transform;

            for (int i = 0; i < 6; i++) {
                Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
                string[] directionsEnglish = { "Up", "Down", "Left", "Right", "Forward", "Back" };
                GameObject face = new GameObject(directionsEnglish[i]);
                face.transform.parent = collector.transform;

                for (int j = 0; j < primCount; j++) {
                    GameObject newObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    newObj.transform.parent = face.transform;
                    newObj.transform.position = RandomPlacement(directions[i]);
                    newObj.GetComponent<MeshRenderer>().material = material;

                    Vector3 orient = planetGen.transform.InverseTransformVector(newObj.transform.position);
                    Quaternion tilt = Quaternion.LookRotation(newObj.transform.forward, orient);
                    newObj.transform.rotation = tilt;
                    Debug.DrawLine(newObj.transform.position, newObj.transform.up);
                }
            }
        } else {
            Debug.Log("Reminder: Objects are not being scattered.");
        }
    }

    private void Update() {
        if (scatterObjects) {
            GameObject collector = transform.Find("Scattered Object Collector").gameObject;
            for (int i = 0; i < 6; i++) {

                GameObject face = planetGen.gameObject.transform.GetChild(i).gameObject;
                GameObject toDisable = collector.transform.GetChild(i).gameObject;

                if (face.GetComponent<MeshFilter>().sharedMesh.vertexCount == 0) {
                    if (!toDisable.name.Contains("(Hidden)")) {
                        toDisable.name += " (Hidden)";

                        for (int j = 0; j < primCount; j++) {
                            toDisable.transform.GetChild(j).gameObject.GetComponent<MeshRenderer>().enabled = false;
                        }
                    }
                } else {
                    if (toDisable.name.Contains("(Hidden)")) {
                        toDisable.name = face.name;
                    }
                    for (int j = 0; j < primCount; j++) {
                        float distance = Vector3.Distance(planetGen.player.position, toDisable.transform.GetChild(j).transform.position);
                        if (distance < drawDistance) {
                            toDisable.transform.GetChild(j).gameObject.GetComponent<MeshRenderer>().enabled = true;
                        } else {
                            toDisable.transform.GetChild(j).gameObject.GetComponent<MeshRenderer>().enabled = false;
                        }
                    }
                }
            }
        }
    }

    //Random pos on Unit Sphere, will NOT place intelligently
    public Vector3 RandomPlacement(Vector3 direction) {
        Vector3 axisA = new Vector3(direction.y, direction.z, direction.x);
        Vector3 axisB = Vector3.Cross(direction, axisA);

        //Vector3 testVector = Random.onUnitSphere;
        Vector3 point = ((axisA * 2 * (Random.value - .5f)) + (axisB * 2 * (Random.value - .5f)) + direction);
        float placement = planetGen.shapeBuilder.Evaluate(point.normalized, 1);
        float actualHeight = (1 + placement) * planetGen.size;

        Vector3 actualPlacement = transform.position + (point.normalized * actualHeight);
        return actualPlacement;
    }

}
