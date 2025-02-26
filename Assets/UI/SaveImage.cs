using System.Text;
using System.IO;
using UnityEngine;

public class SaveImage : MonoBehaviour
{
    public const string OUTPUT_PATH = "Assets/Data/";

    public NanoVolumeSceneSettings settings;

    string path;

    void Start()
    {
        path = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/'));
        Debug.Log($"Saving images to: {path + "/" + OUTPUT_PATH}");

        if (!Directory.Exists(path + "/" + OUTPUT_PATH))
        {
            Directory.CreateDirectory(path + "/" + OUTPUT_PATH);
        }
    }

    public void SaveCurrentFrame()
    {
        string currentTime = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string vdbName = GetVDBName();

        // Save the image as PNG
        Texture2D sample = GetCurrentCameraTexture();
        byte[] imageBytes = sample.EncodeToPNG();
        string experimentImagePath = Path.Combine(OUTPUT_PATH, vdbName + "_" + currentTime + ".png");
        File.WriteAllBytes(experimentImagePath, imageBytes);

        // Also save corresponding ground truth
        settings.ToggleGroundTruth();
        Texture2D groundTruth = GetCurrentCameraTexture();
        imageBytes = groundTruth.EncodeToPNG();
        string groundTruthImagePath = Path.Combine(OUTPUT_PATH, vdbName + "_" + currentTime + "_GT" + ".png");
        File.WriteAllBytes(groundTruthImagePath, imageBytes);
        settings.ToggleGroundTruth();

        float rmse = ComputeRMSE(sample, groundTruth);

        // Each saved image will have a text file with relevant data
        string metaData = GetSceneProperties(vdbName, rmse);
        string metaPath = Path.Combine(OUTPUT_PATH, vdbName + "_" + currentTime + ".txt");
        File.WriteAllText(metaPath, metaData);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.ImportAsset(metaPath);
        UnityEditor.AssetDatabase.ImportAsset(experimentImagePath);
        UnityEditor.AssetDatabase.ImportAsset(groundTruthImagePath);
        UnityEditor.AssetDatabase.Refresh();
#endif
        // Clean up
        Destroy(sample);
        Destroy(groundTruth);

        Debug.Log($"Frame saved to: {experimentImagePath}");
    }

    Texture2D GetCurrentCameraTexture()
    {
        RenderTexture renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
        Texture2D texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

        Camera.main.targetTexture = renderTexture;
        Camera.main.Render();

        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        texture.Apply();

        Camera.main.targetTexture = null;
        RenderTexture.active = null;
        Destroy(renderTexture);

        return texture;
    }

    float ComputeRMSE(Texture2D sample, Texture2D groundTruth)
    {
        int width = sample.width;
        int height = sample.height;

        Color[] pixels1 = sample.GetPixels();
        Color[] pixels2 = groundTruth.GetPixels();

        float errorSum = 0f;
        for (int i = 0; i < pixels1.Length; i++)
        {
            float rDiff = pixels1[i].r - pixels2[i].r;
            float gDiff = pixels1[i].g - pixels2[i].g;
            float bDiff = pixels1[i].b - pixels2[i].b;

            float diffSquared = rDiff * rDiff + gDiff * gDiff + bDiff * bDiff;
            errorSum += diffSquared;
        }

        float mse = errorSum / (width * height);
        float rmse = Mathf.Sqrt(mse);
        return rmse;
    }

    string GetSceneProperties(string vdbName, float rmse)
    {
        Vector3 sunRotation = settings.sun.transform.rotation.eulerAngles;
        Vector3 cameraPosition = Camera.main.transform.position;
        Vector3 cameraRotation = Camera.main.transform.rotation.eulerAngles;

        float density = settings.activeAsset.density;
        int steps = settings.activeAsset.lightStepsSamples;
        int noiseType = settings.ActiveNoiseType;
        int spatialType = settings.ActiveSpatialFilter;
        int temporal = settings.TemporalFiltering ? 1 : 0;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"name,{vdbName}");
        sb.AppendLine($"srot,{FloatToString(sunRotation.x)},{FloatToString(sunRotation.y)},{FloatToString(sunRotation.z)}");
        sb.AppendLine($"cpos,{FloatToString(cameraPosition.x)},{FloatToString(cameraPosition.y)},{FloatToString(cameraPosition.z)}");
        sb.AppendLine($"crot,{FloatToString(cameraRotation.x)},{FloatToString(cameraRotation.y)},{FloatToString(cameraRotation.z)}");
        sb.AppendLine($"density,{FloatToString(density)}");
        sb.AppendLine($"steps,{steps}");
        sb.AppendLine($"noise,{noiseType}");
        sb.AppendLine($"spatial,{spatialType}");
        sb.AppendLine($"temporal,{temporal}");
        sb.AppendLine($"rmse,{FloatToString(rmse)}");

        return sb.ToString();
    }

    string FloatToString(float value)
    {
        return value.ToString("0.0000").Replace(',', '.');
    }

    string GetVDBName()
    {
        // "Assets/wdas_cloud_half.nvdb" -> "wdas_cloud_half"
        string fileName = settings.activeAsset.volumePath;
        fileName = fileName.Substring(fileName.LastIndexOf('/') + 1);
        fileName = fileName.Substring(0, fileName.LastIndexOf('.'));
        return fileName;
    }
}
