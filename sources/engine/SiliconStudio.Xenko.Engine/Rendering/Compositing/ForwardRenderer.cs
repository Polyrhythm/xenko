using System.ComponentModel;
using System.Linq;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Storage;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.Rendering.Images;
using SiliconStudio.Xenko.Rendering.Lights;
using SiliconStudio.Xenko.Rendering.Shadows;

namespace SiliconStudio.Xenko.Rendering.Compositing
{
    /// <summary>
    /// Renders your game. It should use current <see cref="RenderContext.RenderView"/> and <see cref="CameraComponentRendererExtensions.GetCurrentCamera"/>.
    /// </summary>
    public partial class ForwardRenderer : SceneRendererBase, ISharedRenderer
    {
        private IShadowMapRenderer shadowMapRenderer;
        private Texture depthStencilROCached;

        public ClearRenderer Clear { get; set; } = new ClearRenderer();

        /// <summary>
        /// The main render stage for opaque geometry.
        /// </summary>
        public RenderStage OpaqueRenderStage { get; set; }

        /// <summary>
        /// The transparent render stage for transparent geometry.
        /// </summary>
        public RenderStage TransparentRenderStage { get; set; }

        /// <summary>
        /// The shadow map render stage for shadow casters. No shadows rendering will happen if null.
        /// </summary>
        public RenderStage ShadowMapRenderStage { get; set; }

        /// <summary>
        /// The post effects renderer.
        /// </summary>
        public PostProcessingEffects PostEffects { get; set; }

        /// <summary>
        /// If true, depth buffer generated during <see cref="OpaqueRenderStage"/> will be available as a shader resource named DepthBase.DepthStencil during <see cref="TransparentRenderStage"/>.
        /// </summary>
        /// <remarks>
        /// This is needed by some effects such as particles soft edges.
        /// 
        /// On recent platforms that can bind depth buffer as read-only (<see cref="GraphicsDeviceFeatures.HasDepthAsReadOnlyRT"/>), depth buffer will be used as is. Otherwise, a copy will be generated.
        /// </remarks>
        [DefaultValue(true)]
        public bool BindDepthAsResourceDuringTransparentRendering { get; set; } = true;


        protected override void InitializeCore()
        {
            base.InitializeCore();

            shadowMapRenderer = Context.RenderSystem.RenderFeatures.OfType<MeshRenderFeature>().FirstOrDefault()?.RenderFeatures.OfType<ForwardLightingRenderFeature>().FirstOrDefault()?.ShadowMapRenderer;
        }


        protected override void CollectCore(RenderContext context)
        {
            // Setup pixel formats for RenderStage
            using (context.SaveRenderOutputAndRestore())
            {
                if (PostEffects != null)
                {
                    context.RenderOutput = new RenderOutputDescription(PostEffects != null ? PixelFormat.R16G16B16A16_Float : context.RenderOutput.RenderTargetFormat0, PixelFormat.D24_UNorm_S8_UInt);
                }

                // Mark this view as requiring shadows
                shadowMapRenderer?.RenderViewsWithShadows.Add(context.RenderView);

                // Fill RenderStage formats and register render stages to main view
                if (OpaqueRenderStage != null)
                {
                    context.RenderView.RenderStages.Add(OpaqueRenderStage);
                    OpaqueRenderStage.Output = context.RenderOutput;
                }
                if (TransparentRenderStage != null)
                {
                    context.RenderView.RenderStages.Add(TransparentRenderStage);
                    TransparentRenderStage.Output = context.RenderOutput;
                }

                if (ShadowMapRenderStage != null)
                    ShadowMapRenderStage.Output = new RenderOutputDescription(PixelFormat.None, PixelFormat.D32_Float);
            }
        }

        protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
        {
            var renderSystem = context.RenderSystem;
            var viewport = drawContext.CommandList.Viewport;

            var currentRenderTarget = drawContext.CommandList.RenderTarget;
            var currentDepthStencil = drawContext.CommandList.DepthStencilBuffer;

            // Allocate render targets
            var renderTarget = PostEffects != null ? PushScopedResource(drawContext.GraphicsContext.Allocator.GetTemporaryTexture2D(TextureDescription.New2D((int)viewport.Width, (int)viewport.Height, 1, PixelFormat.R16G16B16A16_Float, TextureFlags.ShaderResource | TextureFlags.RenderTarget))) : currentRenderTarget;

            using (drawContext.PushRenderTargetsAndRestore())
            {
                if (PostEffects != null)
                    drawContext.CommandList.SetRenderTargetAndViewport(currentDepthStencil, renderTarget);

                // Clear render target and depth stencil
                Clear?.Draw(drawContext);

                // Render Shadow maps
                if (shadowMapRenderer != null)
                {
                    // Clear atlases
                    shadowMapRenderer.PrepareAtlasAsRenderTargets(drawContext.CommandList);

                    using (drawContext.PushRenderTargetsAndRestore())
                    {
                        // Draw all shadow views generated for the current view
                        foreach (var renderView in renderSystem.Views)
                        {
                            var shadowmapRenderView = renderView as ShadowMapRenderView;
                            if (shadowmapRenderView != null && shadowmapRenderView.RenderView == drawContext.RenderContext.RenderView)
                            {
                                if (Profiling)
                                    drawContext.CommandList.BeginProfile(Color.Black, $"Shadow Map {shadowmapRenderView.ShadowMapTexture.Light}");

                                var shadowMapRectangle = shadowmapRenderView.Rectangle;
                                drawContext.CommandList.SetRenderTarget(shadowmapRenderView.ShadowMapTexture.Atlas.Texture, null);
                                shadowmapRenderView.ShadowMapTexture.Atlas.MarkClearNeeded();
                                drawContext.CommandList.SetViewport(new Viewport(shadowMapRectangle.X, shadowMapRectangle.Y, shadowMapRectangle.Width, shadowMapRectangle.Height));

                                renderSystem.Draw(drawContext, shadowmapRenderView, shadowmapRenderView.RenderStages[0].RenderStage);

                                if (Profiling)
                                    drawContext.CommandList.EndProfile();
                            }
                        }
                    }

                    shadowMapRenderer.PrepareAtlasAsShaderResourceViews(drawContext.CommandList);
                }

                // Draw [main view | main stage]
                if (OpaqueRenderStage != null)
                    renderSystem.Draw(drawContext, context.RenderView, OpaqueRenderStage);

                // Draw [main view | transparent stage]
                if (TransparentRenderStage != null)
                {
                    // Some transparent shaders will require the depth as a shader resource - resolve it only once and set it here
                    var depthStencilSRV = ResolveDepthAsSRV(drawContext);

                    renderSystem.Draw(drawContext, context.RenderView, TransparentRenderStage);

                    // Free the depth texture since we won't need it anymore
                    if (depthStencilSRV != null)
                    {
                        drawContext.Resolver.ReleaseDepthStenctilAsShaderResource(depthStencilSRV);
                    }
                }

                // Run post effects
                if (PostEffects != null)
                {
                    // TODO: output in proper renderTarget location according to viewport
                    PostEffects.Draw(drawContext, renderTarget, currentDepthStencil, currentRenderTarget);
                }
            }
        }

        private Texture ResolveDepthAsSRV(RenderDrawContext context)
        {
            if (!BindDepthAsResourceDuringTransparentRendering)
                return null;

            using (context.PushRenderTargetsAndRestore())
            {
                var depthStencilSRV = context.Resolver.ResolveDepthStencil(context.CommandList.DepthStencilBuffer);

                var renderView = context.RenderContext.RenderView;

                foreach (var renderFeature in context.RenderContext.RenderSystem.RenderFeatures)
                {
                    if (!(renderFeature is RootEffectRenderFeature))
                        continue;

                    var depthLogicalKey = ((RootEffectRenderFeature)renderFeature).CreateViewLogicalGroup("Depth");
                    var viewFeature = renderView.Features[renderFeature.Index];

                    // Copy ViewProjection to PerFrame cbuffer
                    foreach (var viewLayout in viewFeature.Layouts)
                    {
                        var resourceGroup = viewLayout.Entries[renderView.Index].Resources;

                        var depthLogicalGroup = viewLayout.GetLogicalGroup(depthLogicalKey);
                        if (depthLogicalGroup.Hash == ObjectId.Empty)
                            continue;

                        // Might want to use ProcessLogicalGroup if more than 1 Recource
                        resourceGroup.DescriptorSet.SetShaderResourceView(depthLogicalGroup.DescriptorSlotStart, depthStencilSRV);
                    }
                }

                depthStencilROCached = context.Resolver.GetDepthStencilAsRenderTarget(context.CommandList.DepthStencilBuffer, depthStencilROCached);
                context.CommandList.SetRenderTargets(depthStencilROCached, context.CommandList.RenderTargetCount, context.CommandList.RenderTargets);

                return depthStencilSRV;
            }
        }
    }
}