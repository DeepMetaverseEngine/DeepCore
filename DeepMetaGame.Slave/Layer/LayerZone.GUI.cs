using DeepCore.GUI.Data;
using DeepMetaGame.Data.Message.UI;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Slave.GUI;
using System;
using System.Collections.Generic;


namespace DeepCore.Game3D.Slave.Layer
{

    partial class LayerZone
    {
        private HashMap<string, List<IZoneGUIForm>> activeForms = new();
        public ZoneGUIRuntime GUIRuntime { get; set; }

        protected virtual void InitGUI()
        {


        }
        protected virtual void ClearGUI()
        {
            foreach (var forms in activeForms.Values.ToArray())
            {
                foreach (var form in forms)
                {
                    form.Dispose();
                }
            }
            activeForms.Clear();
        }
        protected virtual void ProcessGUIEvents(IMessageGUI msg)
        {
            if (msg is ShowFormEvent show)
            {
                if (show.IsDialog)
                {
                    ShowDialog(show.GUID, show.GUITemplateID, show.CloseOnClick, null);
                }
                else
                {
                    ShowForm(show.GUID, show.GUITemplateID, show.CloseOnClick, null);
                }
            }
            else if (msg is ShowPlayerFormEvent showPlayer)
            {
                if (Actor != null && Actor.ObjectID == showPlayer.object_id)
                {
                    if (showPlayer.IsDialog)
                    {
                        ShowDialog(showPlayer.GUID, showPlayer.GUITemplateID, showPlayer.CloseOnClick, Actor);
                    }
                    else
                    {
                        ShowForm(showPlayer.GUID, showPlayer.GUITemplateID, showPlayer.CloseOnClick, Actor);
                    }
                }
            }
            else if (msg is CloseFormEvent close)
            {
                if (activeForms.TryGetValue(close.GUID, out var forms))
                {
                    foreach (var form in forms)
                    {
                        form.Close();
                    }
                }
            }
            else if (msg is GUINodeBindDataEvent bindData)
            {
                if (activeForms.TryGetValue(bindData.GUID, out var forms))
                {
                    foreach (var form in forms)
                    {
                        try
                        {
                            var v = SlaveFactory.DecodeZoneVar(this, bindData.ZoneVar);
                            form.BindData(bindData.ToArgs(), bindData.Key, bindData.Deep, v);
                        }
                        catch (Exception err)
                        {
                            doError(err);
                        }
                    }
                }
            }
            else if (msg is GUINodeVisibleEvent visible)
            {
                if (activeForms.TryGetValue(visible.GUID, out var forms))
                {
                    foreach (var form in forms)
                    {
                        try
                        {
                            form.SetVisible(visible.ToArgs(), visible.Visible);
                        }
                        catch (Exception err)
                        {
                            doError(err);
                        }
                    }
                }
            }
            else if (msg is GUINodeControlEvent control)
            {
                if (activeForms.TryGetValue(control.GUID, out var forms))
                {
                    foreach (var form in forms)
                    {
                        try
                        {
                            var v = SlaveFactory.DecodeZoneVar(this, control.ZoneVar);
                            form.ControlNode(control.ToArgs(), control.Name, control.Index, v);
                        }
                        catch (Exception err)
                        {
                            doError(err);
                        }
                    }
                }
            }
        }
        protected virtual void ClearGUIEvents()
        {
            OnFormShown = null;
            OnFormClosed = null;
            OnFormNodeClick = null;
            CustomShowForm = null;
            GUIRuntime = null;
        }

        private void ShowDialog(string guid, int templateID, bool closeOnClick, LayerPlayer player)
        {
            if (DataRoot.Templates.TryGetTemplate<BattleUITemplate>(templateID, out var Data))
            {
                if (Data.Forms != null)
                {
                    DoShowDialog(CloneData(Data), guid, dialog =>
                    {
                        activeForms.GetOrNew(guid).Add(dialog);
                        dialog.OnNodeClick += Dialog_OnNodeClick;
                        dialog.OnNodeDataChanged += Dialog_OnNodeDataChanged;
                        dialog.OnShown += Dialog_OnShown;
                        dialog.OnClose += Dialog_OnClose;
                        dialog.Show(closeOnClick, () => { });
                    });
                }
            }
        }

