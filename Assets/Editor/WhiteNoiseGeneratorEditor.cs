using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WhiteNoiseGenerator))]
public class WhiteNoiseGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WhiteNoiseGenerator generator = (WhiteNoiseGenerator)target;
        if (GUILayout.Button("Generate White Noise Textures"))
        {
            generator.GenerateTextures();
        }
    }
}
