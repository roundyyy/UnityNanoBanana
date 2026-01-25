using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Roundy.UnityBanana
{
    public static class StyleTemplates
    {
        public static readonly StyleTemplate[] BuiltInTemplates = new[]
        {
            // Standard Set
            new StyleTemplate(
                "Empty",
                "",
                "",
                true
            ),
            // Standard Set
            new StyleTemplate(
                "Photorealistic",
                "Transform this scene into a photorealistic photograph with natural lighting, realistic textures, and cinematic depth of field.",
                "8K resolution, DSLR quality, soft shadows, volumetric lighting",
                true
            ),
            new StyleTemplate(
                "Cinematic",
                "Convert this scene to a cinematic movie still with dramatic lighting, film grain, and anamorphic lens effects.",
                "Hollywood production quality, color graded, 2.39:1 aspect feel",
                true
            ),
            new StyleTemplate(
                "Western Comic",
                "Transform this scene into a Western comic book panel with bold outlines, cel shading, and dynamic composition.",
                "Clean ink lines, halftone dots, vibrant colors, action lines, superhero comic style",
                true
            ),
            new StyleTemplate(
                "Manga",
                "Convert this scene into a Japanese manga-style illustration with characteristic screentones, expressive linework, and dynamic panel composition.",
                "Black and white, screentone shading, speed lines, dramatic expressions, Japanese comic aesthetic",
                true
            ),
            new StyleTemplate(
                "Manhwa",
                "Transform this scene into a Korean manhwa/webtoon style with clean digital coloring, soft shading, and modern aesthetic.",
                "Vertical scroll format feel, soft gradients, clean lines, modern digital art, webtoon aesthetic",
                true
            ),
            new StyleTemplate(
                "Graphic Novel",
                "Convert this scene into a graphic novel panel with detailed artwork, sophisticated coloring, and cinematic composition.",
                "Painted style, dramatic lighting, mature themes aesthetic, literary feel",
                true
            ),
            new StyleTemplate(
                "Anime",
                "Convert this scene to anime style with clean lines, expressive characters, and vibrant colors.",
                "Japanese animation style, soft gradients, detailed backgrounds",
                true
            ),
            new StyleTemplate(
                "Oil Painting",
                "Transform this scene into a classical oil painting with visible brushstrokes and rich colors.",
                "Impasto technique, Renaissance lighting, gallery quality",
                true
            ),
            new StyleTemplate(
                "Watercolor",
                "Convert this scene to a delicate watercolor painting with soft edges and translucent washes.",
                "Wet-on-wet technique, paper texture, flowing colors",
                true
            ),
            new StyleTemplate(
                "Pixel Art",
                "Transform this scene into retro pixel art with limited color palette and crisp pixels.",
                "16-bit style, dithering, nostalgic gaming aesthetic",
                true
            ),
            new StyleTemplate(
                "Pencil Sketch",
                "Convert this scene to a detailed pencil sketch with clean linework and subtle shading.",
                "Cross-hatching, architectural precision, hand-drawn feel, graphite texture",
                true
            ),
            new StyleTemplate(
                "Charcoal Drawing",
                "Transform this scene into a charcoal drawing with rich blacks, dramatic contrasts, and expressive strokes.",
                "Smudged edges, textured paper, high contrast, artistic study feel",
                true
            ),
            new StyleTemplate(
                "Ink Wash",
                "Convert this scene to an ink wash painting style with flowing brushstrokes and atmospheric gradients.",
                "Sumi-e inspired, minimalist, varying ink density, zen aesthetic",
                true
            ),
            new StyleTemplate(
                "Doodle Art",
                "Transform this scene into a playful doodle art style with loose hand-drawn lines and whimsical details.",
                "Colorful markers, thick pencil strokes, childlike charm, notebook aesthetic",
                true
            ),
            new StyleTemplate(
                "Blueprint",
                "Convert this scene into a technical blueprint drawing with precise lines and engineering aesthetics.",
                "White lines on blue background, measurement annotations, technical precision, architectural feel",
                true
            ),
            // Extended Set
            new StyleTemplate(
                "Noir",
                "Transform this scene into a film noir style with high contrast black and white, dramatic shadows, and moody atmosphere.",
                "1940s detective aesthetic, Venetian blinds shadows, cigarette smoke",
                true
            ),
            new StyleTemplate(
                "Steampunk",
                "Convert this scene to steampunk aesthetic with brass machinery, gears, Victorian-era technology, and industrial elements.",
                "Copper and brass tones, clockwork details, airship era",
                true
            ),
            new StyleTemplate(
                "Cyberpunk",
                "Transform this scene into a cyberpunk world with neon lights, rain-slicked streets, holographic advertisements, and high-tech dystopia.",
                "Blade Runner aesthetic, pink and cyan neons, urban decay",
                true
            ),
            new StyleTemplate(
                "Fantasy Art",
                "Convert this scene to epic fantasy art with magical lighting, mythical atmosphere, and painterly detail.",
                "Frank Frazetta inspired, dramatic composition, ethereal glow",
                true
            ),
            new StyleTemplate(
                "Studio Ghibli",
                "Transform this scene in the style of Studio Ghibli films with lush environments, soft colors, and whimsical charm.",
                "Miyazaki-inspired, detailed nature, warm nostalgic feeling",
                true
            ),
            new StyleTemplate(
                "Retro 80s",
                "Convert this scene to 1980s retro aesthetic with synthwave colors, chrome effects, and VHS nostalgia.",
                "Sunset gradients, grid lines, outrun aesthetic, neon pink and blue",
                true
            ),
            new StyleTemplate(
                "Product Shot",
                "Transform this scene into a high-end commercial product shot with studio lighting and clean background.",
                "Professional studio lighting, softbox effects, 4K detail, sharp focus, advertising standard",
                true
            ),
            new StyleTemplate(
                "3D Render",
                "Convert this scene into a high-fidelity 3D render with physically based rendering materials.",
                "Octane render style, raytracing, subsurface scattering, ambient occlusion, digital art",
                true
            ),
            new StyleTemplate(
                "Claymation",
                "Transform this scene into a claymation style with plasticine textures and stop-motion aesthetic.",
                "Aardman style, fingerprint textures, soft lighting, miniature feel, playful",
                true
            ),
            new StyleTemplate(
                "Low Poly",
                "Convert this scene into a low poly 3D art style with flat shading and geometric shapes.",
                "Minimalist, vibrant colors, sharp edges, game art aesthetic",
                true
            ),
            new StyleTemplate(
                "Origami",
                "Transform this scene into an origami paper craft style with folded paper textures.",
                "Paper texture, sharp creases, layered paper effects, soft shadows",
                true
            ),
            // Realism Set
            new StyleTemplate(
                "Real World",
                "Transform this image as if it was captured in a real-world location with natural photography aesthetics.",
                "Natural lighting, authentic textures, candid photography feel, real environment",
                true
            ),
            new StyleTemplate(
                "Documentary",
                "Convert this scene into a documentary-style photograph with authentic, unposed composition and natural lighting.",
                "Photojournalistic feel, available light, candid moments, raw authenticity",
                true
            ),
            new StyleTemplate(
                "Nature Photography",
                "Transform this scene into a professional nature photograph with stunning natural beauty and wildlife documentary quality.",
                "National Geographic style, golden hour lighting, shallow depth of field, environmental portrait",
                true
            )
        };

        /// <summary>
        /// Builds the final prompt by combining style template with reference image instructions and user input.
        /// </summary>
        public static string BuildFinalPrompt(
            StyleTemplate template,
            bool hasCharacterReference,
            bool hasStyleReference,
            int objectReferenceCount,
            int humanReferenceCount,
            string additionalInstructions)
        {
            var sb = new StringBuilder();

            bool hasAnyReference = hasCharacterReference || hasStyleReference || objectReferenceCount > 0 || humanReferenceCount > 0;
            bool hasBasePrompt = !string.IsNullOrWhiteSpace(template.basePrompt);

            // Always start with a clear instruction about the main scene image
            if (hasBasePrompt)
            {
                // Template has its own transformation instruction (e.g., "Transform this scene into...")
                sb.AppendLine(template.basePrompt);
            }
            else if (hasStyleReference)
            {
                // No template prompt but has style reference - use style transfer instruction
                sb.AppendLine("Transform the first image (the captured scene) using the artistic style and visual aesthetic from the style reference image. Preserve the composition and content of the scene while applying the style.");
            }
            else if (hasAnyReference)
            {
                // Has other references but no style - basic transformation
                sb.AppendLine("Transform the first image (the captured scene) while incorporating the provided reference images.");
            }

            // Reference image instructions with clear positioning
            if (hasCharacterReference || humanReferenceCount > 0)
            {
                sb.AppendLine("Preserve the exact facial features, identity, and appearance from the character/human reference image(s) without alteration. The face must remain 100% identical to the reference.");
            }

            // Only add style instruction if we have a base prompt (otherwise it's already covered above)
            if (hasStyleReference && hasBasePrompt)
            {
                sb.AppendLine("Apply the artistic style and visual aesthetic shown in the style reference image to the scene.");
            }

            if (objectReferenceCount > 0)
            {
                sb.AppendLine($"Include the object(s) from the {objectReferenceCount} object reference image(s) with high-fidelity details, naturally integrated into the scene.");
            }

            // Style modifiers
            if (!string.IsNullOrWhiteSpace(template.styleModifiers))
            {
                sb.AppendLine(template.styleModifiers);
            }

            // Additional user instructions
            if (!string.IsNullOrWhiteSpace(additionalInstructions))
            {
                sb.AppendLine();
                sb.AppendLine("Additional instructions: " + additionalInstructions.Trim());
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Gets the display names of all templates for dropdown display.
        /// </summary>
        public static string[] GetTemplateNames(List<StyleTemplate> allTemplates)
        {
            var names = new string[allTemplates.Count];
            for (int i = 0; i < allTemplates.Count; i++)
            {
                var template = allTemplates[i];
                names[i] = template.isBuiltIn ? template.name : $"{template.name} (Custom)";
            }
            return names;
        }

        /// <summary>
        /// Creates a custom template based on an existing one.
        /// </summary>
        public static StyleTemplate CreateCustomFromExisting(StyleTemplate source, string newName)
        {
            return new StyleTemplate(
                newName,
                source.basePrompt,
                source.styleModifiers,
                false
            );
        }
    }
}
