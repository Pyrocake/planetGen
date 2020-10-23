using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class TerrainInstance {

    public volatile Mesh mesh;
    public Vector3 localUP;
    Vector3 axisA;
    Vector3 axisB;
    float radius;
    public Tile parentTile;
    public PlanetGen planetGen;
    public MainBuilder mainBuilder;
    public List<Tile> visibleChildren = new List<Tile>();

    //Mesh data lists
    public List<Vector3> vertices = new List<Vector3>();
    public List<Vector3> borderVertices = new List<Vector3>();
    public List<Vector3> normals = new List<Vector3>();
    public List<int> triangles = new List<int>();
    public List<int> borderTriangles = new List<int>();
    public Dictionary<int, bool> edgefanIndex = new Dictionary<int, bool>();

    public MinMax elevationMinMax;

    ShapeGenerator shapeGenerator;

    public List<Vector3> highestDefined = new List<Vector3>();

    public TerrainInstance(Mesh mesh, Vector3 localUP, float radius, PlanetGen planetGen, MainBuilder mainBuilder) {
        this.mesh = mesh;
        this.localUP = localUP;
        this.radius = radius;
        this.planetGen = planetGen;
        this.mainBuilder = mainBuilder;

        axisA = new Vector3(localUP.y, localUP.z, localUP.x);
        axisB = Vector3.Cross(localUP, axisA);
    }

    public void BuildTileTree() {
        vertices.Clear();
        triangles.Clear();
        normals.Clear();
        borderVertices.Clear();
        borderTriangles.Clear();
        visibleChildren.Clear();

        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        parentTile = new Tile(1, planetGen, this, null, localUP.normalized * planetGen.size, radius, 0, localUP, axisA, axisB, new byte[4], 0, mainBuilder);
        parentTile.GenerateChildren();

        //Begin data
        int triangleOffset = 0;
        int borderTriangleOffset = 0;
        parentTile.GetVisibleChildren();
        foreach (Tile child in visibleChildren) {
            child.GetNeighborLOD();
            (Vector3[], int[], int[], Vector3[], Vector3[]) vertsAndTris = child.CalculateVertsAndTris(triangleOffset, borderTriangleOffset);

            vertices.AddRange(vertsAndTris.Item1);
            triangles.AddRange(vertsAndTris.Item2);
            borderTriangles.AddRange(vertsAndTris.Item3);
            borderVertices.AddRange(vertsAndTris.Item4);
            normals.AddRange(vertsAndTris.Item5);
            triangleOffset += vertsAndTris.Item1.Length;
            borderTriangleOffset += vertsAndTris.Item4.Length;
        }

        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        // mesh.uv = uv;
        planetGen.UpdateUV(mesh);
    }

    public void UpdateTree() {
        vertices.Clear();
        triangles.Clear();
        normals.Clear();
        borderVertices.Clear();
        borderTriangles.Clear();
        visibleChildren.Clear();
        edgefanIndex.Clear();

        parentTile.UpdateTile();

        int triangleOffset = 0;
        int borderTriangleOffset = 0;
        parentTile.GetVisibleChildren();
        foreach (Tile child in visibleChildren) {
            child.GetNeighborLOD();
            (Vector3[], int[], int[], Vector3[], Vector3[]) vertsAndTris = (new Vector3[0], new int[0], new int[0], new Vector3[0], new Vector3[0]);

            if (child.vertices == null) {
                vertsAndTris = child.CalculateVertsAndTris(triangleOffset, borderTriangleOffset);
            } else if (child.vertices.Length == 0 || child.triangles != Presets.quadTemplateTriangles[(child.neighbors[0] | child.neighbors[1] * 2 | child.neighbors[2] * 4 | child.neighbors[3] * 8)]) {
                vertsAndTris = child.CalculateVertsAndTris(triangleOffset, borderTriangleOffset);
            } else {
                vertsAndTris = (child.vertices, child.GetTrianglesWithOffset(triangleOffset), child.GetBorderTrianglesWithOffset(borderTriangleOffset, triangleOffset), child.borderVertices, child.normals);
            }

            vertices.AddRange(vertsAndTris.Item1);
            triangles.AddRange(vertsAndTris.Item2);
            borderTriangles.AddRange(vertsAndTris.Item3);
            borderVertices.AddRange(vertsAndTris.Item4);
            normals.AddRange(vertsAndTris.Item5);

            // Increase offset to accurately point to the next slot in the lists
            triangleOffset += (Presets.quadRes + 1) * (Presets.quadRes + 1);
            borderTriangleOffset += vertsAndTris.Item4.Length;
        }

        //Vector2[] uvs = (mesh.uv.Length == vertices.Count) ? mesh.uv : new Vector2[vertices.Count];

        //float planetGenSizeDivide = (1 / planetGen.size);
        //float twoPiDivide = (1 / (2 * Mathf.PI));
        //
        //for (int i = 0; i < uvs.Length; i++) {
        //    Vector3 d = vertices[i] * planetGenSizeDivide;
        //    float u = 0.5f + Mathf.Atan2(d.z, d.x) * twoPiDivide;
        //    float v = 0.5f - Mathf.Asin(d.y) / Mathf.PI;
        //
        //    uvs[i] = new Vector2(u, v);
        //}

        // Reset mesh and apply new data
        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        planetGen.UpdateUV(mesh);
    }
}
public class Tile {
    public uint hashvalue;
    public PlanetGen planetGen;
    public TerrainInstance terrainInstance;
    public MainBuilder mainBuilder;

