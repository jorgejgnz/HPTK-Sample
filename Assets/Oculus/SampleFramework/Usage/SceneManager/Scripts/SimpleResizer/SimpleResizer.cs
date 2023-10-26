using UnityEngine;

/// <summary>
/// This class will create a mesh with vertices that have been scaled using
/// 9-slice scaling in 3D (27-slicing) to solve issues that can arise
/// when meshes have been stretched along 1 axis more than the others.
/// </summary>
/// <remarks>
/// While this can solve issues related to geometry stretching, it does
/// not attempt to solve texture stretching issues, and should therefore be
/// used more as a starting point to see how to modify 3D models dynamically.
/// </remarks>
public class SimpleResizer
{
    public void CreateResizedObject(Vector3 newSize, GameObject parent, SimpleResizable sourcePrefab)
    {
        var prefab = MonoBehaviour.Instantiate(sourcePrefab.gameObject, Vector3.zero, Quaternion.identity);
        prefab.name = sourcePrefab.name;

        var resizable = prefab.GetComponent<SimpleResizable>();
        resizable.SetNewSize(newSize);
        if (resizable == null)
        {
            Debug.LogError("Resizable component missing.");
            return;
        }

        var resizedMesh = ProcessVertices(resizable, newSize);

        MeshFilter mf = prefab.GetComponent<MeshFilter>();
        mf.sharedMesh = resizedMesh;
        mf.sharedMesh.RecalculateBounds();

        // child it after creation so the bounds math plays nicely
        prefab.transform.parent = parent.transform;
        prefab.transform.localPosition = Vector3.zero;
        prefab.transform.localRotation = Quaternion.identity;

        // cleanup
        MonoBehaviour.Destroy(resizable);
    }

    internal static Mesh ProcessVertices(SimpleResizable resizable, Vector3 newSize, bool pivot = false)
    {
        Mesh originalMesh = resizable.OriginalMesh;
        Vector3 originalBounds = resizable.DefaultSize;

        // Force scaling if newSize is smaller than the original mesh
        SimpleResizable.Method methodX = (originalBounds.x < newSize.x)
            ? resizable.ScalingX
            : SimpleResizable.Method.Scale;
        SimpleResizable.Method methodY = (originalBounds.y < newSize.y)
            ? resizable.ScalingY
            : SimpleResizable.Method.Scale;
        SimpleResizable.Method methodZ = (originalBounds.z < newSize.z)
            ? resizable.ScalingZ
            : SimpleResizable.Method.Scale;

        Vector3[] resizedVertices = originalMesh.vertices;

        // Transform pivot to object local space otherwise a
        // world-space transform can affect the resizer
        Vector3 localSpacePivot = resizable.transform.InverseTransformPoint(
            resizable.PivotPosition);

        float pivotX = (1 / resizable.DefaultSize.x) * localSpacePivot.x;
        float pivotY = (1 / resizable.DefaultSize.y) * localSpacePivot.y;
        float pivotZ = (1 / resizable.DefaultSize.z) * localSpacePivot.z;

        for (int i = 0; i < resizedVertices.Length; i++)
        {
            Vector3 vertexPosition = resizedVertices[i];
            vertexPosition.x = CalculateNewVertexPosition(
                methodX,
                vertexPosition.x,
                originalBounds.x,
                newSize.x,
                resizable.PaddingX,
                resizable.PaddingXMax,
                pivotX);

            vertexPosition.y = CalculateNewVertexPosition(
                methodY,
                vertexPosition.y,
                originalBounds.y,
                newSize.y,
                resizable.PaddingY,
                resizable.PaddingYMax,
                pivotY);

            vertexPosition.z = CalculateNewVertexPosition(
                methodZ,
                vertexPosition.z,
                originalBounds.z,
                newSize.z,
                resizable.PaddingZ,
                resizable.PaddingZMax,
                pivotZ);

            if (pivot)
                vertexPosition += localSpacePivot;

            resizedVertices[i] = vertexPosition;
        }

        Mesh clonedMesh = MonoBehaviour.Instantiate(originalMesh);
        clonedMesh.vertices = resizedVertices;

        return clonedMesh;
    }

    private static float CalculateNewVertexPosition(
        SimpleResizable.Method resizeMethod,
        float currentPosition,
        float currentSize,
        float newSize,
        float padding,
        float paddingMax,
        float pivot)
    {
        float resizedRatio = currentSize / 2
                             * (newSize / 2 * (1 / (currentSize / 2)))
                             - currentSize / 2;

        switch (resizeMethod)
        {
            case SimpleResizable.Method.Adapt:
                if (Mathf.Abs(currentPosition) >= padding)
                    currentPosition = resizedRatio * Mathf.Sign(currentPosition) + currentPosition;
                break;

            case SimpleResizable.Method.AdaptWithAsymmetricalPadding:
                if (currentPosition >= padding)
                    currentPosition = resizedRatio * Mathf.Sign(currentPosition) + currentPosition;
                if (currentPosition <= paddingMax)
                    currentPosition = resizedRatio * Mathf.Sign(currentPosition) + currentPosition;
                break;

            case SimpleResizable.Method.Scale:
                currentPosition = newSize / (currentSize / currentPosition);
                break;

            case SimpleResizable.Method.None:
                break;
        }

        float pivotPos = newSize * (-pivot);
        currentPosition += pivotPos;

        return currentPosition;
    }
}
