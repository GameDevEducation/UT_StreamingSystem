using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StreamingPOVSource : MonoBehaviour
{
    [SerializeField] float DistanceToMoveToBecomeDirty = 1.0f;

    public bool IsDirty { get; private set; } = false;
    public Vector3 Position => transform.position;

    float ThresholdDistanceSq;
    Vector3 LastPosition;

    // Start is called before the first frame update
    void Start()
    {
        LastPosition = transform.position;
        ThresholdDistanceSq = DistanceToMoveToBecomeDirty * DistanceToMoveToBecomeDirty;

        StreamingManager.RegisterPOV(this);
    }

    void OnDestroy()
    {
        StreamingManager.DeregisterPOV(this);
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsDirty && ((LastPosition - Position).sqrMagnitude >= ThresholdDistanceSq))
        {
            LastPosition = Position;
            IsDirty = true;
        }
    }

    public void MarkClean()
    {
        IsDirty = false;
        LastPosition = Position;
    }
}
