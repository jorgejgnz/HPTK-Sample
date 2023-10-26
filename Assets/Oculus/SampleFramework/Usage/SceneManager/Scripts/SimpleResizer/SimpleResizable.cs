using UnityEngine;

/// <summary>
/// A MonoBehavior that provides the ability for a mesh to
/// be rescaled using 9-slice scaling (27-slicing in 3D).
/// For more details, see <seealso cref="SimpleResizer"/>>.
/// </summary>
[ExecuteInEditMode]
public class SimpleResizable : MonoBehaviour
{
    public Vector3 PivotPosition => _pivotTransform.position;

    [Space(15)] public Method ScalingX;
    [Range(0, 0.5f)] public float PaddingX;

    [Range(-0.5f, 0)] public float PaddingXMax;

    [Space(15)] public Method ScalingY;
    [Range(0, 0.5f)] public float PaddingY;

    [Range(-0.5f, 0)] public float PaddingYMax;

    [Space(15)] public Method ScalingZ;
    [Range(0, 0.5f)] public float PaddingZ;

    [Range(-0.5f, 0)] public float PaddingZMax;

    public enum Method
    {
        Adapt,
        AdaptWithAsymmetricalPadding,
        Scale,
        None
    }

    public Vector3 DefaultSize { get; private set; }
    public Mesh OriginalMesh { get; private set; }

    private Vector3 _oldSize;
    private MeshFilter _meshFilter;

    [SerializeField] private Vector3 _newSize;
    [SerializeField] private bool _updateInPlayMode;
    [SerializeField] private Transform _pivotTransform;

    public void SetNewSize(Vector3 newSize) => _newSize = newSize;

    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        OriginalMesh = GetComponent<MeshFilter>().sharedMesh;
        DefaultSize = OriginalMesh.bounds.size;
        _newSize = DefaultSize;
        _oldSize = _newSize;

        if (!_pivotTransform)
            _pivotTransform = transform.Find("Pivot");
    }

    private void OnEnable()
    {
        DefaultSize = OriginalMesh.bounds.size;
        if (_newSize == Vector3.zero)
            _newSize = DefaultSize;
    }

    private void Update()
    {
        if (Application.isPlaying && !_updateInPlayMode)
            return;

        if (_newSize != _oldSize)
        {
            _oldSize = _newSize;

            var resizedMesh = SimpleResizer.ProcessVertices(this, _newSize, true);
            _meshFilter.sharedMesh = resizedMesh;
            _meshFilter.sharedMesh.RecalculateBounds();
        }
    }

    private void OnDrawGizmos()
    {
        if (!_pivotTransform)
            return;

        Gizmos.color = Color.red;
        float lineSize = 0.1f;

        Vector3 startX = _pivotTransform.position + Vector3.left * lineSize * 0.5f;
        Vector3 startY = _pivotTransform.position + Vector3.down * lineSize * 0.5f;
        Vector3 startZ = _pivotTransform.position + Vector3.back * lineSize * 0.5f;

        Gizmos.DrawRay(startX, Vector3.right * lineSize);
        Gizmos.DrawRay(startY, Vector3.up * lineSize);
        Gizmos.DrawRay(startZ, Vector3.forward * lineSize);
    }

    private void OnDrawGizmosSelected()
    {
        // The furniture piece was not customized yet, nothing to do here
        if (_meshFilter.sharedMesh == null)
            return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Vector3 newCenter = _meshFilter.sharedMesh.bounds.center;

        Gizmos.color = new Color(1, 0, 0, 0.5f);
        switch (ScalingX)
        {
            case Method.Adapt:
                Gizmos.DrawWireCube(newCenter, new Vector3(_newSize.x * PaddingX * 2, _newSize.y, _newSize.z));
                break;
            case Method.AdaptWithAsymmetricalPadding:
                Gizmos.DrawWireCube(newCenter + new Vector3(
                    _newSize.x * PaddingX, 0, 0), new Vector3(0, _newSize.y, _newSize.z));
                Gizmos.DrawWireCube(newCenter + new Vector3(
                    _newSize.x * PaddingXMax, 0, 0), new Vector3(0, _newSize.y, _newSize.z));
                break;
            case Method.None:
                Gizmos.DrawWireCube(newCenter, _newSize);
                break;
        }

        Gizmos.color = new Color(0, 1, 0, 0.5f);
        switch (ScalingY)
        {
            case Method.Adapt:
                Gizmos.DrawWireCube(newCenter, new Vector3(_newSize.x, _newSize.y * PaddingY * 2, _newSize.z));
                break;
            case Method.AdaptWithAsymmetricalPadding:
                Gizmos.DrawWireCube(newCenter + new Vector3(0, _newSize.y * PaddingY, 0),
                    new Vector3(_newSize.x, 0, _newSize.z));
                Gizmos.DrawWireCube(newCenter + new Vector3(0, _newSize.y * PaddingYMax, 0),
                    new Vector3(_newSize.x, 0, _newSize.z));
                break;
            case Method.None:
                Gizmos.DrawWireCube(newCenter, _newSize);
                break;
        }

        Gizmos.color = new Color(0, 0, 1, 0.5f);
        switch (ScalingZ)
        {
            case Method.Adapt:
                Gizmos.DrawWireCube(newCenter, new Vector3(_newSize.x, _newSize.y, _newSize.z * PaddingZ * 2));
                break;
            case Method.AdaptWithAsymmetricalPadding:
                Gizmos.DrawWireCube(newCenter + new Vector3(0, 0, _newSize.z * PaddingZ),
                    new Vector3(_newSize.x, _newSize.y, 0));
                Gizmos.DrawWireCube(newCenter + new Vector3(0, 0, _newSize.z * PaddingZMax),
                    new Vector3(_newSize.x, _newSize.y, 0));
                break;
            case Method.None:
                Gizmos.DrawWireCube(newCenter, _newSize);
                break;
        }

        Gizmos.color = new Color(0, 1, 1, 1);
        Gizmos.DrawWireCube(newCenter, _newSize);
    }
}
