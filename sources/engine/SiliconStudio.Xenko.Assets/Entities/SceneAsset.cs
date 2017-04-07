﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.Collections.Generic;
using System.ComponentModel;
using SiliconStudio.Assets;
using SiliconStudio.Core;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Engine;

namespace SiliconStudio.Xenko.Assets.Entities
{
    /// <summary>
    /// A scene asset.
    /// </summary>
    [DataContract("SceneAsset")]
    [AssetDescription(FileSceneExtension, AllowArchetype = false)]
    [AssetContentType(typeof(Scene))]
#if SILICONSTUDIO_XENKO_SUPPORT_BETA_UPGRADE
    [AssetFormatVersion(XenkoConfig.PackageName, CurrentVersion, "1.9.0-beta06")]
    [AssetUpgrader(XenkoConfig.PackageName, "1.9.0-beta06", "1.10.0-beta01", typeof(RemoveSceneSettingsUpgrader))]
    [AssetUpgrader(XenkoConfig.PackageName, "1.10.0-beta01", "1.10.0-beta02", typeof(MoveRenderGroupInsideComponentUpgrader))]
    [AssetUpgrader(XenkoConfig.PackageName, "1.10.0-beta02", "1.10.0-beta03", typeof(FixPartReferenceUpgrader))]
    [AssetUpgrader(XenkoConfig.PackageName, "1.10.0-beta03", "2.0.0.0", typeof(EmptyAssetUpgrader))]
#else
    [AssetFormatVersion(XenkoConfig.PackageName, CurrentVersion, "2.0.0.0")]
#endif
    [Display(2000, "Scene")]
    public partial class SceneAsset : EntityHierarchyAssetBase
    {
        private const string CurrentVersion = "2.0.0.0";

        public const string FileSceneExtension = ".xkscene;.pdxscene";

        /// <summary>
        /// A collection of identifier of all the children of this scene..
        /// </summary>
        [DataMember(10)]
        [Display(Browsable = false)]
        [NonIdentifiableCollectionItems]
        [NotNull]
        public List<AssetId> ChildrenIds { get; } = new List<AssetId>();

        /// <summary>
        /// The parent scene.
        /// </summary>
        /// <userdoc>The parent scene.</userdoc>
        [DataMember(20)]
        [Display(Browsable = false)] // TODO: make it visible in the property grid, but readonly.
        [DefaultValue(null)]
        public Scene Parent { get; set; }

        /// <summary>
        /// The translation offset relative to the <see cref="Parent"/> scene.
        /// </summary>
        /// <userdoc>The translation offset of the scene with regard to its parent scene, if any.</userdoc>
        [DataMember(30)]
        public Vector3 Offset { get; set; }
    }
}
