using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Roundy.UnityBanana
{
    public class GenerationResult
    {
        public bool Success { get; set; }
        public Texture2D GeneratedImage { get; set; }
        public string ResponseText { get; set; }
        public string ErrorMessage { get; set; }
        public string RawResponse { get; set; }
    }

    [Serializable]
    public class GeminiRequest
    {
        public Content[] contents;
        public GenerationConfig generationConfig;
    }

    [Serializable]
    public class Content
    {
        public Part[] parts;
    }

    [Serializable]
    public class Part
    {
        public string text;
        public InlineData inline_data;
    }

    [Serializable]
    public class InlineData
    {
        public string mime_type;
        public string data;
    }

    [Serializable]
    public class GenerationConfig
    {
        public string[] responseModalities;
        public ImageConfig imageConfig;
    }

    [Serializable]
    public class ImageConfig
    {
        public string aspectRatio;
        public string imageSize;
    }

    [Serializable]
    public class GeminiResponse
    {
        public Candidate[] candidates;
        public PromptFeedback promptFeedback;
        public UsageMetadata usageMetadata;
        public Error error;
    }

    [Serializable]
    public class Candidate
    {
        public ResponseContent content;
        public string finishReason;
    }

    [Serializable]
    public class ResponseContent
    {
        public ResponsePart[] parts;
        public string role;
    }

    [Serializable]
    public class ResponsePart
    {
        public string text;
        public ResponseInlineData inlineData;  // API returns camelCase
        public bool thought;
    }

    [Serializable]
    public class ResponseInlineData
    {
        public string mimeType;  // API returns camelCase
        public string data;
    }

    [Serializable]
    public class PromptFeedback
    {
        public string blockReason;
        public SafetyRating[] safetyRatings;
    }

    [Serializable]
    public class SafetyRating
    {
        public string category;
        public string probability;
    }

    [Serializable]
    public class UsageMetadata
    {
        public int promptTokenCount;
        public int candidatesTokenCount;
        public int totalTokenCount;
    }

    [Serializable]
    public class Error
    {
        public int code;
        public string message;
        public string status;
    }

    public static class NanoBananaAPI
    {
        private const string BASE_URL = "https://generativelanguage.googleapis.com/v1beta/models/";

        public static event Action<float, string> OnProgressUpdate;

        /// <summary>
        /// Generates an image using the Gemini API.
        /// </summary>
        public static IEnumerator GenerateImageCoroutine(
            Texture2D sceneCapture,
            string prompt,
            string aspectRatio,
            string imageSize,
            Texture2D characterReference,
            Texture2D styleReference,
            List<Texture2D> objectReferences,
            List<Texture2D> humanReferences,
            Action<GenerationResult> onComplete)
        {
            var result = new GenerationResult();

            // Validate API key
            var apiKey = UnityBananaSettings.ApiKey;
            if (!UnityBananaSettings.IsApiKeyValid(apiKey))
            {
                result.Success = false;
                result.ErrorMessage = "Invalid or missing API key. Please configure your API key in the Settings tab.";
                onComplete?.Invoke(result);
                yield break;
            }

            ReportProgress(0.1f, "Building request...");

            // Build the request
            string modelId = UnityBananaSettings.CurrentModelId;
            string url = $"{BASE_URL}{modelId}:generateContent";

            string requestJson;
            try
            {
                requestJson = BuildRequestJson(
                    sceneCapture, prompt, aspectRatio, imageSize,
                    characterReference, styleReference, objectReferences, humanReferences);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Failed to build request: {ex.Message}";
                onComplete?.Invoke(result);
                yield break;
            }

            ReportProgress(0.2f, "Sending to Gemini API...");

            // Create web request
            using (var request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(requestJson);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("x-goog-api-key", apiKey);
                request.timeout = 300; // 5 minute timeout for image generation

                var operation = request.SendWebRequest();

                // Poll for completion
                while (!operation.isDone)
                {
                    ReportProgress(0.2f + operation.progress * 0.6f, "Generating image...");
                    yield return null;
                }

                ReportProgress(0.85f, "Processing response...");

                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    result.Success = false;
                    result.RawResponse = request.downloadHandler.text;

                    // Log detailed error info for debugging
                    Debug.LogError($"[UnityBanana] Request failed - Result: {request.result}, HTTP Code: {request.responseCode}, Error: {request.error}");
                    Debug.LogError($"[UnityBanana] URL: {url}");
                    Debug.LogError($"[UnityBanana] Response body: {request.downloadHandler.text}");

                    // Try to parse error response
                    try
                    {
                        var errorResponse = JsonUtility.FromJson<GeminiResponse>(request.downloadHandler.text);
                        if (errorResponse?.error != null && !string.IsNullOrEmpty(errorResponse.error.message))
                        {
                            result.ErrorMessage = $"API Error ({errorResponse.error.code}): {errorResponse.error.message}";
                        }
                        else if (errorResponse?.promptFeedback?.blockReason != null)
                        {
                            result.ErrorMessage = $"Content blocked: {errorResponse.promptFeedback.blockReason}. Please modify your prompt or scene.";
                        }
                        else
                        {
                            result.ErrorMessage = $"Request failed (HTTP {request.responseCode}): {request.error}";
                        }
                    }
                    catch
                    {
                        result.ErrorMessage = $"Request failed (HTTP {request.responseCode}): {request.error}\n{request.downloadHandler.text}";
                    }
                }
                else
                {
                    // Parse successful response
                    result.RawResponse = request.downloadHandler.text;

                    // Log the raw response for debugging
                    Debug.Log($"[UnityBanana] HTTP {request.responseCode} - Raw response length: {request.downloadHandler.text?.Length ?? 0}");
                    Debug.Log($"[UnityBanana] Raw response (first 2000 chars): {request.downloadHandler.text?.Substring(0, Math.Min(2000, request.downloadHandler.text?.Length ?? 0))}");

                    try
                    {
                        var response = JsonUtility.FromJson<GeminiResponse>(request.downloadHandler.text);
                        ProcessResponse(response, result);
                    }
                    catch (Exception ex)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Failed to parse response: {ex.Message}";
                    }
                }
            }

            ReportProgress(1f, "Complete!");
            onComplete?.Invoke(result);
        }

        private static string BuildRequestJson(
            Texture2D sceneCapture,
            string prompt,
            string aspectRatio,
            string imageSize,
            Texture2D characterReference,
            Texture2D styleReference,
            List<Texture2D> objectReferences,
            List<Texture2D> humanReferences)
        {
            var parts = new List<string>();

            // Add prompt first
            parts.Add($"{{\"text\": {EscapeJsonString(prompt)}}}");

            // Add scene capture (main image to transform)
            var sceneBase64 = TextureToBase64(sceneCapture);
            parts.Add($"{{\"inline_data\": {{\"mime_type\": \"image/png\", \"data\": \"{sceneBase64}\"}}}}");

            // Add character reference if provided
            if (characterReference != null)
            {
                var charBase64 = TextureToBase64(characterReference);
                parts.Add($"{{\"inline_data\": {{\"mime_type\": \"image/png\", \"data\": \"{charBase64}\"}}}}");
            }

            // Add style reference if provided
            if (styleReference != null)
            {
                var styleBase64 = TextureToBase64(styleReference);
                parts.Add($"{{\"inline_data\": {{\"mime_type\": \"image/png\", \"data\": \"{styleBase64}\"}}}}");
            }

            // Add object references
            if (objectReferences != null)
            {
                foreach (var objRef in objectReferences)
                {
                    if (objRef != null)
                    {
                        var objBase64 = TextureToBase64(objRef);
                        parts.Add($"{{\"inline_data\": {{\"mime_type\": \"image/png\", \"data\": \"{objBase64}\"}}}}");
                    }
                }
            }

            // Add human references
            if (humanReferences != null)
            {
                foreach (var humanRef in humanReferences)
                {
                    if (humanRef != null)
                    {
                        var humanBase64 = TextureToBase64(humanRef);
                        parts.Add($"{{\"inline_data\": {{\"mime_type\": \"image/png\", \"data\": \"{humanBase64}\"}}}}");
                    }
                }
            }

            var partsJson = string.Join(",\n            ", parts);

            // Build generation config
            var imageConfigJson = $"\"aspectRatio\": \"{aspectRatio}\"";
            if (UnityBananaSettings.IsProModel && !string.IsNullOrEmpty(imageSize))
            {
                imageConfigJson += $", \"imageSize\": \"{imageSize}\"";
            }

            var json = $@"{{
    ""contents"": [{{
        ""parts"": [
            {partsJson}
        ]
    }}],
    ""generationConfig"": {{
        ""responseModalities"": [""TEXT"", ""IMAGE""],
        ""imageConfig"": {{
            {imageConfigJson}
        }}
    }}
}}";

            return json;
        }

        private static void ProcessResponse(GeminiResponse response, GenerationResult result)
        {
            if (response == null)
            {
                result.Success = false;
                result.ErrorMessage = "Empty response from API";
                return;
            }

            // Check if there's an actual error (Unity's JsonUtility creates default objects, so check for meaningful content)
            if (response.error != null && (response.error.code != 0 || !string.IsNullOrEmpty(response.error.message)))
            {
                result.Success = false;
                result.ErrorMessage = $"API Error ({response.error.code}): {response.error.message}";
                return;
            }

            if (response.promptFeedback?.blockReason != null)
            {
                result.Success = false;
                result.ErrorMessage = $"Content blocked: {response.promptFeedback.blockReason}";
                return;
            }

            if (response.candidates == null || response.candidates.Length == 0)
            {
                result.Success = false;
                result.ErrorMessage = "No candidates in response";
                return;
            }

            var candidate = response.candidates[0];
            if (candidate.content?.parts == null)
            {
                result.Success = false;
                result.ErrorMessage = "No content parts in response";
                return;
            }

            // Find the generated image (skip thought images)
            Texture2D generatedImage = null;
            var textParts = new List<string>();

            foreach (var part in candidate.content.parts)
            {
                // Skip thought parts (interim images)
                if (part.thought)
                    continue;

                if (part.inlineData != null && !string.IsNullOrEmpty(part.inlineData.data))
                {
                    try
                    {
                        byte[] imageBytes = Convert.FromBase64String(part.inlineData.data);
                        var texture = new Texture2D(2, 2);
                        if (texture.LoadImage(imageBytes))
                        {
                            generatedImage = texture;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[UnityBanana] Failed to decode image: {ex.Message}");
                    }
                }
                else if (!string.IsNullOrEmpty(part.text))
                {
                    textParts.Add(part.text);
                }
            }

            if (generatedImage != null)
            {
                result.Success = true;
                result.GeneratedImage = generatedImage;
                result.ResponseText = string.Join("\n", textParts);
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = "No image found in response";
                result.ResponseText = string.Join("\n", textParts);
            }
        }

        private static string TextureToBase64(Texture2D texture)
        {
            // Ensure texture is readable
            Texture2D readableTexture = texture;
            if (!texture.isReadable)
            {
                // Create a readable copy
                RenderTexture tmp = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(texture, tmp);
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = tmp;

                readableTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                readableTexture.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                readableTexture.Apply();

                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(tmp);
            }

            byte[] pngData = readableTexture.EncodeToPNG();

            if (readableTexture != texture)
            {
                UnityEngine.Object.DestroyImmediate(readableTexture);
            }

            return Convert.ToBase64String(pngData);
        }

        private static string EscapeJsonString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "\"\"";

            var sb = new StringBuilder();
            sb.Append('"');
            foreach (char c in input)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ')
                        {
                            sb.Append($"\\u{(int)c:X4}");
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        private static void ReportProgress(float progress, string message)
        {
            OnProgressUpdate?.Invoke(progress, message);
        }

        /// <summary>
        /// Tests if the API key is valid by making a minimal request.
        /// </summary>
        public static IEnumerator ValidateApiKeyCoroutine(string apiKey, Action<bool, string> onComplete)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                onComplete?.Invoke(false, "API key is empty");
                yield break;
            }

            // Use a simple models list endpoint to validate
            string url = "https://generativelanguage.googleapis.com/v1beta/models";

            using (var request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("x-goog-api-key", apiKey);
                request.timeout = 30;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    onComplete?.Invoke(true, "API key is valid");
                }
                else
                {
                    if (request.responseCode == 401 || request.responseCode == 403)
                    {
                        onComplete?.Invoke(false, "Invalid API key");
                    }
                    else
                    {
                        onComplete?.Invoke(false, $"Validation failed: {request.error}");
                    }
                }
            }
        }
    }
}