        private void ShowForm(string guid, int templateID, bool closeOnClick, LayerPlayer player)
        {
            if (DataRoot.Templates.TryGetTemplate<BattleUITemplate>(templateID, out var Data))
            {
                if (Data.Forms != null)
                {
                    DoShowForm(CloneData(Data), guid, dialog =>
                    {
                        activeForms.GetOrNew(guid).Add(dialog);
                        dialog.OnNodeClick += Dialog_OnNodeClick;
                        dialog.OnNodeDataChanged += Dialog_OnNodeDataChanged;
                        dialog.OnShown += Dialog_OnShown;
                        dialog.OnClose += Dialog_OnClose;
                        dialog.Show(closeOnClick, () => { });
                    });
                }
            }
        }
        private void DoShowDialog(BattleUITemplate form, string guid, Action<IZoneGUIForm> action)
        {
            if (CustomShowForm != null)
            {
                var dialog = CustomShowForm?.Invoke(this.LayerClient, form, guid, true);
                if (dialog != null)
                {
                    action(dialog);
                    if (form.SingleInstance)
                    {
                        return;
                    }
                }
            }
            if (this.GUIRuntime != null)
            {
                var dialog = GUIRuntime.ShowDialog(this.LayerClient, guid, form);
                if (dialog != null)
                {
                    action(dialog);
                    if (form.SingleInstance)
                    {
                        return;
                    }
                }
            }
        }
        private void DoShowForm(BattleUITemplate form, string guid, Action<IZoneGUIForm> action)
        {
            if (CustomShowForm != null)
            {
                var dialog = CustomShowForm?.Invoke(this.LayerClient, form, guid, false);
                if (dialog != null)
                {
                    action(dialog);
                    if (form.SingleInstance)
                    {
                        return;
                    }
                }
            }
            if (this.GUIRuntime != null)
            {
                var dialog = GUIRuntime.ShowForm(this.LayerClient, guid, form);
                if (dialog != null)
                {
                    action(dialog);
                    if (form.SingleInstance)
                    {
                        return;
                    }
                }
            }
        }

        private void Dialog_OnShown(IZoneGUIForm sender)
        {
            this.OnFormShown?.Invoke(this, sender);
        }
        private void Dialog_OnClose(IZoneGUIForm sender)
        {
            if (activeForms.TryGetValue(sender.GUID, out var forms))
            {
                forms.Remove(sender);
                this.OnFormClosed?.Invoke(this, sender);
                SendAction(ObjectPool.AllocInit(sender, static (sender, t) =>
                {
                    t.GUID = sender.Name;
                }, default(CloseFormAction)));
            }
        }
        private void Dialog_OnNodeClick(IZoneGUINode sender, string subName)
        {
            this.OnFormNodeClick?.Invoke(this, sender);
            SendAction(ObjectPool.AllocInit((sender, subName), static (st, t) =>
            {
                t.GUID = st.sender.Form.GUID;
                t.NodeName = st.sender.Name;
                t.DialogResult = st.sender.DialogResult;
                t.SubNodeURL = st.subName;
            }, default(GUINodeClickAction)));
            //SendGUINodeClick(sender.Form.GUID, sender.Name, sender.Meta.DialogResult);
        }
        private void Dialog_OnNodeDataChanged(IZoneGUINode sender, string subName)
        {
            SendAction(ObjectPool.AllocInit((sender, subName), static (st, t) =>
            {
                t.GUID = st.sender.Form.GUID;
                t.NodeName = st.sender.Name;
                t.TextValue = st.sender.GetString();
                t.BooleanValue = st.sender.GetBool();
                t.NumberValue = st.sender.GetNumber();
                t.SubNodeURL = st.subName;
            }, default(GUINodeDataChangedAction)));
        }
        //         public void SendGUINodeClick(string formGUID, string nodeName, string result)
        //         {
        //             SendAction(new GUINodeClickAction()
        //             {
        //                 GUID = formGUID,
        //                 NodeName = nodeName,
        //                 DialogResult = result,
        //             });
        //         }
        //         public void SendGUINodeDataChanged(string formGUID, string nodeName, string value)
        //         {
        //             SendAction(new GUINodeDataChangedAction()
        //             {
        //                 GUID = formGUID,
        //                 NodeName = nodeName,
        //                 TextValue = value,
        //             });
        //         }
        //         public void SendGUINodeDataChanged(string formGUID, string nodeName, bool value)
        //         {
        //             SendAction(new GUINodeDataChangedAction()
        //             {
        //                 GUID = formGUID,
        //                 NodeName = nodeName,
        //                 BooleanValue = value,
        //             });
        //         }
        //         public void SendGUINodeDataChanged(string formGUID, string nodeName, double value)
        //         {
        //             SendAction(new GUINodeDataChangedAction()
        //             {
        //                 GUID = formGUID,
        //                 NodeName = nodeName,
        //                 NumberValue = value,
        //             });
        //         }


        public event OnFormHandler OnFormShown;
        public event OnFormHandler OnFormClosed;
        public event OnNodeHandler OnFormNodeClick;
        public event OnShowFormDataHandler CustomShowForm;

        public delegate void OnFormHandler(LayerZone zone, IZoneGUIForm form);
        public delegate void OnNodeHandler(LayerZone zone, IZoneGUINode node);



        public delegate IZoneGUIForm OnShowFormDataHandler(ILayerZoneListener zone, BattleUITemplate form, string guid, bool dialog);

    }


}