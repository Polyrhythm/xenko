﻿// <auto-generated>
// Do not edit this file yourself!
//
// This code was generated by Xenko Shader Mixin Code Generator.
// To generate it yourself, please install SiliconStudio.Xenko.VisualStudio.Package .vsix
// and re-save the associated .xkfx.
// </auto-generated>

using System;
using SiliconStudio.Core;
using SiliconStudio.Xenko.Rendering;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.Shaders;
using SiliconStudio.Core.Mathematics;
using Buffer = SiliconStudio.Xenko.Graphics.Buffer;

namespace SiliconStudio.Xenko.Rendering.Shadows
{
    internal static partial class ShadowMapReceiverPointParaboloidKeys
    {
        public static readonly ValueParameterKey<Matrix> View = ParameterKeys.NewValue<Matrix>();
        public static readonly ValueParameterKey<Vector2> FaceOffset = ParameterKeys.NewValue<Vector2>();
        public static readonly ValueParameterKey<Vector2> BackfaceOffset = ParameterKeys.NewValue<Vector2>();
        public static readonly ValueParameterKey<Vector2> FaceSize = ParameterKeys.NewValue<Vector2>();
        public static readonly ValueParameterKey<float> DepthBiases = ParameterKeys.NewValue<float>();
        public static readonly ValueParameterKey<Vector2> DepthParameters = ParameterKeys.NewValue<Vector2>();
    }
}
