using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

/// <summary>
/// Written with the help of ChatGPT-4-turbo
/// </summary>
public class TextureImportProcessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        var texturePaths = importedAssets
            .Where(path => path.EndsWith(".png") || path.EndsWith(".jpg"))
            .ToList();

        var textures = texturePaths
            .Select(path => EnsureTextureIsReadable(path))
            .Where(tex => tex != null)
            .ToList();

        if (textures.Count > 1)
        {
            string directory = Path.GetDirectoryName(texturePaths[0]);
            string sheetName = Path.GetFileNameWithoutExtension(texturePaths[0]) + "_spritesheet.png";
            string sheetPath = Path.Combine(directory, sheetName);

            if (EditorUtility.DisplayDialog("Combine Textures?", $"You imported {textures.Count} textures. Do you want to combine them into a spritesheet?", "Yes", "No"))
            {
                CreateSpriteSheet(textures, sheetPath);
                DeleteOriginalTextures(texturePaths);
            }
        }
    }

    static Texture2D EnsureTextureIsReadable(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && !importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static void CreateSpriteSheet(System.Collections.Generic.List<Texture2D> textures, string path)
    {
        int textureSize = textures[0].width; // Assuming all textures are the same size
        int count = textures.Count;
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(count));
        int sheetSize = gridSize * textureSize;

        Texture2D spritesheet = new Texture2D(sheetSize, sheetSize);
        Color[] emptyPixels = Enumerable.Repeat(Color.clear, sheetSize * sheetSize).ToArray();
        spritesheet.SetPixels(emptyPixels);
        
        // Sort textures by name length and alphabetically
        textures.Sort((a, b) => a.name.Length == b.name.Length ? a.name.CompareTo(b.name) : a.name.Length.CompareTo(b.name.Length));
 
        for (int i = 0; i < count; i++)
        {
            int x = i % gridSize * textureSize;
            int y = (count - 1 - i) / gridSize * textureSize;
            Texture2D tex = textures[i];
            spritesheet.SetPixels(x, y, textureSize, textureSize, tex.GetPixels());

            Debug.Log($"Added {tex.name} to spritesheet at ({x}, {y}) with index {i}");
        }

        spritesheet.Apply();

        File.WriteAllBytes(path, spritesheet.EncodeToPNG());
        AssetDatabase.Refresh();
        Debug.Log("Spritesheet created at " + path);
    }

    static void DeleteOriginalTextures(System.Collections.Generic.List<string> texturePaths)
    {
        foreach (string path in texturePaths)
        {
            AssetDatabase.DeleteAsset(path);
        }
        AssetDatabase.Refresh();
        Debug.Log("Original textures deleted.");
    }
}
