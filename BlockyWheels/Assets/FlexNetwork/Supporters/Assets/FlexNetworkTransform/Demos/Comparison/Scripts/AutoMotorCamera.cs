using FirstGearGames.Utilities.Networks;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FirstGearGames.Mirrors.Assets.FlexNetworkTransforms.Demos
{

    public class AutoMotorCamera : NetworkBehaviour
    {
        public bool RunOffline = false;
        public float MoveRate = 4f;
        public float MoveVariance = 0.3f;
        public Transform[] Targets = new Transform[0];


        private void Awake()
        {
            if (RunOffline)
                StartCoroutine(__Move());
        }
  
        private IEnumerator __Move()
        {
            Vector3[] startPositions = new Vector3[Targets.Length];
            for (int i = 0; i < Targets.Length; i++)
            {
                if (Targets[i] != null)
                    startPositions[i] = Targets[i].position;
            }

            int waitSteps = 8;
            int step = 0;

            bool movingRight = true;
            while (true)
            {
                bool notAtGoal = false;
                for (int i = 0; i < Targets.Length; i++)
                {
                    if (Targets[i] != null && Targets[i].gameObject.activeInHierarchy)
                    {

                        Vector3 goal = (movingRight) ? startPositions[i] + new Vector3(14f,0f,0f) : startPositions[i];
                        Targets[i].position = Vector3.MoveTowards(Targets[i].position, goal, MoveRate * Time.deltaTime);
                        if (Targets[i].position != goal)
                            notAtGoal = true;
                    }
                }

                //If all transforms are at goal.
                if (!notAtGoal)
                {
                    movingRight = !movingRight;

                    step++;
                    if (step == waitSteps)
                    {
                        step = 0;
                        yield return new WaitForSeconds(1f);
                    }
                }

                yield return null;
            }
        }

    }


}