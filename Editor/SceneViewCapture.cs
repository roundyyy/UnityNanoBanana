using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Roundy.UnityBanana
{
    public static class SceneViewCapture
    {
        private const string CAPTURE_CAMERA_NAME = "UnityBanana_CaptureCamera";

        /// <summary>
        /// Gets or creates the capture camera in the scene.
        /// </summary>
        public static Camera GetOrCreateCaptureCamera()
        {
            var existingCam = GameObject.Find(CAPTURE_CAMERA_NAME);
            if (existingCam != null)
            {
                var cam = existingCam.GetComponent<Camera>();
                if (cam != null)
                    return cam;
            }

            // Create new camera
            var camGO = new GameObject(CAPTURE_CAMERA_NAME);
            camGO.hideFlags = HideFlags.DontSave;
            var camera = camGO.AddComponent<Camera>();
            camera.enabled = false; // We'll render manually

            Debug.Log("[UnityBanana] Created capture camera");
            return camera;
        }

        /// <summary>
        /// Aligns the capture camera to match the current Scene View camera.
        /// </summary>
        public static bool AlignToSceneView(Camera captureCamera)
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                Debug.LogError("[UnityBanana] No active Scene View found. Please open a Scene View window.");
                return false;
            }

            var sceneCamera = sceneView.camera;
            if (sceneCamera == null)
            {
                Debug.LogError("[UnityBanana] Scene View camera not available.");
                return false;
            }

            // Copy transform
            captureCamera.transform.position = sceneCamera.transform.position;
            captureCamera.transform.rotation = sceneCamera.transform.rotation;

            // Copy camera settings
            captureCamera.fieldOfView = sceneCamera.fieldOfView;
            captureCamera.nearClipPlane = sceneCamera.nearClipPlane;
            captureCamera.farClipPlane = sceneCamera.farClipPlane;
            captureCamera.orthographic = sceneCamera.orthographic;
            captureCamera.orthographicSize = sceneCamera.orthographicSize;
            captureCamera.clearFlags = CameraClearFlags.Skybox;
            captureCamera.backgroundColor = sceneCamera.backgroundColor;

            return true;
        }

        /// <summary>
        /// Captures an image from the camera at the specified resolution.
        /// </summary>
        public static Texture2D CaptureFromCamera(Camera camera, int width, int height)
        {
            if (camera == null)
            {
                Debug.LogError("[UnityBanana] Camera is null");
                return null;
            }

            // Create render texture
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            renderTexture.antiAliasing = 8;

            // Store original target
            var originalTarget = camera.targetTexture;
            var originalActive = RenderTexture.active;

            try
            {
                // Render to texture
                camera.targetTexture = renderTexture;
                camera.Render();

                // Read pixels
                RenderTexture.active = renderTexture;
                var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();

                return texture;
            }
            finally
            {
                // Restore
                camera.targetTexture = originalTarget;
                RenderTexture.active = originalActive;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        /// <summary>
        /// Captures the current Scene View directly.
        /// </summary>
        public static Texture2D CaptureSceneViewDirect(int targetWidth, int targetHeight)
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                Debug.LogError("[UnityBanana] No active Scene View found.");
                return null;
            }

            // Get or create capture camera
            var captureCamera = GetOrCreateCaptureCamera();
            if (!AlignToSceneView(captureCamera))
            {
                return null;
            }

            // Capture
            var texture = CaptureFromCamera(captureCamera, targetWidth, targetHeight);

            // Cleanup camera if not keeping it
            if (!UnityBananaSettings.Instance.KeepCaptureCamera)
            {
                CleanupCaptureCamera();
            }

            return texture;
        }

        /// <summary>
        /// Removes the capture camera from the scene.
        /// </summary>
        public static void CleanupCaptureCamera()
        {
            var existingCam = GameObject.Find(CAPTURE_CAMERA_NAME);
            if (existingCam != null)
            {
                UnityEngine.Object.DestroyImmediate(existingCam);
            }
        }

        /// <summary>
        /// Converts texture to base64 PNG string.
        /// </summary>
        public static string TextureToBase64(Texture2D texture)
        {
            if (texture == null) return null;

            byte[] pngData = texture.EncodeToPNG();
            return Convert.ToBase64String(pngData);
        }

        /// <summary>
        /// Adds a custom Game View resolution (uses reflection for Unity's internal API).
        /// </summary>
        public static bool SetGameViewResolution(int width, int height, string label)
        {
            try
            {
                var gameViewSizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
                var singletonProp = gameViewSizesType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public);
                var singletonObj = singletonProp.GetValue(null, null);

                var getCurrentGroupMethod = gameViewSizesType.GetMethod("GetGroup");
                var gameViewSizeGroupType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeGroup");

                // Get current group (standalone by default)
                var currentGroup = getCurrentGroupMethod.Invoke(singletonObj, new object[] { (int)GameViewSizeGroupType.Standalone });

                // Check if size already exists
                var getDisplayTextsMethod = gameViewSizeGroupType.GetMethod("GetDisplayTexts");
                var displayTexts = (string[])getDisplayTextsMethod.Invoke(currentGroup, null);

                string targetLabel = $"UB_{width}x{height}";
                int existingIndex = -1;
                for (int i = 0; i < displayTexts.Length; i++)
                {
                    if (displayTexts[i].Contains(targetLabel))
                    {
                        existingIndex = i;
                        break;
                    }
                }

                // If not exists, add it
                if (existingIndex == -1)
                {
                    var gameViewSizeType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");
                    var gameViewSizeTypeEnum = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
                    var fixedResolution = Enum.Parse(gameViewSizeTypeEnum, "FixedResolution");

                    var constructor = gameViewSizeType.GetConstructor(new Type[] {
                        gameViewSizeTypeEnum, typeof(int), typeof(int), typeof(string)
                    });

                    var newSize = constructor.Invoke(new object[] { fixedResolution, width, height, targetLabel });

                    var addCustomSizeMethod = gameViewSizeGroupType.GetMethod("AddCustomSize");
                    addCustomSizeMethod.Invoke(currentGroup, new object[] { newSize });

                    // Refresh display texts
                    displayTexts = (string[])getDisplayTextsMethod.Invoke(currentGroup, null);
                    for (int i = 0; i < displayTexts.Length; i++)
                    {
                        if (displayTexts[i].Contains(targetLabel))
                        {
                            existingIndex = i;
                            break;
                        }
                    }
                }

                // Set the game view to use this resolution
                if (existingIndex >= 0)
                {
                    var gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
                    var gameView = EditorWindow.GetWindow(gameViewType);

                    var selectedSizeIndexProp = gameViewType.GetProperty("selectedSizeIndex",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (selectedSizeIndexProp != null)
                    {
                        selectedSizeIndexProp.SetValue(gameView, existingIndex, null);
                        gameView.Repaint();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UnityBanana] Could not set Game View resolution: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Gets current Game View size.
        /// </summary>
        public static (int width, int height) GetCurrentGameViewSize()
        {
            try
            {
                var gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
                var gameView = EditorWindow.GetWindow(gameViewType);

                var currentSizeMethod = gameViewType.GetMethod("GetMainGameViewSize",
                    BindingFlags.Static | BindingFlags.NonPublic);

                if (currentSizeMethod != null)
                {
                    var size = (Vector2)currentSizeMethod.Invoke(null, null);
                    return ((int)size.x, (int)size.y);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UnityBanana] Could not get Game View size: {ex.Message}");
            }

            return (1920, 1080);
        }

        // Helper enum for game view size group
        private enum GameViewSizeGroupType
        {
            Standalone = 0,
            WebPlayer = 1,
            iOS = 2,
            Android = 3,
            PS3 = 4,
            XBox360 = 5,
            WiiU = 6,
            Tizen = 7,
            WP8 = 8,
            Nintendo3DS = 9,
            tvOS = 10,
            NintendoSwitch = 11
        }
    }
}
