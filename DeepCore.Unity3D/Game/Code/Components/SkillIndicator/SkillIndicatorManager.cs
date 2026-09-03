using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Code.System.Resource;
using Gate.Client;
using IOGame.Client;
using IOGame.Client.Unity.IOBattle;
using IOGame.Core.Battle.Data;
using SkillIndicator.Basic;
using UnityEngine;

namespace Code.Managers
{
    public class SkillIndicatorManager : MonoBehaviour
    {
        public static SkillIndicatorManager Instance; 
        public LayerMask ProjectorIgnore = 0;

        private Dictionary<IOSkillPreWarning.PreWarningType, SpellIndicator> mSpellIndicators;
        private Dictionary<IOSkillPreWarning.PreWarningType, RangeIndicator> mRangeIndicators;

        /// <summary>
        /// 加载资源
        /// </summary>
        private Dictionary<IOSkillPreWarning.PreWarningType, WrapGO> mWrapMapping;
        
        private async void Awake()
        {
            Instance = this;
            
            mSpellIndicators ??= new Dictionary<IOSkillPreWarning.PreWarningType, SpellIndicator>();
            mRangeIndicators ??= new Dictionary<IOSkillPreWarning.PreWarningType, RangeIndicator>();

            var config = GateClientManager.Battle.DataRoot.Templates.ExtConfig;
            if (config is not IOGameConfig cfg)
                return;
            
            if ((cfg.SkillWarningRes?.Count ?? 0) == 0)
                return;

            mWrapMapping ??= new Dictionary<IOSkillPreWarning.PreWarningType, WrapGO>(cfg.SkillWarningRes.Count);
            mWrapMapping.Clear();

            foreach (var res_path in cfg.SkillWarningRes)
            {
                var ab_name = Path.GetFileNameWithoutExtension(res_path.Value.Res);
                var wrap = await ResourceSystem.GetWrapGOAsync(res_path.Value.Res, ab_name);
                mWrapMapping[res_path.Key] = wrap;
                if (wrap != null)
                {
                    var splat = wrap.GameObject.GetComponent<Splat>();
                    switch (splat)
                    {
                        case SpellIndicator si:
                            mSpellIndicators.Add(res_path.Key, si);
                            break;
                        case RangeIndicator ri:
                            mRangeIndicators.Add(res_path.Key, ri);
                            break;
                    }
                }
            }

            
        }

        private void OnEnable()
        {
            foreach (var kv in mSpellIndicators) 
                InitSplat(kv.Value);

            
            foreach (var kv in mRangeIndicators)
                InitSplat(kv.Value);
        }

        private void OnDisable()
        {
            foreach (var v in mSpellIndicators.Values) 
                Destroy(v);
            
            mSpellIndicators.Clear();
            mSpellIndicators = null;
            
            foreach (var v in mRangeIndicators.Values) 
                Destroy(v);

            mRangeIndicators.Clear();
            mRangeIndicators = null;
            if (mWrapMapping != null)
            {
                foreach (var v in mWrapMapping)
                    v.Value.Dispose();

                mWrapMapping.Clear();
            }
            mWrapMapping = null;
        }

        public SpellIndicator CurrentSpellIndicator { get; private set; }
        
        public void ShowIndicator(int skillID)
        {
            var skill = GateClientManager.Battle.DataRoot.Templates.GetSkill(skillID);
            if (skill.Properties is IOSkillProperties expand)
            {
                var indicator = GetIndicator(expand.PreWarningType);
                if (indicator != null)
                {
                    indicator.Active();
                    CurrentSpellIndicator = indicator;
                }
                
            }
            
        }
        
        


        private SpellIndicator GetIndicator(IOSkillPreWarning.PreWarningType type)
        {
            if (mSpellIndicators.TryGetValue(type, out var ret))
            {
                return ret;
            }
            return null;
        }
        

        private void InitSplat(Splat item)
        {
            item.Init();
            item.gameObject.SetActive(false);
            UpdateIgnoreLayers(item);
        }

        private void UpdateIgnoreLayers(Splat splat)
        {
            splat.mProjector.ToList().ForEach(projector =>
            {
                projector.ignoreLayers = ProjectorIgnore;
            });
        }

        public void CloseIndicator()
        {
            
        }
        
        
    }


}