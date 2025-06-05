using System.IO;
using UnityEngine;

public class DataCollector : MonoBehaviour
{
    const int MAX_FRAMES = 32;
    const string OUTPUT_PATH = "Assets/Data/";

    const TextureFormat TEXTURE_FORMAT = TextureFormat.R8;

    enum NoiseType
    {
        White = 1,
        Blue = 2,
        STBN = 3,
        FAST = 4,
        IGN = 5,
    }

    enum FilterType
    {
        None = 1,
        Gaussian = 2,
        Box3x3 = 3,
        Box5x5 = 4,
        Binom3x3 = 5,
        Binom5x5 = 6,
    }


    public GameObject volumeRenderer;
    NanoVolumeSceneSettings settings;

    Texture2DArray frames;
    bool isRunning = false;
    int frame = 0;
    int width;
    int height;

    [Header("Optinal")]
    public Texture2D groundTruth;

    void Start()
    {
        settings = volumeRenderer.GetComponent<NanoVolumeSceneSettings>();
        width = Screen.width;
        height = Screen.height;

        string path = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/'));
        Debug.Log($"Saving images to: {path + "/" + OUTPUT_PATH}");

        if (!Directory.Exists(path + "/" + OUTPUT_PATH))
        {
            Directory.CreateDirectory(path + "/" + OUTPUT_PATH);
        }
    }

    void Update()
    {
        if (!isRunning)
            return;

        if (frame == MAX_FRAMES)
        {
            SaveData();
            isRunning = false;
            return;
        }

        // Save the current frame to the array
        Texture2D currentFrame = GetCurrentCameraTexture();
        SaveFrameToArray(currentFrame, frame);

        if (groundTruth != null)
        {
            double rmse = CalculateRMSE(currentFrame, groundTruth);
            Debug.Log($"[{frame}] RMSE: {rmse}");
        }
        else
        {
            Debug.Log($"Frame {frame} saved to array");
        }

        frame++;
    }

    public void CollectData()
    {
        frames = new Texture2DArray(width, height, MAX_FRAMES, TEXTURE_FORMAT, false);

        PrepareExperiment();
    }

    double CalculateRMSE(Texture2D currentFrame, Texture2D groundTruth)
    {
        if (currentFrame.width != groundTruth.width || currentFrame.height != groundTruth.height)
        {
            Debug.LogError("Texture dimensions do not match for RMSE calculation. " +
                           $"Current: {currentFrame.width}x{currentFrame.height}, " +
                           $"Ground Truth: {groundTruth.width}x{groundTruth.height}");
            return -1;
        }

        Color[] currentPixels = currentFrame.GetPixels();
        Color[] groundTruthPixels = groundTruth.GetPixels();

        double sumSquaredError = 0.0;
        for (int i = 0; i < currentPixels.Length; i++)
        {
            float error = currentPixels[i].r - groundTruthPixels[i].r;
            sumSquaredError += error * error;
        }

        double meanSquaredError = sumSquaredError / currentPixels.Length;
        return Mathf.Sqrt((float)meanSquaredError);
    }

    void SaveData()
    {
        string scene = GetSceneName();
        string noise = GetNoiseNameByType(settings.ActiveNoiseType);
        string spatial = GetSpatialFilterName(settings.ActiveSpatialFilter);
        string temporal = GetTemporalFilterName();
        string file = $"{scene}_{noise}_{temporal}_{spatial}";
        Debug.Log("Experiment finished");

        Texture2D tex;
        byte[] imageBytes;
        for (int i = 0; i < MAX_FRAMES; i++)
        {
            string fileName;
            if (i < 10)
                fileName = $"{file}_0{i}.png";
            else
                fileName = $"{file}_{i}.png";
            string filePath = Path.Combine(OUTPUT_PATH, fileName);
            tex = new Texture2D(width, height, TEXTURE_FORMAT, false);
            tex.SetPixels(frames.GetPixels(i));
            imageBytes = tex.EncodeToPNG();
            File.WriteAllBytes(filePath, imageBytes);
        }

        // Save Ground Truth
        settings.ToggleGroundTruth();
        tex = GetCurrentCameraTexture();
        imageBytes = tex.EncodeToPNG();
        string groundTruthImagePath = Path.Combine(OUTPUT_PATH, $"{file}_GT.png");
        File.WriteAllBytes(groundTruthImagePath, imageBytes);
        settings.ToggleGroundTruth();

        Destroy(tex);
    }

    void PrepareExperiment()
    {
        frame = 0;
        isRunning = true;
        settings.ExperimentModeHold = false;
    }

    Texture2D GetCurrentCameraTexture()
    {
        RenderTexture renderTexture = new RenderTexture(Screen.width, Screen.height, 0);
        Texture2D texture = new Texture2D(Screen.width, Screen.height, TEXTURE_FORMAT, false);

        Debug.Log($"Saving camera texture for frame {frame}");
        //settings.EnableRenderPass = true;
        Camera.main.targetTexture = renderTexture;
        Camera.main.Render();
        //settings.EnableRenderPass = false;

        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        texture.Apply();

        Camera.main.targetTexture = null;
        RenderTexture.active = null;
        Destroy(renderTexture);

        return texture;
    }

    void SaveFrameToArray(Texture2D currentFrame, int frameIndex)
    {
        frames.SetPixels(currentFrame.GetPixels(), frameIndex);
        frames.Apply();
    }

    string GetTemporalFilterName()
    {
        if (settings.IsUsingUnityTAA)
            return "unity_taa";
        else
            return "ema";
    }

    string GetNoiseNameByType(int type)
    {
        switch (type)
        {
            case (int)NoiseType.White:
                return settings.whiteNoise.name;
            case (int)NoiseType.Blue:
                return settings.blueNoise.name;
            case (int)NoiseType.STBN:
                return settings.STBN.name;
            case (int)NoiseType.FAST:
                return settings.FAST.name;
            case (int)NoiseType.IGN:
                return "ign";
            default:
                return "error";
        }
    }

    string GetSpatialFilterName(int type)
    {
        switch (type)
        {
            case (int)FilterType.None:
                return "none";
            case (int)FilterType.Gaussian:
                return "gauss1_0";
            case (int)FilterType.Box3x3:
                return "box3x3";
            case (int)FilterType.Box5x5:
                return "box5x5";
            case (int)FilterType.Binom3x3:
                return "binom3x3";
            case (int)FilterType.Binom5x5:
                return "binom5x5";
            default:
                return "error";
        }
    }

    string GetSceneName()
    {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    }
}
