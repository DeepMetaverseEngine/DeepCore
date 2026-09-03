using DeepCore.FuncData;
using DeepCore.Reflection;
using DeepCore.Xml;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepMetaGame.Data.Misc
{
    public interface IKeyFrame
    {
        int FrameMS { get; }
    }

    public abstract class BaseKeyFrame : IBaseFuncData, IKeyFrame, IPropertiesOwner
    {
        [DescAttribute("关键帧时间(毫秒)")]
        public int FrameMS;
        [Desc("自定义动作")]
        public IKeyFrameProperties CustomAction;

        int IKeyFrame.FrameMS => FrameMS;

        IPropertiesData IPropertiesOwner.PropertiesData => CustomAction;
        public BaseKeyFrame()
        {
            CustomAction = ZoneDataFactory.Factory.CreateProperties<IKeyFrameProperties>(this);
        }

    }


}