    public Tile[] children;
    public Vector3 position;
    public float radius;
    public int detailLevel;
    public Vector3 localUP;
    public Vector3 axisA;
    public Vector3 axisB;
    public byte corner;

    public Vector3 normalizedPos;

    public Vector3[] vertices;
    public Vector3[] borderVertices;
    public int[] triangles;
    public int[] borderTriangles;
    public Vector3[] normals;

    public byte[] neighbors = new byte[4];

    public Tile(uint hashvalue, PlanetGen planetGen, TerrainInstance terrainInstance, Tile[] children, Vector3 position, float radius, int detailLevel, Vector3 localUP, Vector3 axisA, Vector3 axisB, byte[] neighbors, byte corner, MainBuilder mainBuilder) {

        this.hashvalue = hashvalue;
        this.planetGen = planetGen;
        this.terrainInstance = terrainInstance;
        this.children = children;
        this.position = position;
        this.radius = radius;
        this.detailLevel = detailLevel;
        this.localUP = localUP;
        this.axisA = axisA;
        this.axisB = axisB;
        this.neighbors = neighbors;
        this.corner = corner;
        this.normalizedPos = position.normalized;
        this.mainBuilder = mainBuilder;
    }

    public void GenerateChildren() {
        if (detailLevel <= planetGen.detailLevelDistances.Length - 1 && detailLevel >= 0) {
            if (Vector3.Distance(planetGen.transform.TransformDirection(normalizedPos * planetGen.size) + planetGen.transform.position, planetGen.player.position) <= planetGen.detailLevelDistances[detailLevel]) {
                children = new Tile[4];
                children[0] = new Tile(hashvalue * 4 + 0, planetGen, terrainInstance, new Tile[0], position + axisA * radius * 0.5f - axisB * radius * 0.5f, radius * 0.5f, detailLevel + 1, localUP, axisA, axisB, new byte[4], 0, mainBuilder);
                children[1] = new Tile(hashvalue * 4 + 1, planetGen, terrainInstance, new Tile[0], position + axisA * radius * 0.5f + axisB * radius * 0.5f, radius * 0.5f, detailLevel + 1, localUP, axisA, axisB, new byte[4], 1, mainBuilder);
                children[2] = new Tile(hashvalue * 4 + 2, planetGen, terrainInstance, new Tile[0], position - axisA * radius * 0.5f + axisB * radius * 0.5f, radius * 0.5f, detailLevel + 1, localUP, axisA, axisB, new byte[4], 2, mainBuilder);
                children[3] = new Tile(hashvalue * 4 + 3, planetGen, terrainInstance, new Tile[0], position - axisA * radius * 0.5f - axisB * radius * 0.5f, radius * 0.5f, detailLevel + 1, localUP, axisA, axisB, new byte[4], 3, mainBuilder);

                foreach (Tile child in children) {
                    child.GenerateChildren();
                }
            }
            
        }
    }

