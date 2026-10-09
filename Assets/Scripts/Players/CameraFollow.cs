using UnityEngine;

namespace CyberExtraction.Players
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.08f;

        Vector3 velocity;

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 goal = new Vector3(target.position.x, target.position.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
        }

        public void SetTarget(Transform t) { target = t; }
    }
}
