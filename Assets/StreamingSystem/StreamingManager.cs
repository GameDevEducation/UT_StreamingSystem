using Eflatun.SceneReference;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StreamingManager : MonoBehaviour
{
    class PendingUnloadInfo : IEquatable<PendingUnloadInfo>
    {
        public string ScenePath;
        public float TimeRemaining;

        public override bool Equals(object obj)
        {
            return Equals(obj as PendingUnloadInfo);
        }

        public bool Equals(PendingUnloadInfo other)
        {
            return other is not null &&
                   ScenePath == other.ScenePath;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ScenePath);
        }

        public static bool operator ==(PendingUnloadInfo left, PendingUnloadInfo right)
        {
            return EqualityComparer<PendingUnloadInfo>.Default.Equals(left, right);
        }

        public static bool operator !=(PendingUnloadInfo left, PendingUnloadInfo right)
        {
            return !(left == right);
        }
    }

    [SerializeField] List<SceneReference> AlwaysLoadedScenes;
    [SerializeField] float CleanupDelay = 0.1f;
    [SerializeField] float UnloadDelay = 5.0f;

    public static StreamingManager Instance { get; private set; } = null;

    List<StreamingPOVSource> POVSources = new();
    List<StreamingVolume> Volumes = new();

    List<string> CurrentScenes = new();
    List<PendingUnloadInfo> PendingUnloads = new();

    float LastDirtiedTime = -1.0f;

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError($"Found duplicate StreamingManager on {gameObject.name}");
            Destroy(gameObject);
        }

        Instance = this;
    }

    void Start()
    {
        // load always loaded scenes
        foreach(var SceneRef in AlwaysLoadedScenes)
        {
            if (SceneRef.State != SceneReferenceState.Regular)
                continue;

            SceneManager.LoadSceneAsync(SceneRef.Path, LoadSceneMode.Additive);
        }

        Cleanup();
    }

    // Update is called once per frame
    void Update()
    {
        // process any pending unloads
        foreach(var UnloadInfo in PendingUnloads)
        {
            UnloadInfo.TimeRemaining -= Time.deltaTime;

            if (UnloadInfo.TimeRemaining <= 0)
            {
                SceneManager.UnloadSceneAsync(UnloadInfo.ScenePath, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects).completed += (InUnloadOp) =>
                {
                    Resources.UnloadUnusedAssets();
                };
            }
        }

        // clear any unloads that were initiated
        PendingUnloads.RemoveAll((InUnloadInfo) =>
        {
            return InUnloadInfo.TimeRemaining <= 0;
        });

        bool bCleanupNeeded = (LastDirtiedTime >= 0f) && ((LastDirtiedTime + CleanupDelay) <= Time.time);

        if (bCleanupNeeded)
            Cleanup();
        else if (LastDirtiedTime < 0f)
        {
            // check if any sources are reporting dirty
            foreach (var POVSource in POVSources)
            {
                if ((POVSource != null) && POVSource.IsDirty)
                {
                    LastDirtiedTime = Time.time;
                    break;
                }
            }
        }
    }

    void Cleanup()
    {
        List<string> ScenesRequired = new();

        // build up required scenes
        foreach(var StreamingVol in Volumes)
        {
            bool bPOVSourceInside = false;

            // any POV present in volume?
            foreach(var POVSource in POVSources)
            {
                if (StreamingVol.Contains(POVSource.Position))
                {
                    bPOVSourceInside = true;
                    break;
                }
            }

            if (bPOVSourceInside)
                StreamingVol.UpdateSceneList(ScenesRequired);
        }

        // clear any invalid pending unloads
        PendingUnloads.RemoveAll((InUnloadInfo) =>
        {
            if (ScenesRequired.Contains(InUnloadInfo.ScenePath))
            {
                CurrentScenes.Add(InUnloadInfo.ScenePath);
                return true;
            }

            return false;
        });

        // unload any old scenes
        foreach (var ScenePath in CurrentScenes)
        {
            if (!ScenesRequired.Contains(ScenePath))
            {
                var UnloadInfo = new PendingUnloadInfo() { ScenePath = ScenePath, TimeRemaining = UnloadDelay };

                if (!PendingUnloads.Contains(UnloadInfo))
                    PendingUnloads.Add(UnloadInfo);
            }
        }

        // load any new scene
        foreach(var ScenePath in ScenesRequired)
        {
            if (!CurrentScenes.Contains(ScenePath))
                SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Additive);
        }

        CurrentScenes = ScenesRequired;

        // mark sources as clean
        foreach (var POVSource in POVSources)
            POVSource.MarkClean();

        LastDirtiedTime = -1f;
    }

    public static void RegisterVolume(StreamingVolume InVolume)
    {
        if (Instance == null)
            return;

        Instance.RegisterVolume_Internal(InVolume);
    }

    public static void DeregisterVolume(StreamingVolume InVolume)
    {
        if (Instance == null)
            return;

        Instance.DeregisterVolume_Internal(InVolume);
    }

    public static void RegisterPOV(StreamingPOVSource InSource)
    {
        if (Instance == null)
            return;

        Instance.RegisterPOV_Internal(InSource);
    }

    public static void DeregisterPOV(StreamingPOVSource InSource)
    {
        if (Instance == null)
            return;

        Instance.DeregisterPOV_Internal(InSource);
    }

    void RegisterVolume_Internal(StreamingVolume InVolume)
    {
        if (!Volumes.Contains(InVolume))
            Volumes.Add(InVolume);
    }

    void DeregisterVolume_Internal(StreamingVolume InVolume)
    {
        Volumes.Remove(InVolume);
    }

    void RegisterPOV_Internal(StreamingPOVSource InSource)
    {
        if (!POVSources.Contains(InSource))
            POVSources.Add(InSource);
    }

    void DeregisterPOV_Internal(StreamingPOVSource InSource)
    {
        POVSources.Remove(InSource);
    }
}
