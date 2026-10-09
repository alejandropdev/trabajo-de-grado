using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleAnim : MonoBehaviour
{
    public bool position = true;
    public bool rotation = true;
    public bool scale = true;

    public float speed = 0.5f;
    public AnimationCurve curve;
    public Transform from;
    public Transform to;

    private TransformData startData;
    private TransformData targetData;

    // private Rigidbody rb;

    private float time;
    private float frame;

    private void Start()
    {
        // rb = gameObject.GetComponent<Rigidbody>();
        // if (!rb)
        // {
        //     rb = gameObject.AddComponent<Rigidbody>();
        //     rb.isKinematic = true;
        //     rb.useGravity = false;
        // }

        startData = new TransformData(from.localPosition, from.localScale, from.localRotation);
        targetData = new TransformData(to.localPosition, to.localScale, to.localRotation);
        // to.SetParent(transform.parent);
        // to.gameObject.SetActive(false);
    }
    private void Update()
    {
        if (!position && !rotation && !scale) { return; }
        if (!from || !to) { return; }
        time += Time.deltaTime;
        frame = curve.Evaluate(time * speed);
        // if (position) { rb.MovePosition( Vector3.MoveTowards(startData.position, targetData.position, frame)); }
        if (position) { from.localPosition = Vector3.Lerp(startData.position, targetData.position, frame); }
        if (rotation) { from.localRotation = Quaternion.Lerp(startData.rotation, targetData.rotation, frame); }
        if (scale) { from.localScale = Vector3.Lerp(startData.scale, targetData.scale, frame); }
    }

    private void OnDrawGizmos()
    {
        if (!from) { from = createPoint("From"); }
        if (!to) { to = createPoint("To"); }
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(from.position, 0.2f);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(to.position, 0.2f);
    }

    private Transform createPoint(string name)
    {
        Transform point = new GameObject(name).transform;
        point.SetParent(transform);
        setGlobalScale(point, Vector3.one);
        point.localPosition = Vector3.zero;
        return point;
    }

    private void setGlobalScale(Transform transform, Vector3 globalScale)
    {
        transform.localScale = Vector3.one;
        transform.localScale = new Vector3(globalScale.x / transform.lossyScale.x, globalScale.y / transform.lossyScale.y, globalScale.z / transform.lossyScale.z);
    }

    // private void createTarget()
    // {
    //     this.enabled = false;
    //     to = Instantiate(gameObject).transform;
    //     DestroyImmediate(to.GetComponent<SimpleAnim>());
    //     to.SetParent(transform);
    //     to.transform.localPosition = Vector3.zero;
    //     to.gameObject.name = "Target";
    //     this.enabled = true;
    // }

    [System.Serializable]
    public class TransformData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public TransformData(Vector3 position, Vector3 scale, Quaternion rotation)
        {
            this.position = position;
            this.scale = scale;
            this.rotation = rotation;
        }

    }
}
