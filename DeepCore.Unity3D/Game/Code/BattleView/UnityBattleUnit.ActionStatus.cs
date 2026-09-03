using System;
using System.Collections.Generic;
using Code.BattleView.MaterialActions;
using Code.BattleView.UnitActionStatuses;
using DeepCore;
using DeepCore.GameData.Data;

namespace Code.BattleView
{
    public partial class UnityBattleUnit
    {
        private HashMap<UnitActionStatus, ActionStack> _actionStatus = new HashMap<UnitActionStatus, ActionStack>();
        private ActionStatus _currentActionStatus;
        private ActionStatus _lockActionStatus;

        public ActionStatus CurrentActionStatus
        {
            get { return _currentActionStatus; }
        }

        //注册动作控制对象//
        protected virtual void InitActionStatus()
        {
            //从配置表初始化动作列表//
            foreach (UnitActionStatus st in Enum.GetValues(typeof(UnitActionStatus)))
            {
                var action = ZoneUnit.Templates.GetDefinedUnitAction(st);
                if (action != null)
                {
                    switch (st)
                    {
                        case UnitActionStatus.Skill:
                            RegistAction(st, new SkillActionStatus(st, st.ToString()));
                            break;
                        case UnitActionStatus.Spawn:
                            RegistAction(st, new SpawnActionStatus(st, st.ToString()));
                            break;
                        case UnitActionStatus.Idle:
                            RegistAction(st, new IdleAction(st, st.ToString()));
                            break;
                        case UnitActionStatus.Move:
                            RegistAction(st, new MoveAction(st, st.ToString()));
                            break;
                        default:
                            RegistAction(st, new DefinedActionStatus(st, st.ToString(), action));
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="st"></param>
        /// <param name="action">If return True , break</param>
        public bool ForeachAction(UnitActionStatus st, Predicate<ActionStatus> action)
        {
            ActionStack stack;
            if (_actionStatus.TryGetValue(st, out stack))
            {
                return stack.Foreach(action);
            }

            return false;
        }

        public ActionStatus ReplaceAction(UnitActionStatus st, ActionStatus dst)
        {
            ActionStack stack;
            if (_actionStatus.TryGetValue(st, out stack))
            {
                var ret = stack.Remove(dst.Key);
                if (ret != null)
                {
                    stack.Add(dst);
                    ChangeAction(ZoneUnit.CurrentState);
                }

                return ret;
            }

            return null;
        }

        public ActionStatus RegistAction(UnitActionStatus st, ActionStatus status)
        {
            ActionStack stack;
            if (_actionStatus.TryGetValue(st, out stack) == false)
            {
                stack = new ActionStack(status);
                _actionStatus.Add(st, stack);
            }
            else
            {
                stack.Add(status);
            }

            ChangeAction(ZoneUnit.CurrentState);
            return status;
        }

        public ActionStatus RemoveAction(UnitActionStatus st, string key)
        {
            ActionStack stack;
            if (_actionStatus.TryGetValue(st, out stack))
            {
                var status = stack.Remove(key);
                if (status != null)
                {
                    ChangeAction(ZoneUnit.CurrentState);
                }

                return status;
            }

            return null;
        }

        public ActionStatus GetTopActionStatus(UnitActionStatus st)
        {
            ActionStack stack;
            if (_actionStatus.TryGetValue(st, out stack))
            {
                return stack.Top;
            }

            return null;
        }

        public void SetLockActionStatus(ActionStatus status)
        {
            if (status != null)
            {
                _lockActionStatus = status;
                if (status != _currentActionStatus)
                {
                    if (_currentActionStatus != null)
                    {
                        _currentActionStatus.Stop(this);
                    }

                    _currentActionStatus = status;
                    if (_currentActionStatus != null)
                    {
                        _currentActionStatus.Start(this);
                    }
                }
            }
            else
            {
                _lockActionStatus = null;
                ChangeAction(ZoneUnit.CurrentState);
            }
        }

        protected virtual void ChangeAction(UnitActionStatus st, bool bForce = false)
        {
            if (_lockActionStatus == null)
            {
                var newAction = GetTopActionStatus(st);
                if (newAction != null && (bForce || _currentActionStatus != newAction))
                {
                    if (_currentActionStatus != null)
                    {
                        _currentActionStatus.Stop(this);
                    }

                    _currentActionStatus = newAction;
                    if (_currentActionStatus != null)
                    {
                        _currentActionStatus.Start(this);
                    }
                }
            }
        }

        protected virtual void UpdateAction(float deltaTime)
        {
            if (_currentActionStatus != null)
            {
                _currentActionStatus.Update(this, deltaTime);
            }
        }

        internal class ActionStack
        {
            readonly List<ActionStatus> List = new List<ActionStatus>();

            public ActionStatus Top
            {
                get
                {
                    if (List.Count > 0) return List[List.Count - 1];
                    return null;
                }
            }

            internal ActionStack(ActionStatus status)
            {
                List.Add(status);
            }

            public bool Foreach(Predicate<ActionStatus> action)
            {
                var list = new List<ActionStatus>(List);
                foreach (var a in list)
                {
                    if (action(a))
                    {
                        return true;
                    }
                }

                return false;
            }

            public ActionStatus Add(ActionStatus status)
            {
                var tmp = Get(status.Key);
                if (tmp != null)
                {
                    List.Remove(tmp);
                }
                else
                {
                    tmp = status;
                }

                List.Add(tmp);

                return tmp;
            }

            public ActionStatus Get(string key)
            {
                return List.Find((st) =>
                {
                    if (st.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                        return true;
                    return false;
                });
            }

            public ActionStatus Remove(string key)
            {
                for (var i = 0; i < List.Count; i++)
                {
                    var tmp = List[i];
                    if (tmp.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        List.RemoveAt(i);
                        return tmp;
                    }
                }

                return null;
            }
        }
    }
}