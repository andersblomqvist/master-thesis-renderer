using UnityEngine;
using UnityEditor;

public class WhiteNoiseGenerator : MonoBehaviour
{
    public ComputeShader computeShader;
    public int textureSize = 128;
    public int textureCount = 64;

    private RenderTexture[] renderTextures;

    public void GenerateTextures()
    {
        if (computeShader == null)
        {
            Debug.LogError("Compute Shader not assigned.");
            return;
        }

        renderTextures = new RenderTexture[textureCount];
        System.Random random = new System.Random(); // Ensures different seeds per session

        for (int i = 0; i < textureCount; i++)
        {
            RenderTexture rt = new RenderTexture(textureSize, textureSize, 0)
            {
                enableRandomWrite = true,
                format = RenderTextureFormat.ARGB32
            };
            rt.Create();

            int kernelHandle = computeShader.FindKernel("CSMain");
            computeShader.SetTexture(kernelHandle, "Result", rt);

            int seed = random.Next(); // Unique seed
            computeShader.SetInt("seed", seed);

            computeShader.Dispatch(kernelHandle, textureSize / 8, textureSize / 8, 1);

            SaveTextureAsAsset(rt, i);
            renderTextures[i] = rt;
        }

        Debug.Log("Generated 64 unique white noise textures.");
    }

    private void SaveTextureAsAsset(RenderTexture renderTexture, int index)
    {
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
        texture.Apply();
        RenderTexture.active = null;

        byte[] bytes = texture.EncodeToPNG();
        string path = $"Assets/WhiteNoiseTextures/WhiteNoise_{index}.png";

        System.IO.Directory.CreateDirectory("Assets/WhiteNoiseTextures");
        System.IO.File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);
    }
}
