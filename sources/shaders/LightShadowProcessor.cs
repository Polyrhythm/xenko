﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System.Collections.Generic;

using SiliconStudio.Core;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Engine;
using SiliconStudio.Paradox.EntityModel;
using SiliconStudio.Paradox.Graphics;

namespace SiliconStudio.Paradox.Effects.Modules
{
    public abstract class LightShadowProcessor : EntityProcessor<EntityLightShadow>
    {
        public static readonly Logger Logger = GlobalLogger.GetLogger("LightShadowProcessor");

        #region Protected members

        protected readonly GraphicsDevice GraphicsDevice;

        protected readonly bool ManageShadows;

        protected readonly HashSet<ShadowMapTexture> InternalShadowMapTextures;

        protected readonly HashSet<ShadowMapTexture> InternalActiveShadowMapTextures;

        protected readonly HashSet<ShadowMap> InternalShadowMaps;

        protected readonly HashSet<ShadowMap> InternalActiveShadowMaps;

        #endregion

        #region Public properties

        public Dictionary<Entity, EntityLightShadow> Lights
        {
            get { return enabledEntities; }
        }

        public virtual HashSet<ShadowMapTexture> ShadowMapTextures
        {
            get { return InternalShadowMapTextures; }
        }

        public virtual HashSet<ShadowMapTexture> ActiveShadowMapTextures
        {
            get { return InternalActiveShadowMapTextures; }
        }

        public virtual HashSet<ShadowMap> ActiveShadowMaps
        {
            get { return InternalActiveShadowMaps; }
        }

        #endregion

        #region Protected methods

        protected override void OnSystemRemove()
        {
            foreach (var shadowMap in InternalShadowMaps)
                shadowMap.Texture = null;
            InternalShadowMaps.Clear();

            foreach (var texture in InternalShadowMapTextures)
            {
                InternalShadowMapTextures.Remove(texture);
                Utilities.Dispose(ref texture.ShadowMapDepthBuffer);
                Utilities.Dispose(ref texture.ShadowMapDepthTexture);
                Utilities.Dispose(ref texture.ShadowMapRenderTarget);
                Utilities.Dispose(ref texture.ShadowMapTargetTexture);
                Utilities.Dispose(ref texture.IntermediateBlurRenderTarget);
                Utilities.Dispose(ref texture.IntermediateBlurTexture);
            }
            InternalShadowMapTextures.Clear();

            base.OnSystemRemove();
        }

        protected LightShadowProcessor(GraphicsDevice device, bool manageShadows)
            : base(new PropertyKey[] { LightComponent.Key })
        {
            GraphicsDevice = device;
            ManageShadows = manageShadows;
            InternalShadowMapTextures = new HashSet<ShadowMapTexture>();
            InternalActiveShadowMapTextures = new HashSet<ShadowMapTexture>();
            InternalShadowMaps = new HashSet<ShadowMap>();
            InternalActiveShadowMaps = new HashSet<ShadowMap>();
        }

        protected override EntityLightShadow GenerateAssociatedData(Entity entity)
        {
            return new EntityLightShadow { Entity = entity, Light = entity.Get(LightComponent.Key), ShadowMap = null };
        }

        #endregion

        #region Protected static methods

        protected static void UpdateEntityLightShadow(EntityLightShadow light)
        {
            var worldDir = Vector4.Transform(new Vector4(light.Light.LightDirection, 0), light.Entity.Transformation.WorldMatrix);
            light.ShadowMap.LightDirection = new Vector3(worldDir.X, worldDir.Y, worldDir.Z);
            light.ShadowMap.ReceiverInfo.ShadowLightDirection = light.ShadowMap.LightDirectionNormalized;
            light.ShadowMap.ShadowFarDistance = light.Light.ShadowFarDistance;
            light.ShadowMap.ReceiverInfo.ShadowMapDistance = light.ShadowMap.ShadowFarDistance;
            light.ShadowMap.ReceiverVsmInfo.BleedingFactor = light.Light.BleedingFactor;
            light.ShadowMap.ReceiverVsmInfo.MinVariance = light.Light.MinVariance;
        }

        #endregion
    }
}
