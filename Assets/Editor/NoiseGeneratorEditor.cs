using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NoiseGenerator))]
public class NoiseGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        NoiseGenerator generator = (NoiseGenerator)target;
        if (GUILayout.Button("Generate White Noise Textures"))
        {
            generator.GenerateTextures();
        }
    }
}
