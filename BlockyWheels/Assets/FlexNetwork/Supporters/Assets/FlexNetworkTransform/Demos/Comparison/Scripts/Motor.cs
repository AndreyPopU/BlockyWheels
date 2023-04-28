using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FirstGearGames.Mirrors.Assets.FlexNetworkTransforms.Demos
{

    public class Motor : NetworkBehaviour
    {
        [SerializeField]
        private float _moveRate = 3f;
        [SerializeField]
        private bool _clientAuth = true;

        private FlexNetworkTransform _fnt;

        private void Awake()
        {
            _fnt = GetComponent<FlexNetworkTransform>();
        }
        private void Update()
        {
            if (!base.hasAuthority)
                return;

            float horizontal = Input.GetAxisRaw("Horizontal");
            if (horizontal == 0f)
                return;

            if (_clientAuth)
            {
                Move(horizontal);
            }
            else
            {
                CmdSendHorizontal(horizontal);
            }
        }

        [Command]
        private void CmdSendHorizontal(float hor)
        {
            Move(hor);
        }

        private void Move(float horizontal)
        {
            transform.position += new Vector3(horizontal * _moveRate * Time.deltaTime, 0f, 0f);

            //Switch facing.
            if (horizontal > 0f)
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, 0f, transform.eulerAngles.z);
            else
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, 180f, transform.eulerAngles.z);
        }

    }


}