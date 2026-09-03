
using DeepCore.GUI.Data;
using DeepCore.GUI.Display.Node;
using DeepCore.GUI.Gemo;
using System;

namespace DeepCore.GUI.Display.Action
{
   public interface IAction
    {
        /// <summary>
        /// 动作更新.
        /// </summary>
        /// <param name="unit"></param>
	     void onUpdate(IActionCompment unit , float deltaTime);

        /// <summary>
        /// 动作开始.
        /// </summary>
        /// <param name="unit"></param>
		void onStart(IActionCompment unit);

        /// <summary>
        /// 动作停止.
        /// </summary>
        /// <param name="unit"></param>
		void onStop(IActionCompment unit,bool sendCallBack);
       
        /// <summary>
        /// 动作是否结束.
        /// </summary>
        /// <param name="unit"></param>
        bool IsEnd();

        string GetActionType();
    }
}

