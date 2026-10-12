using UnityEngine;

namespace CyberExtraction.Players
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.08f;

        Vector3 velocity;
        bool bounded;
        Rect bounds;
        Camera viewCamera;

        void Awake() { viewCamera=GetComponent<Camera>(); }

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 goal = new Vector3(target.position.x, target.position.y, transform.position.z);
            goal=ClampToBounds(goal);
            transform.position = ClampToBounds(Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime));
        }

        public void SetTarget(Transform t) { target = t; bounded=false; velocity=Vector3.zero; }
        public void SetBounds(Rect value) { bounds=value; bounded=true; transform.position=ClampToBounds(transform.position); }

        Vector3 ClampToBounds(Vector3 value)
        {
            if(!bounded || viewCamera==null || !viewCamera.orthographic) return value;
            float halfHeight=viewCamera.orthographicSize, halfWidth=halfHeight*viewCamera.aspect;
            value.x=bounds.width>halfWidth*2 ? Mathf.Clamp(value.x,bounds.xMin+halfWidth,bounds.xMax-halfWidth) : bounds.center.x;
            value.y=bounds.height>halfHeight*2 ? Mathf.Clamp(value.y,bounds.yMin+halfHeight,bounds.yMax-halfHeight) : bounds.center.y;
            return value;
        }
    }
}
