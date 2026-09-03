
using DeepCore.GUI.Data;
using DeepCore.GUI.Display.Node;
using DeepCore.GUI.Gemo;
using System;

namespace DeepCore.GUI.Display.Action
{
    public interface IActionCompment
    {
        void AddAction(IAction action);
        void RemoveAction(IAction action, bool sendCallBack);
        bool HasAction(IAction action);
        void RemoveAllAction(bool sendCallBack = false);
        void UpdateAction(float deltaTime);
        float X { set; get; }
        float Y { set; get; }
        float ScaleX { get; set; }
        float ScaleY { get; set; }
        float Alpha { get; set; }
    }
}
