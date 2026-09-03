using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DeepCore.Unity3D.UGUIAction
{

    public interface IAction
    {
        /// <summary>
        /// 动作更新.
        /// </summary>
        /// <param name="unit"></param>
        void onUpdate(IActionCompment unit, float deltaTime);

        /// <summary>
        /// 动作开始.
        /// </summary>
        /// <param name="unit"></param>
        void onStart(IActionCompment unit);

        /// <summary>
        /// 动作停止.
        /// </summary>
        /// <param name="unit"></param>
        void onStop(IActionCompment unit, bool sendCallBack);

        /// <summary>
        /// 动作是否结束.
        /// </summary>
        /// <param name="unit"></param>
        bool IsEnd();

        string GetActionType();
    }

    public interface IActionCompment
    {
        void AddAction(IAction action);
        void RemoveAction(IAction action, bool sendCallBack);
        bool HasAction(IAction action);
        void RemoveAllAction(bool sendCallBack = false);
        void UpdateAction(float deltaTime);
        float X { set; get; }
        float Y { set; get; }
        Vector2 Scale { get; set; }
        Vector2 Position2D { get; set; }
        Vector2 Size2D { get; set; }
        float Alpha { get; set; }
    }

}
