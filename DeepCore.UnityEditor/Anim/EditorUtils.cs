using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
#if false
public static class AnimUtils
{
    public static void CompressAllAnimationClips()
    {
        //逻辑跟之前差不多
        DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath + PathTools._AnimationClipsDir);
        FileInfo[] fileInfos = directoryInfo.GetFiles();
        foreach (FileInfo fileInfo in fileInfos)
        {
            if (fileInfo.Extension == ".anim")
            {
                //string fileName = fileInfo.Name.Substring(0, fileInfo.Name.IndexOf(".")); ;
                string pathName = fileInfo.FullName.Substring(fileInfo.FullName.IndexOf("Assets\\"));
                //获取之前导出的animationClip
                AnimationClip animationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(pathName);
                if (animationClip != null)
                {
                    //复制一份进行压缩动画
                    var clip = UnityEngine.Object.Instantiate(animationClip) as AnimationClip;
                    CompressAnim(clip);

                    EditorUtility.CopySerialized(clip, animationClip);
                    EditorUtility.SetDirty(animationClip);
                }
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    //压缩单个animation 创建新的animation
    public static void CompressAnim(AnimationClip clip)
    {
        ReduceScaleKey(clip);
        ReduceFloatPrecision(clip);
    }

    //去掉骨骼动画scale
    private static void ReduceScaleKey(AnimationClip clip)
    {
        EditorCurveBinding[] curves = AnimationUtility.GetCurveBindings(clip);

        for (int j = 0; j < curves.Length; j++)
        {
            EditorCurveBinding curveBinding = curves[j];

            if (curveBinding.propertyName.ToLower().Contains("scale"))
                AnimationUtility.SetEditorCurve(clip, curveBinding, null);
        }

    }

    //浮点精度保留3位
    private static void ReduceFloatPrecision(AnimationClip clip)
    {
        EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(clip);
        AnimationClipCurveData[] curves = new AnimationClipCurveData[curveBindings.Length];
        for (int index = 0; index < curves.Length; ++index)
        {
            curves[index] = new AnimationClipCurveData(curveBindings[index]);
            curves[index].curve = AnimationUtility.GetEditorCurve(clip, curveBindings[index]);
        }
        foreach (AnimationClipCurveData curveDate in curves)
        {
            var keyFrames = curveDate.curve.keys;
            for (int i = 0; i < keyFrames.Length; i++)
            {
                var key = keyFrames[i];
                key.value = float.Parse(key.value.ToString("f3"));
                key.inTangent = float.Parse(key.inTangent.ToString("f3"));
                key.outTangent = float.Parse(key.outTangent.ToString("f3"));
                keyFrames[i] = key;
            }
            curveDate.curve.keys = keyFrames;
            clip.SetCurve(curveDate.path, curveDate.type, curveDate.propertyName, curveDate.curve);
        }
    }


    //根据压缩过的clip，创建AnimatorController
    public static void CreateAnimatorControllerByClip()
    {
        //遍历压缩过的所有animtionClip
        DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath + PathTools._AnimationClipsDir);
        FileInfo[] files = directoryInfo.GetFiles("*.anim", SearchOption.AllDirectories);
        string animatorName = "";
        List<AnimationNameAndCondition> tempList = new List<AnimationNameAndCondition>();//这里用到一个数据结构,里面包含一个string和一个int类型
        foreach (var file in files)
        {
            string fileName = file.Name.Substring(0, file.Name.IndexOf("."));
            string pathName = file.FullName.Substring(file.FullName.IndexOf("Assets\\"));
            string[] names = fileName.Split('_');
            if (names.Length != 2 || names[0] != "Model")
            {
                continue;
            }
            if (animatorName.Equals(""))
            {
                animatorName = names[0];
            }
            int conditionValue = int.Parse(names[1]);
            AnimationNameAndCondition temp = new AnimationNameAndCondition(pathName, conditionValue); //将名字和序列存放进list中
            tempList.Add(temp);
        }
        if (tempList.Count == 0)
        {
            Debug.LogError("Animation获取失败");
            return;
        }
        CreateAnimatorController(PathTools._AnimaControlDir, animatorName, tempList);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Animator已生成");
    }
    /// <summary>
    /// 创建动画控制器
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    private static AnimatorController CreateAnimatorController(string animationControlDir, string animatorName, List<AnimationNameAndCondition> animationList)
    {
        //创建animator
        var animatorPath = animationControlDir + animatorName + ".controller";
        AnimatorController animatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(animatorPath);
        if (animatorController == null)
        {
            animatorController = AnimatorController.CreateAnimatorControllerAtPath(animationControlDir + animatorName + ".controller");
        }

        //设定一个名为Action的Int类型Parameter用作动画切换的标志
        bool isExist = false;
        var parameters = animatorController.parameters;
        foreach (var param in parameters)
        {
            if (param.name == "Action")
            {
                isExist = true;
            }
        }
        if (isExist == false)
        {
            animatorController.AddParameter("Action", AnimatorControllerParameterType.Int);
        }

        //获取base layer
        AnimatorControllerLayer animatorControllerLayer = animatorController.layers[0];
        AnimatorStateMachine animatorStateMachine = animatorControllerLayer.stateMachine;

        //按condition排序
        animationList.Sort((left, right) =>
        {
            if (left.Condition > right.Condition)
                return 1;
            else if (left.Condition == right.Condition)
                return 0;
            else
                return -1;
        });

        //开始添加状态
        bool enterState = false;
        foreach (var animationPath in animationList)
        {
            AnimationClip animationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animationPath.Path);
            if (!animationClip)
                continue;

            //添加一个状态
            AnimatorState state = null;
            string stateName = animatorName + "_" + animationPath.Condition;
            var childState = animatorStateMachine.states;
            foreach (var animatorState in childState)//遍历是否之前存在状态
            {
                if (animatorState.state.name == stateName)
                {
                    state = animatorState.state;
                    break;
                }
            }
            //不存在状态，则添加一个
            if (state == null)
            {
                state = animatorStateMachine.AddState(stateName);
            }
            state.motion = animationClip;//加入状态机中

            //第一个状态默认连接enter
            if (enterState == false)
            {
                var enterStates = animatorStateMachine.entryTransitions;
                if (enterStates.Length == 0)
                {
                    animatorStateMachine.AddEntryTransition(state);
                }
                enterState = true;
            }
            //所有状态连接anystate
            AnimatorStateTransition stateTransition = null;
            var anyTransitions = animatorStateMachine.anyStateTransitions;
            foreach (var transitions in anyTransitions)//同上的遍历
            {
                if (transitions.destinationState.name == stateName)
                {
                    stateTransition = transitions;
                    break;
                }
            }
            if (stateTransition == null)
            {
                stateTransition = animatorStateMachine.AddAnyStateTransition(state);
                //将之前命名下划线后的序列作为动作切换的值
                stateTransition.AddCondition(AnimatorConditionMode.Equals, animationPath.Condition, "Action");
            }

            stateTransition.hasExitTime = false;
            stateTransition.canTransitionToSelf = false;
        }

        EditorUtility.SetDirty(animatorController);

        return animatorController;
    }

}


public class AnimationNameAndCondition
{
    public string Path;
    public int Condition;

    public AnimationNameAndCondition(string path, int condition)
    {
        Path = path;
        Condition = condition;
    }
}
#endif