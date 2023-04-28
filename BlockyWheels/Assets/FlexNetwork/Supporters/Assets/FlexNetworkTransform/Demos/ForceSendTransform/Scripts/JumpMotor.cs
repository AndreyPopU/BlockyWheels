using Mirror;
using UnityEngine;

namespace FirstGearGames.Mirrors.Assets.FlexNetworkTransforms.Demos
{

    public class JumpMotor : MonoBehaviour
    {
        [SerializeField]
        private float _peak = 4f;
        [SerializeField]
        private float _delay = 0f;
        [SerializeField]
        private float _upRate = 5f;
        [SerializeField]
        private float _downRate = 9f;
        [SerializeField]
        private float _easing = 0.001f;
        [SerializeField]
        private float _easeStartStop = 1.25f;

        private Vector3 _start;
        private bool _movingUp = true;
        private float _nextStart = 0f;
        private void Awake()
        {
            _start = transform.position;
        }

        private void Update()
        {
            if (!NetworkServer.active)
                return;

            if (Time.time < _nextStart)
                return;

            float posPercent = (Mathf.InverseLerp(_easeStartStop, 2f, transform.position.y));
            float easing = Mathf.Lerp(1f, _easing, posPercent);

            Vector3 target;
            float rate;
            if (_movingUp)
            {
                rate = _upRate * easing;
                target = new Vector3(_start.x, _start.y + _peak, _start.z);
            }
            else
            {
                rate = _downRate * easing;
                target = new Vector3(_start.x, _start.y, _start.z);
            }

            transform.position = Vector3.MoveTowards(transform.position, target, rate * Time.deltaTime);

            if (transform.position == target)
            {
                /* In this example the transform is server auth,
                 * so passing in true to ForceSendTransform indicates
                 * the server is sending the update. If this were client
                 * auth you would send false. */
                FlexNetworkTransformBase fnt = GetComponent<FlexNetworkTransformBase>();
                if (fnt != null)
                    fnt.ForceSendTransform(true);

                _nextStart = Time.time + _delay;
                _movingUp = !_movingUp;
            }

        }
    }


}