using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Roundy.UnityBanana
{
    public class UnityBananaWindow : EditorWindow
    {
        // Tab management
        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "Generation", "Settings" };

        // Generation tab state
        private int _selectedTemplateIndex = 0;
        private string _currentPrompt = "";
        private string _additionalInstructions = "";
        private bool _promptModified = false;
        private string _newTemplateName = "";

        // Reference images
        private Texture2D _characterReference;
        private Texture2D _styleReference;
        private List<Texture2D> _objectReferences = new List<Texture2D>();
        private List<Texture2D> _humanReferences = new List<Texture2D>();
        private bool _showReferenceImages = true;

        // Preview
        private Texture2D _capturePreview;
        private bool _showCapturePreview = false;

        // Settings tab state
        private string _apiKeyInput = "";
        private bool _showApiKey = false;
        private bool? _apiKeyValid = null;
        private string _apiValidationMessage = "";

        // Generation state
        private bool _isGenerating = false;
        private float _generationProgress = 0f;
        private string _generationStatus = "";
        private int _currentCoroutineId = -1;

        // Scroll positions
        private Vector2 _generationScrollPos;
        private Vector2 _settingsScrollPos;

        // Styles
        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _linkStyle;
        private GUIStyle _wrapTextStyle;
        private GUIStyle _greenButtonStyle;
        private GUIStyle _greenButtonLargeStyle;
        private bool _stylesInitialized = false;

        // Button textures
        private Texture2D _greenButtonNormal;
        private Texture2D _greenButtonHover;
        private Texture2D _greenButtonActive;

        // Banner
        private Texture2D _bannerTexture;
        private const string BANNER_PATH = "Assets/Roundy/UnityBanana/Images/banner_banana.jpeg";

        [MenuItem("Tools/UnityBanana/Open Generator", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<UnityBananaWindow>("UnityBanana");
            window.minSize = new Vector2(280, 400);
            window.Show();
        }

        [MenuItem("Tools/UnityBanana/Capture Scene View", false, 101)]
        public static void QuickCapture()
        {
            var window = GetWindow<UnityBananaWindow>("UnityBanana");
            window.CaptureSceneView();
        }

        private void OnEnable()
        {
            _apiKeyInput = UnityBananaSettings.ApiKey;
            NanoBananaAPI.OnProgressUpdate += OnProgressUpdate;

            // Initialize reference lists
            if (_objectReferences == null) _objectReferences = new List<Texture2D>();
            if (_humanReferences == null) _humanReferences = new List<Texture2D>();

            // Load initial template
            UpdatePromptFromTemplate();

            // Load banner
            _bannerTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(BANNER_PATH);
        }

        private void OnDisable()
        {
            NanoBananaAPI.OnProgressUpdate -= OnProgressUpdate;
            EditorCoroutineRunner.StopAllCoroutines(this);
            CleanupPreview();
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(0, 0, 10, 5)
            };

            _boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };

            _linkStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.3f, 0.5f, 1f) },
                hover = { textColor = new Color(0.5f, 0.7f, 1f) },
                stretchWidth = false
            };

            _wrapTextStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true
            };

            // Initialize green button textures
            _greenButtonNormal = MakeColorTexture(new Color(0.28f, 0.45f, 0.35f));
            _greenButtonHover = MakeColorTexture(new Color(0.32f, 0.50f, 0.40f));
            _greenButtonActive = MakeColorTexture(new Color(0.24f, 0.40f, 0.30f));

            // Green button style (normal size)
            _greenButtonStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = _greenButtonNormal, textColor = Color.white },
                hover = { background = _greenButtonHover, textColor = Color.white },
                active = { background = _greenButtonActive, textColor = Color.white },
                focused = { background = _greenButtonNormal, textColor = Color.white }
            };

            // Green button style (large, for Generate button)
            _greenButtonLargeStyle = new GUIStyle(_greenButtonStyle)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };

            _stylesInitialized = true;
        }

        private Texture2D MakeColorTexture(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        private void OnGUI()
        {
            InitializeStyles();

            // Draw banner at top
            DrawBanner();

            // Tab bar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames, EditorStyles.toolbarButton);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Tab content
            switch (_selectedTab)
            {
                case 0:
                    DrawGenerationTab();
                    break;
                case 1:
                    DrawSettingsTab();
                    break;
            }
        }

        private void DrawBanner()
        {
            if (_bannerTexture == null) return;

            // Calculate height based on current width to maintain aspect ratio
            float aspect = (float)_bannerTexture.width / _bannerTexture.height;
            float currentWidth = EditorGUIUtility.currentViewWidth;
            float bannerHeight = currentWidth / aspect;

            var bannerRect = GUILayoutUtility.GetRect(currentWidth, bannerHeight);
            GUI.DrawTexture(bannerRect, _bannerTexture, ScaleMode.ScaleToFit);
        }

        #region Generation Tab

        private void DrawGenerationTab()
        {
            _generationScrollPos = EditorGUILayout.BeginScrollView(_generationScrollPos);

            // API Key warning
            if (!UnityBananaSettings.IsApiKeyValid(UnityBananaSettings.ApiKey))
            {
                EditorGUILayout.HelpBox("API Key not configured. Please go to Settings tab to enter your Gemini API key.", MessageType.Warning);
                EditorGUILayout.Space(5);
            }

            // Resolution Section
            DrawResolutionSection();

            EditorGUILayout.Space(10);

            // Reference Images Section
            DrawReferenceImagesSection();

            EditorGUILayout.Space(10);

            // Style & Prompt Section
            DrawStylePromptSection();

            EditorGUILayout.Space(10);

            // Preview Section
            DrawPreviewSection();

            EditorGUILayout.Space(10);

            // Generate Button
            DrawGenerateSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawResolutionSection()
        {
            EditorGUILayout.LabelField(new GUIContent("Resolution", "Configure output image resolution"), _headerStyle);
            EditorGUILayout.BeginVertical(_boxStyle);

            var settings = UnityBananaSettings.Instance;

            // Aspect Ratio - vertical layout
            EditorGUILayout.LabelField(new GUIContent("Aspect Ratio:", "Aspect ratio for generated image"), EditorStyles.miniLabel);
            var aspectRatioNames = new string[UnityBananaSettings.AspectRatios.Length];
            for (int i = 0; i < UnityBananaSettings.AspectRatios.Length; i++)
            {
                aspectRatioNames[i] = UnityBananaSettings.AspectRatios[i].label;
            }
            int newAspectIndex = EditorGUILayout.Popup(settings.SelectedAspectRatioIndex, aspectRatioNames);
            if (newAspectIndex != settings.SelectedAspectRatioIndex)
            {
                settings.SelectedAspectRatioIndex = newAspectIndex;
            }

            // Image Size (Pro only) - vertical layout
            var sizeTooltip = UnityBananaSettings.IsProModel
                ? "Select output resolution (1K, 2K, or 4K)"
                : "Higher resolutions require Pro model";
            EditorGUILayout.LabelField(new GUIContent("Size:", sizeTooltip), EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = UnityBananaSettings.IsProModel;
            int newSizeIndex = EditorGUILayout.Popup(settings.SelectedImageSizeIndex, UnityBananaSettings.ImageSizeOptions);
            if (newSizeIndex != settings.SelectedImageSizeIndex)
            {
                settings.SelectedImageSizeIndex = newSizeIndex;
            }
            GUI.enabled = true;
            if (!UnityBananaSettings.IsProModel)
            {
                GUILayout.Label(new GUIContent("(Pro)", "Pro model required"), EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
            }
            EditorGUILayout.EndHorizontal();

            // Show current resolution
            var (width, height) = settings.CurrentResolution;
            EditorGUILayout.HelpBox($"{width} x {height} px", MessageType.None);

            EditorGUILayout.EndVertical();
        }

        private void DrawReferenceImagesSection()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Reference Images", "Add reference images to guide the AI generation"), _headerStyle);
            _showReferenceImages = EditorGUILayout.Foldout(_showReferenceImages, "", true);
            EditorGUILayout.EndHorizontal();

            if (!_showReferenceImages) return;

            EditorGUILayout.BeginVertical(_boxStyle);

            // Character Reference - vertical layout for narrow windows
            EditorGUILayout.LabelField(new GUIContent("Character:", "Reference for character consistency"), EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            _characterReference = (Texture2D)EditorGUILayout.ObjectField(_characterReference, typeof(Texture2D), false, GUILayout.Height(18));
            if (_characterReference != null && GUILayout.Button(new GUIContent("X", "Remove"), GUILayout.Width(20), GUILayout.Height(18)))
            {
                _characterReference = null;
            }
            EditorGUILayout.EndHorizontal();

            // Style Reference
            EditorGUILayout.LabelField(new GUIContent("Style:", "Reference for art style"), EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            _styleReference = (Texture2D)EditorGUILayout.ObjectField(_styleReference, typeof(Texture2D), false, GUILayout.Height(18));
            if (_styleReference != null && GUILayout.Button(new GUIContent("X", "Remove"), GUILayout.Width(20), GUILayout.Height(18)))
            {
                _styleReference = null;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Object References (up to 6)
            EditorGUILayout.LabelField(new GUIContent("Objects (up to 6):", "Objects to appear in generated image"), EditorStyles.miniLabel);
            DrawTextureList(_objectReferences, 6);

            EditorGUILayout.Space(5);

            // Human References (up to 5, Pro only)
            GUI.enabled = UnityBananaSettings.IsProModel;
            EditorGUILayout.LabelField(new GUIContent("Humans (up to 5, Pro):", "Human references (Pro only)"), EditorStyles.miniLabel);
            DrawTextureList(_humanReferences, 5);
            GUI.enabled = true;

            EditorGUILayout.EndVertical();
        }

        private void DrawTextureList(List<Texture2D> list, int maxCount)
        {
            for (int i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                list[i] = (Texture2D)EditorGUILayout.ObjectField(list[i], typeof(Texture2D), false);
                if (GUILayout.Button(new GUIContent("-", "Remove this reference"), GUILayout.Width(20)))
                {
                    list.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (list.Count < maxCount)
            {
                if (GUILayout.Button(new GUIContent("+ Add", "Add a new reference image"), GUILayout.Height(20)))
                {
                    list.Add(null);
                }
            }
        }

        private int CountReferences()
        {
            int count = 0;
            if (_characterReference != null) count++;
            if (_styleReference != null) count++;
            count += _objectReferences.FindAll(t => t != null).Count;
            count += _humanReferences.FindAll(t => t != null).Count;
            return count;
        }

        private void DrawStylePromptSection()
        {
            EditorGUILayout.LabelField(new GUIContent("Style & Prompt", "Configure the style and prompt for image generation"), _headerStyle);
            EditorGUILayout.BeginVertical(_boxStyle);

            var allTemplates = UnityBananaSettings.Instance.GetAllTemplates();
            var templateNames = StyleTemplates.GetTemplateNames(allTemplates);

            // Template dropdown - vertical layout
            EditorGUILayout.LabelField(new GUIContent("Template:", "Select a style template"), EditorStyles.miniLabel);
            int newTemplateIndex = EditorGUILayout.Popup(_selectedTemplateIndex, templateNames);
            if (newTemplateIndex != _selectedTemplateIndex)
            {
                _selectedTemplateIndex = newTemplateIndex;
                UpdatePromptFromTemplate();
            }

            EditorGUILayout.Space(5);

            // Prompt text area
            EditorGUILayout.LabelField(new GUIContent("Prompt:", "What to generate"), EditorStyles.miniLabel);
            string newPrompt = EditorGUILayout.TextArea(_currentPrompt, _wrapTextStyle, GUILayout.Height(70));
            if (newPrompt != _currentPrompt)
            {
                _currentPrompt = newPrompt;
                _promptModified = true;
            }

            // Additional instructions
            EditorGUILayout.LabelField(new GUIContent("Extra:", "Additional instructions (optional)"), EditorStyles.miniLabel);
            _additionalInstructions = EditorGUILayout.TextArea(_additionalInstructions, _wrapTextStyle, GUILayout.Height(35));

            EditorGUILayout.Space(5);

            // Save as template / Reset buttons
            EditorGUILayout.BeginHorizontal();
            if (_promptModified)
            {
                _newTemplateName = EditorGUILayout.TextField(_newTemplateName);
                GUI.enabled = !string.IsNullOrWhiteSpace(_newTemplateName);
                if (GUILayout.Button(new GUIContent("Save", "Save as new template"), GUILayout.Width(45)))
                {
                    SaveAsNewTemplate();
                }
                GUI.enabled = true;
            }
            else
            {
                GUILayout.FlexibleSpace();
            }
            if (GUILayout.Button(new GUIContent("Reset", "Reset to template"), GUILayout.Width(45)))
            {
                UpdatePromptFromTemplate();
                _promptModified = false;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawPreviewSection()
        {
            EditorGUILayout.LabelField(new GUIContent("Scene Capture", "Capture the current Scene View as input for generation"), _headerStyle);
            EditorGUILayout.BeginVertical(_boxStyle);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Capture Scene View", "Take a screenshot of the current Scene View window"), _greenButtonStyle, GUILayout.Height(30)))
            {
                CaptureSceneView();
            }

            if (_capturePreview != null)
            {
                if (GUILayout.Button(new GUIContent("Clear", "Remove the current capture"), GUILayout.ExpandWidth(false), GUILayout.Height(30)))
                {
                    CleanupPreview();
                }
            }
            EditorGUILayout.EndHorizontal();

            // Show preview
            if (_capturePreview != null)
            {
                EditorGUILayout.Space(5);
                _showCapturePreview = EditorGUILayout.Foldout(_showCapturePreview, "Preview", true);
                if (_showCapturePreview)
                {
                    float previewWidth = Mathf.Min(position.width - 40, 300);
                    float previewHeight = previewWidth * _capturePreview.height / _capturePreview.width;
                    var previewRect = GUILayoutUtility.GetRect(previewWidth, previewHeight);
                    GUI.DrawTexture(previewRect, _capturePreview, ScaleMode.ScaleToFit);
                    EditorGUILayout.HelpBox($"Captured: {_capturePreview.width}x{_capturePreview.height}", MessageType.None);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Click 'Capture Scene View' to take a screenshot of the current scene view.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawGenerateSection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);

            // Progress bar
            if (_isGenerating)
            {
                EditorGUI.ProgressBar(
                    EditorGUILayout.GetControlRect(GUILayout.Height(20)),
                    _generationProgress,
                    _generationStatus
                );
                EditorGUILayout.Space(5);
            }

            // Generate button
            GUI.enabled = !_isGenerating && _capturePreview != null && UnityBananaSettings.IsApiKeyValid(UnityBananaSettings.ApiKey);

            var buttonContent = _isGenerating
                ? new GUIContent("Generating...", "Image generation in progress")
                : new GUIContent("Generate Image", "Generate an AI image based on the captured scene and prompt");
            if (GUILayout.Button(buttonContent, _greenButtonLargeStyle, GUILayout.Height(40)))
            {
                StartGeneration();
            }
            GUI.enabled = true;

            // Status messages
            if (!UnityBananaSettings.IsApiKeyValid(UnityBananaSettings.ApiKey))
            {
                EditorGUILayout.HelpBox("Configure API key in Settings tab", MessageType.Warning);
            }
            else if (_capturePreview == null)
            {
                EditorGUILayout.HelpBox("Capture scene view first", MessageType.Info);
            }

            // Cancel button
            if (_isGenerating)
            {
                if (GUILayout.Button(new GUIContent("Cancel", "Stop the current generation")))
                {
                    CancelGeneration();
                }
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Settings Tab

        private void DrawSettingsTab()
        {
            _settingsScrollPos = EditorGUILayout.BeginScrollView(_settingsScrollPos);

            // API Key Section
            DrawApiKeySection();

            EditorGUILayout.Space(10);

            // Model Selection
            DrawModelSection();

            EditorGUILayout.Space(10);

            // Output Settings
            DrawOutputSection();

            EditorGUILayout.Space(10);

            // About Section
            DrawAboutSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawApiKeySection()
        {
            EditorGUILayout.LabelField(new GUIContent("API Key", "Configure your Gemini API key"), _headerStyle);
            EditorGUILayout.BeginVertical(_boxStyle);

            // API Key input - vertical layout
            EditorGUILayout.LabelField(new GUIContent("Gemini API Key:", "From Google AI Studio"), EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (_showApiKey)
            {
                _apiKeyInput = EditorGUILayout.TextField(_apiKeyInput);
            }
            else
            {
                _apiKeyInput = EditorGUILayout.PasswordField(_apiKeyInput);
            }
            if (GUILayout.Button(new GUIContent(_showApiKey ? "Hide" : "Show", "Toggle visibility"), GUILayout.Width(40)))
            {
                _showApiKey = !_showApiKey;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Save", "Save API key")))
            {
                UnityBananaSettings.ApiKey = _apiKeyInput;
                _apiKeyValid = null;
                Debug.Log("[UnityBanana] API key saved");
            }
            if (GUILayout.Button(new GUIContent("Test", "Validate API key")))
            {
                ValidateApiKey();
            }
            EditorGUILayout.EndHorizontal();

            // Validation status
            if (_apiKeyValid.HasValue)
            {
                var msgType = _apiKeyValid.Value ? MessageType.Info : MessageType.Error;
                EditorGUILayout.HelpBox(_apiValidationMessage, msgType);
            }

            EditorGUILayout.HelpBox("Get key: aistudio.google.com/apikey", MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        private void DrawModelSection()
        {
            EditorGUILayout.LabelField(new GUIContent("Model", "Choose the AI model"), _headerStyle);
            EditorGUILayout.BeginVertical(_boxStyle);

            EditorGUILayout.LabelField(new GUIContent("Model:", "Pro = higher quality, Standard = faster"), EditorStyles.miniLabel);
            int newModelIndex = EditorGUILayout.Popup(UnityBananaSettings.SelectedModelIndex, UnityBananaSettings.ModelOptions);
            if (newModelIndex != UnityBananaSettings.SelectedModelIndex)
            {
                UnityBananaSettings.SelectedModelIndex = newModelIndex;
            }

            // Model info - compact
            string info = UnityBananaSettings.IsProModel
                ? "Pro: 14 refs, 4K, better text"
                : "Standard: 3 refs, 1K, fast";
            EditorGUILayout.HelpBox(info, MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        private void DrawOutputSection()
        {
            var settings = UnityBananaSettings.Instance;

            EditorGUILayout.LabelField(new GUIContent("Output Settings", "Configure where and how generated images are saved"), _headerStyle);
            EditorGUILayout.BeginVertical(_boxStyle);

            // Output folder - vertical layout for narrow windows
            EditorGUILayout.LabelField(new GUIContent("Output Folder:", "Folder path where images are saved"), EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            settings.OutputFolder = EditorGUILayout.TextField(settings.OutputFolder);
            if (GUILayout.Button(new GUIContent("...", "Browse"), GUILayout.Width(25)))
            {
                string projectRoot = System.IO.Path.GetDirectoryName(Application.dataPath);
                string path = EditorUtility.OpenFolderPanel("Select Output Folder", projectRoot, "GeneratedImages");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(projectRoot))
                    {
                        path = path.Substring(projectRoot.Length + 1);
                    }
                    settings.OutputFolder = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            // File naming pattern
            EditorGUILayout.LabelField(new GUIContent("File Pattern:", "Use {style}, {timestamp}, {date}, {time}"), EditorStyles.miniLabel);
            settings.FileNamingPattern = EditorGUILayout.TextField(settings.FileNamingPattern);

            EditorGUILayout.Space(5);

            // Toggles
            settings.AutoOpenImage = EditorGUILayout.Toggle(
                new GUIContent("Auto-open in Explorer", "Open saved image in file explorer"),
                settings.AutoOpenImage);
            settings.KeepCaptureCamera = EditorGUILayout.Toggle(
                new GUIContent("Keep Capture Camera", "Keep the camera in scene after capture"),
                settings.KeepCaptureCamera);
            settings.SaveCapturedImage = EditorGUILayout.Toggle(
                new GUIContent("Save Captured Image", "Also save the original scene capture for comparison"),
                settings.SaveCapturedImage);

            EditorGUILayout.Space(5);

            if (GUILayout.Button(new GUIContent("Open Folder", "Open the output folder in file explorer")))
            {
                EditorUtility.RevealInFinder(settings.GetOutputPath());
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAboutSection()
        {
            EditorGUILayout.LabelField(new GUIContent("About", "Version and author information"), _headerStyle);
            EditorGUILayout.BeginVertical(_boxStyle);

            EditorGUILayout.LabelField($"UnityBanana v{UnityBananaSettings.VERSION}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Author: {UnityBananaSettings.AUTHOR}");

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Buy me a coffee :)", GUILayout.Width(110));
            if (GUILayout.Button(new GUIContent("Ko-fi", "Support on Ko-fi"), _linkStyle))
            {
                Application.OpenURL(UnityBananaSettings.KOFI_URL);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Source:", GUILayout.Width(55));
            if (GUILayout.Button(new GUIContent("GitHub", "View source on GitHub"), _linkStyle))
            {
                Application.OpenURL(UnityBananaSettings.GITHUB_URL);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Actions

        private void CaptureSceneView()
        {
            var settings = UnityBananaSettings.Instance;
            var (width, height) = settings.CurrentResolution;

            CleanupPreview();

            try
            {
                _capturePreview = SceneViewCapture.CaptureSceneViewDirect(width, height);

                if (_capturePreview != null)
                {
                    _showCapturePreview = true;
                    Debug.Log($"[UnityBanana] Captured scene view at {width}x{height}");
                }
                else
                {
                    EditorUtility.DisplayDialog("Capture Failed", "Could not capture scene view. Make sure a Scene View window is open.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityBanana] Capture failed: {ex}");
                EditorUtility.DisplayDialog("Capture Error", ex.Message, "OK");
            }
        }

        private void StartGeneration()
        {
            if (_capturePreview == null)
            {
                EditorUtility.DisplayDialog("No Capture", "Please capture the scene view first.", "OK");
                return;
            }

            if (!UnityBananaSettings.IsApiKeyValid(UnityBananaSettings.ApiKey))
            {
                EditorUtility.DisplayDialog("API Key Required", "Please configure your API key in the Settings tab.", "OK");
                return;
            }

            _isGenerating = true;
            _generationProgress = 0f;
            _generationStatus = "Starting...";

            var settings = UnityBananaSettings.Instance;
            var allTemplates = settings.GetAllTemplates();
            var selectedTemplate = allTemplates[_selectedTemplateIndex];

            // Build final prompt
            string finalPrompt = StyleTemplates.BuildFinalPrompt(
                new StyleTemplate(selectedTemplate.name, _currentPrompt, selectedTemplate.styleModifiers, false),
                _characterReference != null,
                _styleReference != null,
                _objectReferences.FindAll(t => t != null).Count,
                _humanReferences.FindAll(t => t != null).Count,
                _additionalInstructions
            );

            // Start generation coroutine
            _currentCoroutineId = EditorCoroutineRunner.StartCoroutine(
                NanoBananaAPI.GenerateImageCoroutine(
                    _capturePreview,
                    finalPrompt,
                    settings.CurrentAspectRatio.ratio,
                    settings.CurrentImageSize,
                    _characterReference,
                    _styleReference,
                    _objectReferences,
                    _humanReferences,
                    OnGenerationComplete
                ),
                this
            );
        }

        private void CancelGeneration()
        {
            if (_currentCoroutineId >= 0)
            {
                EditorCoroutineRunner.StopCoroutine(_currentCoroutineId);
                _currentCoroutineId = -1;
            }
            _isGenerating = false;
            _generationProgress = 0f;
            _generationStatus = "";
            Repaint();
        }

        private void OnProgressUpdate(float progress, string message)
        {
            _generationProgress = progress;
            _generationStatus = message;
            Repaint();
        }

        private void OnGenerationComplete(GenerationResult result)
        {
            _isGenerating = false;
            _currentCoroutineId = -1;

            if (result.Success)
            {
                var allTemplates = UnityBananaSettings.Instance.GetAllTemplates();
                var styleName = allTemplates[_selectedTemplateIndex].name;

                // Create a copy of the capture for the popup (original might be cleaned up)
                Texture2D captureCopy = null;
                if (_capturePreview != null)
                {
                    captureCopy = new Texture2D(_capturePreview.width, _capturePreview.height, _capturePreview.format, false);
                    Graphics.CopyTexture(_capturePreview, captureCopy);
                }

                // Show preview popup with capture for comparison
                var popup = ImagePreviewPopup.Show(
                    result.GeneratedImage,
                    styleName,
                    result.ResponseText,
                    StartGeneration,
                    captureCopy,
                    (texture) => {
                        // Callback when user clicks "Save & Use as Style"
                        _styleReference = texture;
                        _showReferenceImages = true;
                        
                        // Update settings with the last used folder path if needed
                        // Ideally we would load the asset at the saved path to ensure it's a project asset
                        // But for immediate visual feedback, the texture is fine.
                        // However, for the API call, we need a readable texture. 
                        // The saved image on disk is fine, but we need to make sure the reference held is valid.
                        
                        // If we want to ensure it's properly linked to the file system for next session:
                        // The popup saves the file. We can try to load it from the saved path if available,
                        // but ImagePreviewPopup doesn't pass the path back in the callback currently.
                        // For now, using the in-memory texture is sufficient for the immediate next generating session.
                        
                        Repaint();
                    }
                );

                Debug.Log($"[UnityBanana] Generation successful!");
            }
            else
            {
                Debug.LogError($"[UnityBanana] Generation failed: {result.ErrorMessage}");
                EditorUtility.DisplayDialog("Generation Failed", result.ErrorMessage, "OK");
            }

            Repaint();
        }

        private void ValidateApiKey()
        {
            EditorCoroutineRunner.StartCoroutine(
                NanoBananaAPI.ValidateApiKeyCoroutine(_apiKeyInput, (valid, message) =>
                {
                    _apiKeyValid = valid;
                    _apiValidationMessage = message;
                    Repaint();
                }),
                this
            );
        }

        private void UpdatePromptFromTemplate()
        {
            var allTemplates = UnityBananaSettings.Instance.GetAllTemplates();
            if (_selectedTemplateIndex >= 0 && _selectedTemplateIndex < allTemplates.Count)
            {
                var template = allTemplates[_selectedTemplateIndex];
                _currentPrompt = template.basePrompt;
                _promptModified = false;
            }
        }

        private void SaveAsNewTemplate()
        {
            if (string.IsNullOrWhiteSpace(_newTemplateName)) return;

            var allTemplates = UnityBananaSettings.Instance.GetAllTemplates();
            var currentTemplate = allTemplates[_selectedTemplateIndex];

            var newTemplate = new StyleTemplate(
                _newTemplateName,
                _currentPrompt,
                currentTemplate.styleModifiers,
                false
            );

            UnityBananaSettings.Instance.AddCustomTemplate(newTemplate);
            _newTemplateName = "";
            _promptModified = false;

            // Select new template
            allTemplates = UnityBananaSettings.Instance.GetAllTemplates();
            _selectedTemplateIndex = allTemplates.Count - 1;

            Debug.Log($"[UnityBanana] Saved new template: {newTemplate.name}");
        }

        private void CleanupPreview()
        {
            if (_capturePreview != null)
            {
                DestroyImmediate(_capturePreview);
                _capturePreview = null;
            }
            _showCapturePreview = false;
        }

        #endregion
    }
}