    // Update the Tile (and maybe its children too)
    public void UpdateTile() {
        float distanceToPlayer = Vector3.Distance(planetGen.transform.TransformDirection(normalizedPos * planetGen.size) + planetGen.transform.position, planetGen.player.position);
        if (detailLevel <= planetGen.detailLevelDistances.Length - 1) {
            if (distanceToPlayer > planetGen.detailLevelDistances[detailLevel]) {
                children = new Tile[0];
            } else {
                if (children.Length > 0) {
                    foreach (Tile child in children) {
                        child.UpdateTile();
                    }   
                } else {
                    GenerateChildren();
                }
            }
        }
    }

    // Returns the latest Tile in every branch, aka the ones to be rendered
    public void GetVisibleChildren() {
        if (children.Length > 0) {
            foreach (Tile child in children) {
                child.GetVisibleChildren();
            }
        } else {
            float b = Vector3.Distance(planetGen.transform.TransformDirection(normalizedPos * planetGen.size) +
                planetGen.transform.position, planetGen.player.position);

            if (Mathf.Acos(((planetGen.size * planetGen.size) + (b * b) -
                planetGen.distanceToPlayerAdjusted) / (2 * planetGen.size * b)) > planetGen.cullingMinAngle) {
                terrainInstance.visibleChildren.Add(this);
            }
        }
    }

    public void GetNeighborLOD() {
        byte[] newNeighbors = new byte[4];

        if (corner == 0) // Top left
        {
            newNeighbors[1] = CheckNeighborLOD(1, hashvalue); // West
            newNeighbors[2] = CheckNeighborLOD(2, hashvalue); // North
        } else if (corner == 1) // Top right
          {
            newNeighbors[0] = CheckNeighborLOD(0, hashvalue); // East
            newNeighbors[2] = CheckNeighborLOD(2, hashvalue); // North
        } else if (corner == 2) // Bottom right
          {
            newNeighbors[0] = CheckNeighborLOD(0, hashvalue); // East
            newNeighbors[3] = CheckNeighborLOD(3, hashvalue); // South
        } else if (corner == 3) // Bottom left
          {
            newNeighbors[1] = CheckNeighborLOD(1, hashvalue); // West
            newNeighbors[3] = CheckNeighborLOD(3, hashvalue); // South
        }

        neighbors = newNeighbors;
    }

    // Find neighbouring Tiles by applying a partial inverse bitmask to the hash
    private byte CheckNeighborLOD(byte side, uint hash) {
        uint bitmask = 0;
        byte count = 0;
        uint twoLast;

        while (count < detailLevel * 2) // 0 through 3 can be represented as a two bit number
        {
            count += 2;
            twoLast = (hash & 3); // Get the two last bits of the hash. 0b_10011 --> 0b_11

            bitmask *= 4; // Add zeroes to the end of the bitmask. 0b_10011 --> 0b_1001100

            // Create mask to get the quad on the opposite side. 2 = 0b_10 and generates the mask 0b_11 which flips it to 1 = 0b_01
            if (side == 2 || side == 3) {
                bitmask += 3; // Add 0b_11 to the bitmask
            } else {
                bitmask += 1; // Add 0b_01 to the bitmask
            }

            // Break if the hash goes in the opposite direction
            if ((side == 0 && (twoLast == 0 || twoLast == 3)) ||
                (side == 1 && (twoLast == 1 || twoLast == 2)) ||
                (side == 2 && (twoLast == 3 || twoLast == 2)) ||
                (side == 3 && (twoLast == 0 || twoLast == 1))) {
                break;
            }

            // Remove already processed bits. 0b_1001100 --> 0b_10011
            hash >>= 2;
        }

        // Return 1 (true) if the quad in quadstorage is less detailed
        if (terrainInstance.parentTile.GetNeighbourDetailLevel(hashvalue ^ bitmask, detailLevel) < detailLevel) {
            return 1;
        } else {
            return 0;
        }
    }

