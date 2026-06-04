using UnityEngine;
using UnityEngine.UI;

namespace UnifyCountry.UI
{
    internal sealed class UnitModelPreview : MonoBehaviour
    {
        private const int PreviewLayer = 31;
        private const int TextureSize = 256;

        private GameObject rig;
        private GameObject modelInstance;
        private Camera previewCamera;
        private RenderTexture renderTexture;
        private RawImage targetImage;

        public void Initialize(GameObject modelPrefab, RawImage image, bool mirrorX)
        {
            targetImage = image;
            if (modelPrefab == null || targetImage == null)
                return;

            Cleanup();

            renderTexture = new RenderTexture(TextureSize, TextureSize, 16, RenderTextureFormat.ARGB32)
            {
                name = $"{modelPrefab.name} Preview RT",
                antiAliasing = 4
            };
            renderTexture.Create();

            targetImage.texture = renderTexture;
            targetImage.color = Color.white;

            rig = new GameObject($"{modelPrefab.name} Preview Rig");
            rig.transform.position = Vector3.zero;

            modelInstance = Instantiate(modelPrefab, rig.transform);
            modelInstance.name = $"{modelPrefab.name} Preview Model";
            SetLayerRecursively(modelInstance, PreviewLayer);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.Euler(0f, mirrorX ? 35f : -35f, 0f);
            modelInstance.transform.localScale = Vector3.one;

            var bounds = CalculateBounds(modelInstance);
            var center = bounds.center;
            var size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z, 0.01f);

            modelInstance.transform.localPosition -= center;

            var lightObject = new GameObject("Key Light", typeof(Light));
            lightObject.transform.SetParent(rig.transform, false);
            lightObject.transform.localPosition = new Vector3(-1.6f, 2.2f, -2.4f);
            lightObject.transform.localRotation = Quaternion.Euler(45f, -25f, 0f);
            lightObject.layer = PreviewLayer;

            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.4f;

            var cameraObject = new GameObject("Preview Camera", typeof(Camera));
            cameraObject.transform.SetParent(rig.transform, false);
            cameraObject.layer = PreviewLayer;

            previewCamera = cameraObject.GetComponent<Camera>();
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            previewCamera.cullingMask = 1 << PreviewLayer;
            previewCamera.orthographic = true;
            previewCamera.orthographicSize = size * 0.62f;
            previewCamera.nearClipPlane = 0.01f;
            previewCamera.farClipPlane = size * 8f;
            previewCamera.targetTexture = renderTexture;

            cameraObject.transform.localPosition = new Vector3(0f, size * 0.05f, -size * 2.5f);
            cameraObject.transform.LookAt(rig.transform.position + Vector3.up * size * 0.05f);

            rig.transform.position = new Vector3(10000f + Mathf.Abs(GetInstanceID()) * 2f, 0f, 0f);
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            if (targetImage != null)
                targetImage.texture = null;

            if (rig != null)
                DestroyObject(rig);

            if (renderTexture != null)
            {
                renderTexture.Release();
                DestroyObject(renderTexture);
            }

            rig = null;
            modelInstance = null;
            previewCamera = null;
            renderTexture = null;
        }

        private static Bounds CalculateBounds(GameObject target)
        {
            var renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0)
                return new Bounds(Vector3.zero, Vector3.one);

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            foreach (Transform child in target.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        private static void DestroyObject(Object target)
        {
            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }
    }
}
