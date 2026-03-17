using UnityEngine;

namespace FlickThrowSystem.Execution
{
    public class BallController : MonoBehaviour
    {
        public Rigidbody ballRigidbody;
        public Transform startPosition;

        // Update is called once per frame
        void Update()
        {
           //if (Input.GetKeyDown(KeyCode.R))
           //{
           //    ballRigidbody.linearVelocity = Vector3.zero;
           //    ballRigidbody.angularVelocity = Vector3.zero;
           //
           //    GameObject g = ballRigidbody.gameObject;
           //    Vector3 sp = startPosition.position;
           //
           //    ballRigidbody.transform.position = startPosition.position;
           //}
        }
    }
}
