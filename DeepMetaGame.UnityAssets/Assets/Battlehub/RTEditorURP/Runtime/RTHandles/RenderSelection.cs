using Battlehub.RTCommon;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Battlehub.RTHandles.URP
{
    public class RenderSelection : ScriptableRendererFeature
    {
        [System.Serializable]
        public class RenderSelectionSettings
        {
            public RenderPassEvent RenderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            public Material PrepassMaterial = null;
            public Material BlurMaterial = null;
            public Material CompositeMaterial = null;
            public Color OutlineColor = new Color32(255, 128, 0, 255);
            public string MeshesCacheName = "SelectedMeshes";
            public string RenderersCacheName = "SelectedRenderers";
            public string CustomRenderersCacheName = "CustomOutlineRenderersCache";
            public LayerMask LayerMask = -1;

            [Range(0.5f, 10f)]
            public float OutlineStength = 5;

            [Range(0.1f, 3)]
            public float BlurSize = 1f;
        }

        [SerializeField]
        public RenderSelectionSettings m_settings = new RenderSelectionSettings();

        class RenderSelectionPass : ScriptableRenderPass
        {
            public RenderSelectionSettings Settings;

            private IMeshesCache m_meshesCache;
            private IRenderersCache m_renderersCache;
            private ICustomOutlineRenderersCache m_customRenderersCache;

            private int m_prepassId;
            private RenderTargetIdentifier m_prepassRT;

            private int m_blurredId;
            private RenderTargetIdentifier m_blurredRT;

            private int m_tmpTexId;
            private RenderTargetIdentifier m_tmpRT;

            private int m_outlineColorId;
            private int m_outlineStrengthId;
            private int m_blurDirectionId;

            private bool m_rtCreated = false;

            /// <summary>
            /// 初始化选中对象缓存引用
            /// </summary>
            public void Setup(IMeshesCache meshesCache, IRenderersCache renderersCache, ICustomOutlineRenderersCache customRenderersCache)
            {
                m_meshesCache = meshesCache;
                m_renderersCache = renderersCache;
                m_customRenderersCache = customRenderersCache;
                m_rtCreated = false;
            }

            /// <summary>
            /// 获取兼容立体/VR渲染的RenderTexture描述器
            /// </summary>
            private RenderTextureDescriptor GetStereoCompatibleDescriptor(RenderTextureDescriptor descriptor, int width, int height, GraphicsFormat format, int depthBufferBits = 0)
            {
                var desc = descriptor;
                desc.depthBufferBits = depthBufferBits;
                desc.msaaSamples = 1;
                desc.width = width;
                desc.height = height;
                desc.graphicsFormat = format;
                return desc;
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor camDesc)
            {
                if (m_rtCreated) return;

                var width = camDesc.width;
                var height = camDesc.height;

                // 初始化Shader全局属性ID
                m_prepassId = Shader.PropertyToID("_PrepassTex");
                m_blurredId = Shader.PropertyToID("_BlurredTex");
                m_tmpTexId = Shader.PropertyToID("_TmpTex");
                m_outlineColorId = Shader.PropertyToID("_OutlineColor");
                m_outlineStrengthId = Shader.PropertyToID("_OutlineStrength");
                m_blurDirectionId = Shader.PropertyToID("_BlurDirection");

                // 创建与相机同尺寸、同格式的临时渲染纹理
                var desc = GetStereoCompatibleDescriptor(camDesc, width, height, camDesc.graphicsFormat);
                cmd.GetTemporaryRT(m_prepassId, desc);
                cmd.GetTemporaryRT(m_blurredId, desc);
                cmd.GetTemporaryRT(m_tmpTexId, desc);

                // 封装为渲染目标标识
                m_prepassRT = new RenderTargetIdentifier(m_prepassId);
                m_blurredRT = new RenderTargetIdentifier(m_blurredId);
                m_tmpRT = new RenderTargetIdentifier(m_tmpTexId);

                // 配置预处理渲染目标，清空为透明黑
                ConfigureTarget(m_prepassRT);
                ConfigureClear(ClearFlag.Color, new Color(0, 0, 0, 1));

                m_rtCreated = true;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                // 基础有效性校验：避免无效渲染操作
                if (!m_rtCreated || renderingData.cameraData.camera == null || context == null)
                    return;
                // 材质判空：缺一不可，弹窗提示
                if (Settings.PrepassMaterial == null || Settings.BlurMaterial == null || Settings.CompositeMaterial == null)
                {
                    Debug.LogError("[RenderSelection] 请在编辑器为RenderSelectionFeature赋值Prepass/Blur/Composite三个材质！");
                    return;
                }

                // ==================== Unity2022 URP 终极适配 ====================
                // 从渲染数据中获取URP管线渲染器，判空后直接获取相机颜色纹理
                ScriptableRenderer urpRenderer = renderingData.cameraData.renderer;
                if (urpRenderer == null) return;
                // 直接获取：Unity2022中urpRenderer非空则该纹理必然有效，无需额外判断
                RenderTargetIdentifier cameraColorTarget = urpRenderer.cameraColorTargetHandle;
                // ==============================================================

                CommandBuffer cmd = CommandBufferPool.Get("RenderSelection");

                // 绘制选中的网格批次：批处理渲染优化
                if (m_meshesCache != null && !m_meshesCache.IsEmpty)
                {
                    IList<RenderMeshesBatch> batches = m_meshesCache.Batches;
                    for (int i = 0; i < batches.Count; ++i)
                    {
                        RenderMeshesBatch batch = batches[i];
                        if (batch.Mesh == null || batch.Matrices == null || batch.Matrices.Length == 0)
                            continue;
                        for (int j = 0; j < batch.Mesh.subMeshCount; ++j)
                        {
                            cmd.DrawMeshInstanced(batch.Mesh, j, Settings.PrepassMaterial, 0, batch.Matrices, batch.Matrices.Length);
                        }
                    }
                }

                // 绘制选中的普通渲染器：网格/精灵/粒子等
                if (m_renderersCache != null && !m_renderersCache.IsEmpty)
                {
                    IList<Renderer> renderers = m_renderersCache.Renderers;
                    for (int i = 0; i < renderers.Count; ++i)
                    {
                        Renderer meshRenderer = renderers[i];
                        if (meshRenderer == null || !meshRenderer.enabled || !meshRenderer.gameObject.activeSelf)
                            continue;
                        Material[] materials = meshRenderer.sharedMaterials;
                        if (materials == null || materials.Length == 0)
                            continue;
                        for (int j = 0; j < materials.Length; ++j)
                        {
                            cmd.DrawRenderer(meshRenderer, Settings.PrepassMaterial, j);
                        }
                    }
                }

                // 绘制自定义描边渲染器【已移除无效continue，逻辑紧凑】
                if (m_customRenderersCache != null)
                {
                    List<ICustomOutlinePrepass> renderers = m_customRenderersCache.GetOutlineRendererItems();
                    if (renderers != null && renderers.Count > 0)
                    {
                        for (int i = 0; i < renderers.Count; ++i)
                        {
                            ICustomOutlinePrepass customRenderer = renderers[i];
                            if (customRenderer == null || !customRenderer.GetRenderer().gameObject.activeSelf)
                                continue;
                            Material prepassMat = customRenderer.GetOutlinePrepassMaterial();
                            if (prepassMat == null)
                                continue;
                            Material[] materials = customRenderer.GetRenderer().sharedMaterials;
                            if (materials == null || materials.Length == 0)
                                continue;
                            for (int j = 0; j < materials.Length; ++j)
                            {
                                cmd.DrawRenderer(customRenderer.GetRenderer(), prepassMat, j);
                            }
                        }
                    }
                }

                // 描边模糊流程：水平+垂直两步高斯模糊（标准描边模糊实现）
                cmd.Blit(m_prepassRT, m_blurredRT);
                cmd.SetGlobalFloat(m_outlineStrengthId, Settings.OutlineStength);
                cmd.SetGlobalVector(m_blurDirectionId, new Vector2(Settings.BlurSize, 0));
                cmd.Blit(m_blurredRT, m_tmpRT, Settings.BlurMaterial, 0);
                cmd.SetGlobalVector(m_blurDirectionId, new Vector2(0, Settings.BlurSize));
                cmd.Blit(m_tmpRT, m_blurredRT, Settings.BlurMaterial, 0);

                // 描边合成核心逻辑：将描边叠加到相机原始渲染画面
                cmd.Blit(cameraColorTarget, m_tmpRT);
                cmd.SetGlobalTexture(m_prepassId, m_prepassRT);
                cmd.SetGlobalTexture(m_blurredId, m_blurredRT);
                cmd.SetGlobalColor(m_outlineColorId, Settings.OutlineColor);
                cmd.Blit(m_tmpRT, cameraColorTarget, Settings.CompositeMaterial);

                // 提交命令缓冲区并释放，避免内存泄漏
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void FrameCleanup(CommandBuffer cmd)
            {
                base.FrameCleanup(cmd);
                // 安全释放临时渲染纹理：判空+标记双重保护
                if (m_rtCreated && cmd != null)
                {
                    cmd.ReleaseTemporaryRT(m_prepassId);
                    cmd.ReleaseTemporaryRT(m_blurredId);
                    cmd.ReleaseTemporaryRT(m_tmpTexId);
                    m_rtCreated = false;
                }
                // 清空缓存引用，避免跨帧持有导致的内存泄漏/空指针
                m_meshesCache = null;
                m_renderersCache = null;
                m_customRenderersCache = null;
            }
        }

        private RenderSelectionPass m_scriptablePass;

        public override void Create()
        {
            // 初始化自定义渲染通道，配置渲染时机
            m_scriptablePass = new RenderSelectionPass();
            m_scriptablePass.Settings = m_settings;
            m_scriptablePass.renderPassEvent = m_settings.RenderPassEvent;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // 前置有效性校验：避免空指针
            if (renderer == null || renderingData.cameraData.camera == null)
                return;
            // 图层掩码校验：仅在指定图层执行描边
            if ((renderingData.cameraData.camera.cullingMask & m_settings.LayerMask) == 0)
                return;

            // 从IOC容器解析选中对象缓存
            IMeshesCache meshesCache = IOC.Resolve<IMeshesCache>(m_settings.MeshesCacheName);
            IRenderersCache renderersCache = IOC.Resolve<IRenderersCache>(m_settings.RenderersCacheName);
            ICustomOutlineRenderersCache customRenderersCache = IOC.Resolve<ICustomOutlineRenderersCache>(m_settings.CustomRenderersCacheName);

            // 无选中对象时，直接返回，不执行渲染通道（性能优化）
            if ((meshesCache == null || meshesCache.IsEmpty) &&
                (renderersCache == null || renderersCache.IsEmpty) &&
                (customRenderersCache == null || customRenderersCache.GetOutlineRendererItems() == null || customRenderersCache.GetOutlineRendererItems().Count == 0))
            {
                return;
            }

            // 初始化渲染通道并加入管线，不传递任何渲染纹理（规避生命周期问题）
            m_scriptablePass.Setup(meshesCache, renderersCache, customRenderersCache);
            renderer.EnqueuePass(m_scriptablePass);
        }

        /// <summary>
        /// 销毁时清空引用，避免内存泄漏
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            m_scriptablePass = null;
            base.Dispose(disposing);
        }
    }
}