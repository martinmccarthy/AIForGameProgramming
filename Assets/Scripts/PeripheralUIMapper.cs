using UnityEngine;
using UnityEngine.UI;

// Renders a Canvas to a RenderTexture and displays it on a sphere
// positioned in the player's peripheral vision.
// Attach to any persistent GameObject in the scene.
public class PeripheralUIMapper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Camera xrCamera;

    [Header("Peripheral Position")]
    [Tooltip("Position relative to the XR camera. Negative X = left, negative Y = down, positive Z = forward.")]
    [SerializeField] private Vector3 peripheralOffset = new Vector3(-0.35f, -0.25f, 0.7f);
    [SerializeField] private float sphereScale = 0.2f;

    [Header("Render Texture")]
    [SerializeField] private int rtWidth = 512;
    [SerializeField] private int rtHeight = 256;

    private RenderTexture _rt;
    private Camera _uiCamera;
    private GameObject _sphere;

    private void Start()
    {
        if (targetCanvas == null || xrCamera == null)
        {
            Debug.LogWarning("PeripheralUIMapper: targetCanvas or xrCamera not assigned.");
            return;
        }

        SetupRenderTexture();
        SetupUICamera();
        SetupCanvas();
        SetupSphere();
    }

    private void SetupRenderTexture()
    {
        _rt = new RenderTexture(rtWidth, rtHeight, 16, RenderTextureFormat.ARGB32);
        _rt.Create();
    }

    private void SetupUICamera()
    {
        GameObject camObj = new GameObject("PeripheralUICamera");
        _uiCamera = camObj.AddComponent<Camera>();
        _uiCamera.clearFlags = CameraClearFlags.SolidColor;
        _uiCamera.backgroundColor = Color.clear;
        _uiCamera.cullingMask = LayerMask.GetMask("UI");
        _uiCamera.orthographic = true;
        _uiCamera.nearClipPlane = 0.1f;
        _uiCamera.farClipPlane = 100f;
        _uiCamera.depth = -10;
        _uiCamera.targetTexture = _rt;
        _uiCamera.allowHDR = false;
        _uiCamera.allowMSAA = false;
    }

    private void SetupCanvas()
    {
        targetCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        targetCanvas.worldCamera = _uiCamera;
        targetCanvas.planeDistance = 1f;
    }

    private void SetupSphere()
    {
        _sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _sphere.name = "PeripheralHUDSphere";

        Destroy(_sphere.GetComponent<Collider>());

        _sphere.transform.SetParent(xrCamera.transform, false);
        _sphere.transform.localPosition = peripheralOffset;
        _sphere.transform.localRotation = Quaternion.identity;
        _sphere.transform.localScale = Vector3.one * sphereScale;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Texture");

        Material mat = new Material(shader);
        mat.mainTexture = _rt;

        MeshRenderer mr = _sphere.GetComponent<MeshRenderer>();
        mr.material = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    private void OnDestroy()
    {
        if (_rt != null)
        {
            _rt.Release();
            Destroy(_rt);
        }
        if (_uiCamera != null)
            Destroy(_uiCamera.gameObject);
        if (_sphere != null)
            Destroy(_sphere);
    }
}
