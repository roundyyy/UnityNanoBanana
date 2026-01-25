using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Roundy.UnityBanana
{
    public class ImagePreviewPopup : EditorWindow
    {
        private Texture2D _previewImage;
        private Texture2D _captureImage;
        private string _styleName;
        private string _responseText;
        private string _savedPath;
        private string _savedCapturePath;
        private Vector2 _scrollPosition;
        private Action _onRegenerate;
        private bool _showSideBySide = false;

        private const float MIN_WIDTH = 400;
        private const float MIN_HEIGHT = 400;
        private const float BUTTON_HEIGHT = 28;
        private const float PADDING = 10;

        private Action<Texture2D> _onUseAsStyle;

        public static ImagePreviewPopup Show(Texture2D image, string styleName, string responseText, Action onRegenerate = null, Texture2D captureImage = null, Action<Texture2D> onUseAsStyle = null)
        {
            var window = GetWindow<ImagePreviewPopup>(true, "Generated Image", true);

            // Calculate window size based on image
            float windowWidth = Mathf.Max(MIN_WIDTH, image.width * 0.5f + PADDING * 2);
            float windowHeight = Mathf.Max(MIN_HEIGHT, image.height * 0.5f + 150);

            // Clamp to screen size
            windowWidth = Mathf.Min(windowWidth, Screen.currentResolution.width * 0.8f);
            windowHeight = Mathf.Min(windowHeight, Screen.currentResolution.height * 0.8f);

            window.minSize = new Vector2(MIN_WIDTH, MIN_HEIGHT);
            window.position = new Rect(
                (Screen.currentResolution.width - windowWidth) / 2,
                (Screen.currentResolution.height - windowHeight) / 2,
                windowWidth,
                windowHeight
            );

            window._previewImage = image;
            window._captureImage = captureImage;
            window._styleName = styleName;
            window._responseText = responseText;
            window._onRegenerate = onRegenerate;
            window._onUseAsStyle = onUseAsStyle;
            window._savedPath = null;
            window._savedCapturePath = null;
            window._showSideBySide = false;

            window.Show();
            return window;
        }

        private void OnGUI()
        {
            if (_previewImage == null)
            {
                EditorGUILayout.HelpBox("No image to display.", MessageType.Warning);
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // Header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label($"Style: {_styleName}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            // Side-by-side toggle
            if (_captureImage != null)
            {
                _showSideBySide = GUILayout.Toggle(_showSideBySide, new GUIContent("Compare", "Show original and generated side by side"), EditorStyles.toolbarButton);
            }

            GUILayout.Label($"{_previewImage.width}x{_previewImage.height}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(PADDING);

            // Image preview area
            var imageRect = GUILayoutUtility.GetRect(
                position.width - PADDING * 2,
                position.height - 180,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true)
            );

            // Draw background
            EditorGUI.DrawRect(imageRect, new Color(0.1f, 0.1f, 0.1f));

            if (_showSideBySide && _captureImage != null)
            {
                // Side by side comparison
                float halfWidth = imageRect.width / 2 - 2;

                // Left: Original capture
                var leftRect = new Rect(imageRect.x, imageRect.y, halfWidth, imageRect.height);
                DrawImageInRect(_captureImage, leftRect);

                // Divider
                EditorGUI.DrawRect(new Rect(imageRect.x + halfWidth, imageRect.y, 4, imageRect.height), new Color(0.3f, 0.3f, 0.3f));

                // Right: Generated
                var rightRect = new Rect(imageRect.x + halfWidth + 4, imageRect.y, halfWidth, imageRect.height);
                DrawImageInRect(_previewImage, rightRect);

                // Labels
                GUI.Label(new Rect(leftRect.x + 5, leftRect.y + 5, 80, 20), "Original", EditorStyles.whiteBoldLabel);
                GUI.Label(new Rect(rightRect.x + 5, rightRect.y + 5, 80, 20), "Generated", EditorStyles.whiteBoldLabel);
            }
            else
            {
                // Single image view
                DrawImageInRect(_previewImage, imageRect);
            }

            EditorGUILayout.Space(PADDING);

            // Response text (if any)
            if (!string.IsNullOrWhiteSpace(_responseText))
            {
                EditorGUILayout.LabelField("Response:", EditorStyles.boldLabel);
                EditorGUILayout.TextArea(_responseText, GUILayout.Height(50));
                EditorGUILayout.Space(5);
            }

            // Saved path notification
            if (!string.IsNullOrEmpty(_savedPath))
            {
                EditorGUILayout.HelpBox($"Saved: {System.IO.Path.GetFileName(_savedPath)}", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();

            // Bottom buttons - wrap if narrow
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(new GUIContent("Save", "Save image to output folder"), GUILayout.Height(BUTTON_HEIGHT)))
            {
                SaveImage();
            }

            if (_onUseAsStyle != null)
            {
                if (GUILayout.Button(new GUIContent("Save & Use as Style", "Save image and set as style reference"), GUILayout.Height(BUTTON_HEIGHT)))
                {
                    if (string.IsNullOrEmpty(_savedPath))
                    {
                        SaveImage();
                    }
                    
                    if (!string.IsNullOrEmpty(_savedPath))
                    {
                        // Load from AssetDatabase to get a proper persistent asset reference
                        Texture2D assetTexture = null;
                        string projectRoot = Path.GetDirectoryName(Application.dataPath);
                        if (_savedPath.StartsWith(projectRoot))
                        {
                            string relativePath = _savedPath.Substring(projectRoot.Length + 1).Replace("\\", "/");
                            assetTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(relativePath);
                        }

                        // Use the loaded asset if available, otherwise fall back to in-memory texture
                        _onUseAsStyle.Invoke(assetTexture != null ? assetTexture : _previewImage);
                        Close();
                    }
                }
            }

            GUI.enabled = !string.IsNullOrEmpty(_savedPath);
            if (GUILayout.Button(new GUIContent("Open", "Open in file explorer"), GUILayout.Height(BUTTON_HEIGHT)))
            {
                OpenInExplorer();
            }
            GUI.enabled = true;

            if (GUILayout.Button(new GUIContent("Copy", "Copy path to clipboard"), GUILayout.Height(BUTTON_HEIGHT)))
            {
                CopyToClipboard();
            }

            if (_onRegenerate != null)
            {
                if (GUILayout.Button(new GUIContent("Redo", "Regenerate image"), GUILayout.Height(BUTTON_HEIGHT)))
                {
                    _onRegenerate?.Invoke();
                    Close();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }

        private void DrawImageInRect(Texture2D texture, Rect rect)
        {
            float imageAspect = (float)texture.width / texture.height;
            float rectAspect = rect.width / rect.height;

            Rect drawRect;
            if (imageAspect > rectAspect)
            {
                float height = rect.width / imageAspect;
                drawRect = new Rect(rect.x, rect.y + (rect.height - height) / 2, rect.width, height);
            }
            else
            {
                float width = rect.height * imageAspect;
                drawRect = new Rect(rect.x + (rect.width - width) / 2, rect.y, width, rect.height);
            }

            GUI.DrawTexture(drawRect, texture, ScaleMode.ScaleToFit);
        }

        private void SaveImage()
        {
            var settings = UnityBananaSettings.Instance;
            var outputPath = settings.EnsureOutputPathExists();
            var fileName = settings.GenerateFileName(_styleName);
            var fullPath = Path.Combine(outputPath, fileName);

            try
            {
                // Save generated image
                byte[] pngData = _previewImage.EncodeToPNG();
                File.WriteAllBytes(fullPath, pngData);
                _savedPath = fullPath;
                Debug.Log($"[UnityBanana] Image saved to: {fullPath}");

                // Also save capture if setting enabled
                if (settings.SaveCapturedImage && _captureImage != null)
                {
                    var captureFileName = settings.GenerateCaptureFileName();
                    var captureFullPath = Path.Combine(outputPath, captureFileName);
                    byte[] capturePngData = _captureImage.EncodeToPNG();
                    File.WriteAllBytes(captureFullPath, capturePngData);
                    _savedCapturePath = captureFullPath;
                    Debug.Log($"[UnityBanana] Capture saved to: {captureFullPath}");
                }

                // Refresh AssetDatabase so Unity can see the new files
                if (outputPath.Contains("Assets"))
                {
                    AssetDatabase.Refresh();
                }

                if (settings.AutoOpenImage)
                {
                    EditorUtility.RevealInFinder(fullPath);
                }

                // Update settings to keep original resolution
                SetTextureImporterSettings(fullPath);
                if (settings.SaveCapturedImage && _captureImage != null && !string.IsNullOrEmpty(_savedCapturePath))
                {
                    SetTextureImporterSettings(_savedCapturePath);
                }
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Save Error", $"Failed to save image: {ex.Message}", "OK");
                Debug.LogError($"[UnityBanana] Failed to save image: {ex}");
            }
        }

        private void SetTextureImporterSettings(string fullPath)
        {
            try
            {
                // Convert full path to relative project path (Assets/...)
                string projectRoot = Path.GetDirectoryName(Application.dataPath);
                if (fullPath.StartsWith(projectRoot))
                {
                    string relativePath = fullPath.Substring(projectRoot.Length + 1).Replace("\\", "/");
                    
                    var importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
                    if (importer != null)
                    {
                        importer.npotScale = TextureImporterNPOTScale.None;
                        importer.SaveAndReimport();
                        Debug.Log($"[UnityBanana] Set NPOT Scale to None for: {relativePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UnityBanana] Failed to update import settings: {ex.Message}");
            }
        }

        private void OpenInExplorer()
        {
            if (!string.IsNullOrEmpty(_savedPath) && File.Exists(_savedPath))
            {
                EditorUtility.RevealInFinder(_savedPath);
            }
        }

        private void CopyToClipboard()
        {
            try
            {
                // Save to temp file and copy path
                var tempPath = Path.Combine(Path.GetTempPath(), "UnityBanana_clipboard.png");
                byte[] pngData = _previewImage.EncodeToPNG();
                File.WriteAllBytes(tempPath, pngData);

                // Copy path to clipboard
                EditorGUIUtility.systemCopyBuffer = tempPath;

                EditorUtility.DisplayDialog("Copied", $"Image path copied to clipboard:\n{tempPath}", "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Copy Error", $"Failed to copy: {ex.Message}", "OK");
            }
        }

        private void OnDestroy()
        {
            // Clean up capture texture (we own this copy)
            if (_captureImage != null)
            {
                DestroyImmediate(_captureImage);
                _captureImage = null;
            }
        }
    }
}
