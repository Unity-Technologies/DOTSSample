using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine.Rendering;
using UnityEditor.Rendering;
using UnityEngine;

public class ShaderVariantStripper : IPreprocessShaders
{
    ShaderKeyword kLightmapOn;
    ShaderKeyword kUseDirectionalLightmap;
    ShaderKeyword kShadowMaskMode;

    public ShaderVariantStripper()
    {
        kLightmapOn = new ShaderKeyword("LIGHTMAP_ON");
        kUseDirectionalLightmap = new ShaderKeyword("DIRLIGHTMAP_COMBINED");
        kShadowMaskMode = new ShaderKeyword("SHADOWS_SHADOWMASK");
    }

    public int callbackOrder { get { return 1; } }

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> inputData)
    {
        bool shouldStripNonShadowmaskVariant = false;

        // We always use shadow-mask
        // Need to check if this is really okay:
        // With no mixed light around, HDRP may set SHADOWS_SHADOWMASK off and not allocate resource needed by shader with SHADOWS_SHADOWMASK turned on
        // However as we test nothing looks broken, let's keep this to make shader compile faster
        // For final build/release, let's not strip SHADOWS_SHADOWMASK off shaders to be on the safe side
        if (shader.name == "HDRP/Lit"
            ||
            shader.name == "HDRP/LayeredLit"
            ||
            shader.name == "HDRP/LitTessellation"
            ||
            shader.name == "HDRP/LayeredLitTessellation")
        {
            bool isGBufferPass = snippet.passName == "GBuffer";
            bool isTransparentBackfacePass = snippet.passName == "TransparentBackface";
            bool isForwardPass = snippet.passName == "Forward";
            if (isGBufferPass || isTransparentBackfacePass || isForwardPass)
            {
                shouldStripNonShadowmaskVariant = true;
            }
        }

        for (int i = 0; i < inputData.Count; ++i)
        {
            ShaderCompilerData input = inputData[i];
            if (ShouldStripShader(input, shouldStripNonShadowmaskVariant))
            {
                inputData.RemoveAt(i);
                i--;
            }
        }
    }

    bool ShouldStripShader(ShaderCompilerData inputData, bool shouldStripNonShadowmaskVariant)
    {
        bool lightMapOn = inputData.shaderKeywordSet.IsEnabled(kLightmapOn);
        bool useDirLightmap = inputData.shaderKeywordSet.IsEnabled(kUseDirectionalLightmap);
        if ((lightMapOn && !useDirLightmap)
            ||
            (!lightMapOn && useDirLightmap))
        {
            // Only meaningful combinations are:
            // 1 - lightmap on, using directional light map
            // 2 - lightmap not on, does not matter what type of light map you use
            // Any other combination of keywords should be stripped
            return true;
        }

        if (shouldStripNonShadowmaskVariant)
        {
            if (!inputData.shaderKeywordSet.IsEnabled(kShadowMaskMode))
            {
                return true;
            }
        }

        return false;
    }
}
