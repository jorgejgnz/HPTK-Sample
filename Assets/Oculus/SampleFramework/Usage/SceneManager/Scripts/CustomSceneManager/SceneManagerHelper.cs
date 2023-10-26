using System;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

/// <summary>
/// A smaller helper class for Custom Scene Manager samples.
/// </summary>
public class SceneManagerHelper
{
    public GameObject AnchorGameObject { get; }

    public SceneManagerHelper(GameObject gameObject)
    {
        AnchorGameObject = gameObject;
    }

    public void SetLocation(OVRLocatable locatable, Camera camera = null)
    {
        if (!locatable.TryGetSceneAnchorPose(out var pose))
            return;

        var projectionCamera = camera == null ? Camera.main : camera;
        var position = pose.ComputeWorldPosition(projectionCamera);
        var rotation = pose.ComputeWorldRotation(projectionCamera);

        if (position != null && rotation != null)
            AnchorGameObject.transform.SetPositionAndRotation(
                position.Value, rotation.Value);
    }

    public void CreatePlane(OVRBounded2D bounds)
    {
        var planeGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        planeGO.name = "Plane";
        planeGO.transform.SetParent(AnchorGameObject.transform, false);
        planeGO.transform.localScale = new Vector3(
            bounds.BoundingBox.size.x,
            bounds.BoundingBox.size.y,
            0.01f);
        planeGO.GetComponent<MeshRenderer>().material.SetColor(
            "_Color", UnityEngine.Random.ColorHSV());
    }

    public void UpdatePlane(OVRBounded2D bounds)
    {
        var planeGO = AnchorGameObject.transform.Find("Plane");
        if (planeGO == null)
            CreatePlane(bounds);
        else
        {
            planeGO.transform.localScale = new Vector3(
                bounds.BoundingBox.size.x,
                bounds.BoundingBox.size.y,
                0.01f);
        }
    }

    public void CreateVolume(OVRBounded3D bounds)
    {
        var volumeGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        volumeGO.name = "Volume";
        volumeGO.transform.SetParent(AnchorGameObject.transform, false);
        volumeGO.transform.localPosition = new Vector3(
            0, 0, -bounds.BoundingBox.size.z / 2);
        volumeGO.transform.localScale = bounds.BoundingBox.size;
        volumeGO.GetComponent<MeshRenderer>().material.SetColor(
            "_Color", UnityEngine.Random.ColorHSV());
    }

    public void UpdateVolume(OVRBounded3D bounds)
    {
        var volumeGO = AnchorGameObject.transform.Find("Volume");
        if (volumeGO == null)
            CreateVolume(bounds);
        else
        {
            volumeGO.transform.localPosition = new Vector3(
                0, 0, -bounds.BoundingBox.size.z / 2);
            volumeGO.transform.localScale = bounds.BoundingBox.size;
        }
    }

    public void CreateMesh(OVRTriangleMesh mesh)
    {
        if (!mesh.TryGetCounts(out var vcount, out var tcount)) return;
        using var vs = new NativeArray<Vector3>(vcount, Allocator.Temp);
        using var ts = new NativeArray<int>(tcount * 3, Allocator.Temp);
        if (!mesh.TryGetMesh(vs, ts)) return;

        var trimesh = new Mesh();
        trimesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        trimesh.SetVertices(vs);
        trimesh.SetTriangles(ts.ToArray(), 0);

        var meshGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
        meshGO.name = "Mesh";
        meshGO.transform.SetParent(AnchorGameObject.transform, false);
        meshGO.GetComponent<MeshFilter>().sharedMesh = trimesh;
        meshGO.GetComponent<MeshCollider>().sharedMesh = trimesh;
        meshGO.GetComponent<MeshRenderer>().material.SetColor(
            "_Color", UnityEngine.Random.ColorHSV());
    }

    public void UpdateMesh(OVRTriangleMesh mesh)
    {
        var meshGO = AnchorGameObject.transform.Find("Mesh");
        if (meshGO != null) UnityEngine.Object.Destroy(meshGO);
        CreateMesh(mesh);
    }

    /// <summary>
    /// A wrapper function for requesting Scene Capture.
    /// </summary>
    public static async Task<bool> RequestSceneCapture()
    {
        if (SceneCaptureRunning) return false;
        SceneCaptureRunning = true;

        var waiting = true;
        Action<ulong, bool> onCaptured = (id, success) => { waiting = false; };

        // subscribe, make non-blocking call, yield and wait
        return await Task.Run(() =>
        {
            OVRManager.SceneCaptureComplete += onCaptured;
            if (!OVRPlugin.RequestSceneCapture("", out var _))
            {
                OVRManager.SceneCaptureComplete -= onCaptured;
                SceneCaptureRunning = false;
                return false;
            }
            while (waiting) Task.Delay(200);
            OVRManager.SceneCaptureComplete -= onCaptured;
            SceneCaptureRunning = false;
            return true;
        });
    }
    private static bool SceneCaptureRunning = false; // single instance

    /// <summary>
    /// A wrapper function for requesting the Android
    /// permission for scene data.
    /// </summary>
    public static void RequestScenePermission()
    {
        const string permission = "com.oculus.permission.USE_SCENE";
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission))
            UnityEngine.Android.Permission.RequestUserPermission(permission);
    }
}
