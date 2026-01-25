using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Roundy.UnityBanana
{
    [Serializable]
    public class StyleTemplate
    {
        public string name;
        public string basePrompt;
        public string styleModifiers;
        public bool isBuiltIn;

        public StyleTemplate() { }

        public StyleTemplate(string name, string basePrompt, string styleModifiers, bool isBuiltIn = false)
        {
            this.name = name;
            this.basePrompt = basePrompt;
            this.styleModifiers = styleModifiers;
            this.isBuiltIn = isBuiltIn;
        }

        public StyleTemplate Clone()
        {
            return new StyleTemplate(name, basePrompt, styleModifiers, false);
        }
    }

    [Serializable]
    public class AspectRatioOption
    {
        public string label;
        public string ratio;
        public int width1K;
        public int height1K;
        public int width2K;
        public int height2K;
        public int width4K;
        public int height4K;

        public AspectRatioOption(string label, string ratio, int w1K, int h1K, int w2K, int h2K, int w4K, int h4K)
        {
            this.label = label;
            this.ratio = ratio;
            this.width1K = w1K;
            this.height1K = h1K;
            this.width2K = w2K;
            this.height2K = h2K;
            this.width4K = w4K;
            this.height4K = h4K;
        }

        public (int width, int height) GetResolution(string imageSize)
        {
            return imageSize switch
            {
                "2K" => (width2K, height2K),
                "4K" => (width4K, height4K),
                _ => (width1K, height1K)
            };
        }
    }

    public class UnityBananaSettings : ScriptableObject
    {
        private const string SETTINGS_PATH = "Assets/Roundy/UnityBanana/Resources/UnityBananaSettings.asset";
        private const string API_KEY_PREF = "UnityBanana_APIKey";
        private const string MODEL_INDEX_PREF = "UnityBanana_ModelIndex";

        [SerializeField] private List<StyleTemplate> customTemplates = new List<StyleTemplate>();
        [SerializeField] private int selectedAspectRatioIndex = 8; // Default to 16:9
        [SerializeField] private int selectedImageSizeIndex = 0;   // Default to 1K
        [SerializeField] private string outputFolder = "Assets/GeneratedImages";
        [SerializeField] private bool autoOpenImage = true;
        [SerializeField] private bool keepCaptureCamera = false;
        [SerializeField] private string fileNamingPattern = "{style}_{timestamp}";
        [SerializeField] private bool saveCapturedImage = false;

        // Version info
        public const string VERSION = "0.1";
        public const string AUTHOR = "Roundy";
        public const string KOFI_URL = "https://ko-fi.com/roundy";
        public const string GITHUB_URL = "https://github.com/roundyyy/UnityNanoBanana";

        // Model options
        public static readonly string[] ModelOptions = { "Nano Banana Pro (gemini-3-pro-image-preview)", "Nano Banana (gemini-2.5-flash-image)" };
        public static readonly string[] ModelIds = { "gemini-3-pro-image-preview", "gemini-2.5-flash-image" };

        // Image size options
        public static readonly string[] ImageSizeOptions = { "1K", "2K", "4K" };

        // Aspect ratio options for Gemini 3 Pro Image Preview
        public static readonly AspectRatioOption[] AspectRatios = {
            new AspectRatioOption("1:1 (Square)", "1:1", 1024, 1024, 2048, 2048, 4096, 4096),
            new AspectRatioOption("2:3 (Portrait)", "2:3", 848, 1264, 1696, 2528, 3392, 5056),
            new AspectRatioOption("3:2 (Landscape)", "3:2", 1264, 848, 2528, 1696, 5056, 3392),
            new AspectRatioOption("3:4 (Portrait)", "3:4", 896, 1200, 1792, 2400, 3584, 4800),
            new AspectRatioOption("4:3 (Landscape)", "4:3", 1200, 896, 2400, 1792, 4800, 3584),
            new AspectRatioOption("4:5 (Portrait)", "4:5", 928, 1152, 1856, 2304, 3712, 4608),
            new AspectRatioOption("5:4 (Landscape)", "5:4", 1152, 928, 2304, 1856, 4608, 3712),
            new AspectRatioOption("9:16 (Vertical)", "9:16", 768, 1376, 1536, 2752, 3072, 5504),
            new AspectRatioOption("16:9 (Widescreen)", "16:9", 1376, 768, 2752, 1536, 5504, 3072),
            new AspectRatioOption("21:9 (Ultrawide)", "21:9", 1584, 672, 3168, 1344, 6336, 2688)
        };

        // Properties - only save when value actually changes
        public List<StyleTemplate> CustomTemplates => customTemplates;
        public int SelectedAspectRatioIndex
        {
            get => selectedAspectRatioIndex;
            set { if (selectedAspectRatioIndex != value) { selectedAspectRatioIndex = value; SaveSettings(); } }
        }
        public int SelectedImageSizeIndex
        {
            get => selectedImageSizeIndex;
            set { if (selectedImageSizeIndex != value) { selectedImageSizeIndex = value; SaveSettings(); } }
        }
        public string OutputFolder
        {
            get => outputFolder;
            set { if (outputFolder != value) { outputFolder = value; SaveSettings(); } }
        }
        public bool AutoOpenImage
        {
            get => autoOpenImage;
            set { if (autoOpenImage != value) { autoOpenImage = value; SaveSettings(); } }
        }
        public bool KeepCaptureCamera
        {
            get => keepCaptureCamera;
            set { if (keepCaptureCamera != value) { keepCaptureCamera = value; SaveSettings(); } }
        }
        public string FileNamingPattern
        {
            get => fileNamingPattern;
            set { if (fileNamingPattern != value) { fileNamingPattern = value; SaveSettings(); } }
        }
        public bool SaveCapturedImage
        {
            get => saveCapturedImage;
            set { if (saveCapturedImage != value) { saveCapturedImage = value; SaveSettings(); } }
        }

        // API Key (stored in EditorPrefs for security)
        public static string ApiKey
        {
            get => EditorPrefs.GetString(API_KEY_PREF, "");
            set => EditorPrefs.SetString(API_KEY_PREF, value);
        }

        // Model Index (stored in EditorPrefs)
        public static int SelectedModelIndex
        {
            get => EditorPrefs.GetInt(MODEL_INDEX_PREF, 0);
            set => EditorPrefs.SetInt(MODEL_INDEX_PREF, value);
        }

        public static bool IsProModel => SelectedModelIndex == 0;
        public static string CurrentModelId => ModelIds[SelectedModelIndex];

        // Current resolution helpers
        public AspectRatioOption CurrentAspectRatio => AspectRatios[selectedAspectRatioIndex];
        public string CurrentImageSize => ImageSizeOptions[selectedImageSizeIndex];
        public (int width, int height) CurrentResolution => CurrentAspectRatio.GetResolution(CurrentImageSize);

        // Singleton instance
        private static UnityBananaSettings _instance;
        public static UnityBananaSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = LoadOrCreateSettings();
                }
                return _instance;
            }
        }

        private static UnityBananaSettings LoadOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<UnityBananaSettings>(SETTINGS_PATH);
            if (settings == null)
            {
                settings = CreateInstance<UnityBananaSettings>();

                // Ensure directory exists
                var directory = Path.GetDirectoryName(SETTINGS_PATH);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                AssetDatabase.CreateAsset(settings, SETTINGS_PATH);
                AssetDatabase.SaveAssets();
                Debug.Log("[UnityBanana] Created new settings asset at: " + SETTINGS_PATH);
            }
            return settings;
        }

        public void SaveSettings()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        public void AddCustomTemplate(StyleTemplate template)
        {
            template.isBuiltIn = false;
            customTemplates.Add(template);
            SaveSettings();
        }

        public void RemoveCustomTemplate(int index)
        {
            if (index >= 0 && index < customTemplates.Count)
            {
                customTemplates.RemoveAt(index);
                SaveSettings();
            }
        }

        public void UpdateCustomTemplate(int index, StyleTemplate template)
        {
            if (index >= 0 && index < customTemplates.Count)
            {
                template.isBuiltIn = false;
                customTemplates[index] = template;
                SaveSettings();
            }
        }

        // Get all templates (built-in + custom)
        public List<StyleTemplate> GetAllTemplates()
        {
            var allTemplates = new List<StyleTemplate>(StyleTemplates.BuiltInTemplates);
            allTemplates.AddRange(customTemplates);
            return allTemplates;
        }

        // Get output path (does not create directory - use EnsureOutputPathExists for that)
        public string GetOutputPath()
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot, outputFolder);
        }

        // Ensures output directory exists - call only when saving
        public string EnsureOutputPathExists()
        {
            var fullPath = GetOutputPath();
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
            return fullPath;
        }

        // Generate filename based on pattern
        public string GenerateFileName(string styleName)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = fileNamingPattern
                .Replace("{style}", SanitizeFileName(styleName))
                .Replace("{timestamp}", timestamp)
                .Replace("{date}", DateTime.Now.ToString("yyyyMMdd"))
                .Replace("{time}", DateTime.Now.ToString("HHmmss"));

            return fileName + ".png";
        }

        // Generate filename for captured image
        public string GenerateCaptureFileName()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"captured_{timestamp}.png";
        }

        private string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name.Replace(" ", "_").ToLower();
        }

        // Validate API key format (basic check)
        public static bool IsApiKeyValid(string key)
        {
            return !string.IsNullOrWhiteSpace(key) && key.Length >= 20;
        }
    }
}
