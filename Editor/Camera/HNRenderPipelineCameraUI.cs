using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;

namespace HN.HNRP.Editor
{
    using CED = CoreEditorDrawer<HNRenderPipelineSerializedCamera>;

    public class HNRenderPipelineCameraUI
    {
        private static readonly ExpandedState<Expandable, Camera> expandedState = new(Expandable.Projection, "HNRP");
        private static readonly AdditionalPropertiesState<ExpandableAdditional, Camera> expandedAdditionalState = new(0, "HNRP");


        //public static readonly CED.IDrawer ProjectionSettings = CED.FoldoutGroup(
        //    CameraUI.Styles.projectionSettingsHeaderContent,
        //    Expandable.Projection,
        //    expandedState,
        //    FoldoutOption.Indent,
        //    CED.Group(
        //        CameraUI.Drawer_Projection
        //        )
        //    );

        //public static readonly CED.IDrawer RenderingSettings = CED.AdditionalPropertiesFoldoutGroup(
        //    CameraUI.Rendering.Styles.header,
        //    Expandable.Rendering,
        //    expandedState,
        //    ExpandableAdditional.Rendering,
        //    expandedAdditionalState,
        //    CED.Group(
        //        CameraUI.Rendering.Drawer_Rendering_StopNaNs
        //        ),
        //    CED.Group(
        //        CameraUI.Rendering.Drawer_Rendering_Dithering
        //        )
        //    );

        //public static readonly CED.IDrawer OutputSettings = CED.FoldoutGroup(
        //    CameraUI.Output.Styles.header,
        //    Expandable.Output,
        //    expandedState,
        //    FoldoutOption.Indent,
        //    CED.Group(
        //        CameraUI.Output.Drawer_Output_AllowDynamicResolution
        //        )
        //    );

        //public static CED.IDrawer[] Inspector =
        //{
        //    ProjectionSettings(),
        //    RenderingSettings(),
        //    OutputSettings(),
        //};


        public static CED.IDrawer[] Inspector()
        {
            return new CED.IDrawer[]
            {
                RenderGraphViewSettings(),
                ProjectionSettings(),
                //RenderingSettings(),
                OutputSettings(),
            };
        }

        public static CED.IDrawer RenderGraphViewSettings()
        {
            return CED.FoldoutGroup(
                Styles.renderGraphView,
                Expandable.RenderGraphView,
                expandedState,
                FoldoutOption.Indent,
                CED.Group(DrawRenderGraphView)
            );
        }

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

        public static CED.IDrawer RenderingSettings()
        {
            return CED.AdditionalPropertiesFoldoutGroup(
            CameraUI.Rendering.Styles.header,
            Expandable.Rendering,
            expandedState,
            ExpandableAdditional.Rendering,
            expandedAdditionalState,
            CED.Group(
                CameraUI.Rendering.Drawer_Rendering_StopNaNs
                ),
            CED.Group(
                CameraUI.Rendering.Drawer_Rendering_Dithering
                )
            );
        }

        public static CED.IDrawer OutputSettings()
        {
            return CED.FoldoutGroup(
            CameraUI.Output.Styles.header,
            Expandable.Output,
            expandedState,
            FoldoutOption.Indent,
            CED.Group(
                CameraUI.Output.Drawer_Output_AllowDynamicResolution
                )
            );
        }


        private static void DrawRenderGraphView(HNRenderPipelineSerializedCamera p, UnityEditor.Editor owner)
        {
            if(owner is HNRenderPipelineCameraEditor cameraEditor)
            {
                cameraEditor.DrawRenderGraphView();
            }
        }



        public enum Expandable
        {
            RenderGraphView = 1 << 0,
            Projection = 1 << 1,
            Output = 1 << 3,

            Rendering = 1 << 6,
        }

        public enum ExpandableAdditional
        {
            Rendering = 1 << 0,
        }


        public class Styles
        {
            public static GUIContent antialiasing = EditorGUIUtility.TrTextContent("Anti-aliasing");
            public static GUIContent renderGraphView = EditorGUIUtility.TrTextContent("Render Graph View", "Chose render graph view's name in HNRenderPipelineAsset runtime render graph views.");
        }


    }




}
