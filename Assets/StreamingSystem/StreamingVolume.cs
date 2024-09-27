using Eflatun.SceneReference;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class StreamingVolume : MonoBehaviour
{
    [SerializeField] List<SceneReference> RequiredScenes;
    BoxCollider LinkedCollider;

    void Awake()
    {
        LinkedCollider = GetComponent<BoxCollider>();
    }

    void Start()
    {
        StreamingManager.RegisterVolume(this);
    }

    void OnDestroy()
    {
        StreamingManager.DeregisterVolume(this);
    }

    public bool Contains(Vector3 InPosition)
    {
        return LinkedCollider.bounds.Contains(InPosition);
    }

    public void UpdateSceneList(List<string> InOutSceneList)
    {
        foreach(var SceneRef in RequiredScenes)
        {
            if (SceneRef.State != SceneReferenceState.Regular)
                continue;

            if (!InOutSceneList.Contains(SceneRef.Path))
                InOutSceneList.Add(SceneRef.Path);
        }
    }
}
