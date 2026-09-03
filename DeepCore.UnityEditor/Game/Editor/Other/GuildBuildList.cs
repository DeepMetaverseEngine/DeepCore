using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace DeepCore.UnityEditor
{
    public class GuildBuildList : MonoBehaviour
    {

        [Serializable]
        public class Build
        {
            public Transform[] trans;
        }

        public Build[] build;

        public void ShowBuildingByLevel(int buildId, int level)
        {
            int index = buildId - 1;
            if (index >= 0 && index < build.Length)
            {
                int lvIndex = level < 0 ? 0 : level;
                lvIndex = lvIndex >= build[index].trans.Length ? build[index].trans.Length - 1 : lvIndex;
                for (int i = 0; i < build[index].trans.Length; ++i)
                {
                    build[index].trans[i].gameObject.SetActive(i == lvIndex);
                }
            }
        }

    }
}