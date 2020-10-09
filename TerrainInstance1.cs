using UnityEngine;

public class TerrainInstance1 {

    ShapeGenerator shapeGenerator;
    Mesh mesh;
    int breakFactor;
    int iteration;

    int resolution;

    Vector3 localUP;
    Vector3 axisA;
    Vector3 axisB;

    public TerrainInstance1(ShapeGenerator shapeGenerator, Mesh mesh, int resolution, Vector3 localUP, int breakFactor, int iteration) {
        this.shapeGenerator = shapeGenerator;
        this.mesh = mesh;
        this.resolution = resolution;
        this.localUP = localUP;
        this.breakFactor = breakFactor;
        this.iteration = iteration;

        axisA = new Vector3(localUP.y, localUP.z, localUP.x);
        axisB = Vector3.Cross(localUP, axisA);
    }

    public void ConstructMesh() {
        Vector3[] vertices = new Vector3[resolution * resolution];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];
        int triangleIndex = 0;
        Vector3[] normals = new Vector3[resolution * resolution];
        Vector2[] uv = (mesh.uv.Length == vertices.Length) ? mesh.uv : new Vector2[vertices.Length];

        for (int y = 0; y < resolution; y++) {
            for (int x = 0; x < resolution; x++) {
                int i = x + y * resolution;

                int column = iteration / breakFactor;
                int row = iteration % breakFactor;

                Vector2 percent = new Vector2(x, y) / (resolution - 1);
                float scaler = 2 * (1 / (float)breakFactor);
                Vector3 pointOnUnit = localUP + (percent.x - .5f) * scaler * axisA + (percent.y - .5f) * scaler * axisB + (axisA * (column - ((float).5) * (breakFactor - 1)) * scaler) + (axisB * (row - ((float).5) * (breakFactor - 1)) * scaler);
                Vector3 pointOnUnitSphere = pointOnUnit.normalized;
                float unscaledElevation = shapeGenerator.CalculateUnscaledElevation(pointOnUnitSphere);
                //vertices[i] = pointOnUnitSphere * shapeGenerator.GetScaledElevation(unscaledElevation);
                uv[i].y = unscaledElevation;

                if (x != resolution - 1 && y != resolution - 1) {
                    triangles[triangleIndex] = i;
                    triangles[triangleIndex + 1] = i + resolution + 1;
                    triangles[triangleIndex + 2] = i + resolution;

                    triangles[triangleIndex + 3] = i;
                    triangles[triangleIndex + 4] = i + 1;
                    triangles[triangleIndex + 5] = i + resolution + 1;
                    triangleIndex += 6;
                }
                normals[i] = vertices[i].normalized;
            }
        }
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        //mesh.normals = vertices;
        mesh.uv = uv;
        //mesh.RecalculateNormals();
    }

    public void UpdateUVs(ColorGenerator colorGenerator) {
        Vector2[] uv = mesh.uv;
        for (int y = 0; y < resolution; y++) {
            for (int x = 0; x < resolution; x++) {
                int column = iteration / breakFactor;
                int row = iteration % breakFactor;
                float scaler = 2 * (1 / (float)breakFactor);

                int i = x + y * resolution;

                Vector2 percent = new Vector2(x, y) / (resolution - 1);
                Vector3 pointOnUnit = localUP + (percent.x - .5f) * scaler * axisA + (percent.y - .5f) * scaler * axisB + (axisA * (column - ((float).5) * (breakFactor - 1)) * scaler) + (axisB * (row - ((float).5) * (breakFactor - 1)) * scaler);
                Vector3 pointOnUnitSphere = pointOnUnit.normalized;

                uv[i].x = colorGenerator.BiomePercentFromPoint(pointOnUnitSphere);
            }
        }
        mesh.uv = uv;
    }

}
