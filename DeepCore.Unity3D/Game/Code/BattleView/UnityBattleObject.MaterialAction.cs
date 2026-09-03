using System;
using System.Collections.Generic;
using Code.BattleView.MaterialActions;
using DeepCore;

namespace Code.BattleView
{
    public partial class UnityBattleObject
    {
        private HashMap<Type, MaterialAction> _materialActions = new HashMap<Type, MaterialAction>();

        private void ClearMaterialActions()
        {
            foreach (var key in _materialActions)
            {
                key.Value.Dispose();
            }
            _materialActions.Clear();
        }
        private void UpdateMaterialActions(int deltaMS)
        {
            if (_materialActions.Count > 0)
            {
                var finished = new List<Type>();
                foreach (var pair in _materialActions)
                {
                    pair.Value.Update(deltaMS);
                    if (pair.Value.IsDone)
                    {
                        finished.Add(pair.Key);
                    }
                }

                if (finished.Count > 0)
                {
                    foreach (var key in finished)
                    {
                        _materialActions[key].Dispose();
                        _materialActions.Remove(key);
                    }
                }
            }
        }

        public void DoMaterialAction<T>(T action, bool overlay = true) where T : MaterialAction
        {
            var type = typeof(T);
            if (!_materialActions.TryGetValue(type, out var tmp))
            {
                _materialActions.Add(type, action);
            }
            else
            {
                if (overlay)
                {
                    tmp.Dispose();
                }

                _materialActions[type] = action;
            }
        }
    }
}
