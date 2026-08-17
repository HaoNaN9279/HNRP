using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;

namespace HN.HNRP.Editor
{
    using CED = CoreEditorDrawer<HNRenderPipelineSerializedCamera>;

    public class HNRenderPipelineCameraUI
    {
        public static CED.IDrawer[] Inspector()
        {
            return new CED.IDrawer[]
            {
                ProjectionSettings(),
                RenderingSettings(),
                EnvironmentSettings(),
                OutputSettings(),
            };
        }


#region Projection
        public static CED.IDrawer ProjectionSettings()
        {
            return CED.FoldoutGroup(
            CameraUI.Styles.projectionSettingsHeaderContent,
            Expandable.Projection,
            expandedState,
            FoldoutOption.Indent,
            CED.Group(
                CameraUI.Drawer_Projection
                )
            );
        }
#endregion


#region Rendering
        public static CED.IDrawer RenderingSettings()
        {
            return CED.AdditionalPropertiesFoldoutGroup(
            CameraUI.Rendering.Styles.header,
            Expandable.Rendering,
            expandedState,
            ExpandableAdditional.Rendering,
            expandedAdditionalState,
            CED.Group(
                CameraUI.Rendering.Drawer_Rendering_StopNaNs,
                CameraUI.Rendering.Drawer_Rendering_Dithering,
                CameraUI.Rendering.Drawer_Rendering_CullingMask,
                CameraUI.Rendering.Drawer_Rendering_OcclusionCulling
                ),
            CED.noop
            );
        }
#endregion


#region Environment
        public static CED.IDrawer EnvironmentSettings()
        {
            return CED.FoldoutGroup(
                CameraUI.Environment.Styles.header,
                Expandable.Environment,
                expandedState,
                FoldoutOption.Indent,
                CED.Group(
                    DrawEnvironmentClearFlags,
                    CameraUI.Environment.Drawer_Environment_VolumeLayerMask
                )
            );
        }

        private static void DrawEnvironmentClearFlags(HNRenderPipelineSerializedCamera p, UnityEditor.Editor owner)
        {
            
        }
#endregion


#region Output
        public static CED.IDrawer OutputSettings()
        {
            return CED.FoldoutGroup(
            CameraUI.Output.Styles.header,
            Expandable.Output,
            expandedState,
            FoldoutOption.Indent,
            CED.Group(
                CameraUI.Output.Drawer_Output_AllowDynamicResolution,
                CameraUI.Output.Drawer_Output_NormalizedViewPort,
                CameraUI.Output.Drawer_Output_Depth,
                CameraUI.Output.Drawer_Output_RenderTarget
                )
            );
        }
#endregion

        private static readonly ExpandedState<Expandable, Camera> expandedState = new(Expandable.Projection, "HNRP");
        private static readonly AdditionalPropertiesState<ExpandableAdditional, Camera> expandedAdditionalState = new(0, "HNRP");


        public enum Expandable
        {
            Projection = 1 << 1,
            Output = 1 << 3,
            Rendering = 1 << 4,
            Environment = 1 << 5,
        }

        public enum ExpandableAdditional
        {
            Rendering = 1 << 0,
        }


        public class Styles
        {
        }


    }




}