    // Find the detail level of the neighbouring quad using the querryHash as a map
    public int GetNeighbourDetailLevel(uint querryHash, int dl) {
        int dlResult = 0; // dl = detail level

        if (hashvalue == querryHash) {
            dlResult = detailLevel;
        } else {
            if (children.Length > 0) {
                dlResult += children[((querryHash >> ((dl - 1) * 2)) & 3)].GetNeighbourDetailLevel(querryHash, dl - 1);
            }
        }

        return dlResult; // Returns 0 if no quad with the given hash is found
    }

    // Return triangles including offset
    public int[] GetTrianglesWithOffset(int triangleOffset) {
        int[] newTriangles = new int[triangles.Length];

        for (int i = 0; i < triangles.Length; i++) {
            newTriangles[i] = triangles[i] + triangleOffset;
        }

        return newTriangles;
    }

    // Return border triangles including offset
    public int[] GetBorderTrianglesWithOffset(int borderTriangleOffset, int triangleOffset) {
        int[] newBorderTriangles = new int[borderTriangles.Length];

        for (int i = 0; i < borderTriangles.Length; i++) {
            newBorderTriangles[i] = (borderTriangles[i] < 0) ? borderTriangles[i] - borderTriangleOffset : borderTriangles[i] + triangleOffset;
        }

        return newBorderTriangles;
    }

    public (Vector3[], int[], int[], Vector3[], Vector3[]) CalculateVertsAndTris(int triangleOffset, int borderTriangleOffset) {
        Matrix4x4 transformMatrix;
        Vector3 rotationMatrixAttrib = new Vector3(0, 0, 0);
        Vector3 scaleMatrixAttrib = new Vector3(radius, radius, 1);

        // Adjust rotation according to the side of the planet
        if (terrainInstance.localUP == Vector3.forward) {
            rotationMatrixAttrib = new Vector3(0, 0, 180);
        } else if (terrainInstance.localUP == Vector3.back) {
            rotationMatrixAttrib = new Vector3(0, 180, 0);
        } else if (terrainInstance.localUP == Vector3.right) {
            rotationMatrixAttrib = new Vector3(0, 90, 270);
        } else if (terrainInstance.localUP == Vector3.left) {
            rotationMatrixAttrib = new Vector3(0, 270, 270);
        } else if (terrainInstance.localUP == Vector3.up) {
            rotationMatrixAttrib = new Vector3(270, 0, 90);
        } else if (terrainInstance.localUP == Vector3.down) {
            rotationMatrixAttrib = new Vector3(90, 0, 270);
        }

        // Create transform matrix
        transformMatrix = Matrix4x4.TRS(position, Quaternion.Euler(rotationMatrixAttrib), scaleMatrixAttrib);

        // Index of quad template
        int quadIndex = (neighbors[0] | neighbors[1] * 2 | neighbors[2] * 4 | neighbors[3] * 8);

        // Choose a quad from the templates, then move it using the transform matrix, normalize its vertices, scale it and store it
        vertices = new Vector3[(Presets.quadRes + 1) * (Presets.quadRes + 1)];

        for (int i = 0; i < vertices.Length; i++) {
            Vector3 pointOnCube = transformMatrix.MultiplyPoint(Presets.quadTemplateVertices[quadIndex][i]);
            Vector3 pointOnUnitSphere = pointOnCube.normalized;
            float elevation = planetGen.shapeBuilder.Evaluate(pointOnUnitSphere, 1);
            vertices[i] = pointOnUnitSphere * (1 + elevation) * planetGen.size;
            mainBuilder.CalculateElevation(pointOnUnitSphere);
        }

        // Do the same for the border vertices
        borderVertices = new Vector3[Presets.quadTemplateBorderVertices[quadIndex].Length];

        for (int i = 0; i < borderVertices.Length; i++) {
            Vector3 pointOnCube = transformMatrix.MultiplyPoint(Presets.quadTemplateBorderVertices[quadIndex][i]);
            Vector3 pointOnUnitSphere = pointOnCube.normalized;
            float elevation = planetGen.shapeBuilder.Evaluate(pointOnUnitSphere, 1);
            borderVertices[i] = pointOnUnitSphere * (1 + elevation) * planetGen.size;
            mainBuilder.CalculateElevation(pointOnUnitSphere);
        }

        // Store the triangles
        triangles = Presets.quadTemplateTriangles[quadIndex];
        borderTriangles = Presets.quadTemplateBorderTriangles[quadIndex];

        // MASSIVE CREDIT TO SEBASTIAN LAGUE FOR PROVIDING THE FOUNDATION FOR THE FOLLOWING CODE
        // Calculate the normals
        normals = new Vector3[vertices.Length];

        int triangleCount = triangles.Length / 3;

        int vertexIndexA;
        int vertexIndexB;
        int vertexIndexC;

        Vector3 triangleNormal;

        int[] edgefansIndices = Presets.quadTemplateEdgeIndices[quadIndex];

        for (int i = 0; i < triangleCount; i++) {
            int normalTriangleIndex = i * 3;
            vertexIndexA = triangles[normalTriangleIndex];
            vertexIndexB = triangles[normalTriangleIndex + 1];
            vertexIndexC = triangles[normalTriangleIndex + 2];

            triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);

            // Don't calculate the normals on the edge edgefans here. They are only calculated using the border vertices.
            if (edgefansIndices[vertexIndexA] == 0) {
                normals[vertexIndexA] += triangleNormal;
            }
            if (edgefansIndices[vertexIndexB] == 0) {
                normals[vertexIndexB] += triangleNormal;
            }
            if (edgefansIndices[vertexIndexC] == 0) {
                normals[vertexIndexC] += triangleNormal;
            }
        }

        int borderTriangleCount = borderTriangles.Length / 3;

        for (int i = 0; i < borderTriangleCount; i++) {
            int normalTriangleIndex = i * 3;
            vertexIndexA = borderTriangles[normalTriangleIndex];
            vertexIndexB = borderTriangles[normalTriangleIndex + 1];
            vertexIndexC = borderTriangles[normalTriangleIndex + 2];

            triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);

            // Apply the normal if the vertex is on the visible edge of the quad
            if (vertexIndexA >= 0 && (vertexIndexA % (Presets.quadRes + 1) == 0 ||
                vertexIndexA % (Presets.quadRes + 1) == Presets.quadRes ||
                (vertexIndexA >= 0 && vertexIndexA <= Presets.quadRes) ||
                (vertexIndexA >= (Presets.quadRes + 1) * Presets.quadRes && vertexIndexA < (Presets.quadRes + 1) * (Presets.quadRes + 1)))) {
                normals[vertexIndexA] += triangleNormal;
            }
            if (vertexIndexB >= 0 && (vertexIndexB % (Presets.quadRes + 1) == 0 ||
                vertexIndexB % (Presets.quadRes + 1) == Presets.quadRes ||
                (vertexIndexB >= 0 && vertexIndexB <= Presets.quadRes) ||
                (vertexIndexB >= (Presets.quadRes + 1) * Presets.quadRes && vertexIndexB < (Presets.quadRes + 1) * (Presets.quadRes + 1)))) {
                normals[vertexIndexB] += triangleNormal;
            }
            if (vertexIndexC >= 0 && (vertexIndexC % (Presets.quadRes + 1) == 0 ||
                vertexIndexC % (Presets.quadRes + 1) == Presets.quadRes ||
                (vertexIndexC >= 0 && vertexIndexC <= Presets.quadRes) ||
                (vertexIndexC >= (Presets.quadRes + 1) * Presets.quadRes && vertexIndexC < (Presets.quadRes + 1) * (Presets.quadRes + 1)))) {
                normals[vertexIndexC] += triangleNormal;
            }
        }

        // Normalize the result to combine the aproximations into one
        for (int i = 0; i < normals.Length; i++) {
            normals[i].Normalize();
        }

        return (vertices, GetTrianglesWithOffset(triangleOffset), GetBorderTrianglesWithOffset(borderTriangleOffset, triangleOffset), borderVertices, normals);
    }

    private Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC) {
        Vector3 pointA = (indexA < 0) ? borderVertices[-indexA - 1] : vertices[indexA];
        Vector3 pointB = (indexB < 0) ? borderVertices[-indexB - 1] : vertices[indexB];
        Vector3 pointC = (indexC < 0) ? borderVertices[-indexC - 1] : vertices[indexC];

        // Get an aproximation of the vertex normal using two other vertices that share the same triangle
        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }



}