using HN.HNRP;
using UnityEditor;
using UnityEngine;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// <see cref="ShadowCameraAttribute"/> 的自定义绘制器。
    /// 绘制相机侧级联阴影设置：级数、级联分割滑条与更新模式 / 更新帧间隔。
    /// </summary>
    /// <remarks>
    /// 这里完全基于 <see cref="PropertyDrawer"/> 传入的 <c>position</c> 做绝对布局，
    /// 不使用 GUILayout / BeginArea —— 因为该 drawer 位于 CoreEditorDrawer 的 foldout
    /// 布局组内，BeginArea 的坐标空间不匹配会导致内容被画到面板左上角。
    /// </remarks>
    [CustomPropertyDrawer(typeof(ShadowCameraAttribute))]
    public class ShadowCameraDrawer : PropertyDrawer
    {
        private const int MaxCascadeCount = ShadowCameraSettings.MaxCascadeCount;

        private const float SliderHandleRowHeight = 13f;
        private const float SliderBarHeight = 28f;
        private const float SliderBarMargin = 2f;
        private const float SliderBottomMargin = 15f;
        private const float SliderTotalHeight = SliderHandleRowHeight + SliderBarHeight + SliderBarMargin * 2f + SliderBottomMargin;

        private const float HandleWidth = 12f;
        private const float HandleHeight = 16f;
        private const float PartitionWidth = 2f;
        private const float InspectorLabelWidth = 96f;

        /// <summary>8 级分组交界轨道的高度（仅在 cascade 级数为 8 时占用）。</summary>
        private const float GroupTrackHeight = 16f;

        /// <summary>分组交界节点的宽度。</summary>
        private const float GroupNodeWidth = 10f;

        /// <summary>分组交界节点的高度。</summary>
        private const float GroupNodeHeight = 12f;

        private static readonly string[] CascadeCountNames = { "1", "2", "4", "8" };
        private static readonly int[] CascadeCountValues = { 1, 2, 4, 8 };

        private static readonly string[] UpdateModeNames = { "Every Frame", "On Demand", "Custom" };
        private static readonly int[] UpdateModeValues = { 0, 1, 2 };

        // 8 级级联配色，扩展自 CoreRP 的 ShadowCascadeGUI。
        private static readonly Color[] CascadeColors =
        {
            new Color(0.50f, 0.50f, 0.70f, 1.0f),
            new Color(0.50f, 0.70f, 0.50f, 1.0f),
            new Color(0.70f, 0.70f, 0.50f, 1.0f),
            new Color(0.70f, 0.50f, 0.50f, 1.0f),
            new Color(0.50f, 0.65f, 0.75f, 1.0f),
            new Color(0.65f, 0.50f, 0.75f, 1.0f),
            new Color(0.60f, 0.60f, 0.60f, 1.0f),
            new Color(0.75f, 0.60f, 0.50f, 1.0f)
        };

        private static readonly Color SelectionColor = new Color(1.0f, 0.85f, 0.1f, 1.0f);

        private static readonly GUIStyle SliderBackgroundStyle = new GUIStyle("LODSliderRange");
        private static readonly GUIStyle CenteredLabelStyle = new GUIStyle(EditorStyles.whiteMiniLabel)
        {
            alignment = TextAnchor.MiddleCenter
        };

        private static readonly int SliderControlHash = "HNRP.ShadowCameraSlider".GetHashCode();
        private static readonly int GroupTrackControlHash = "HNRP.ShadowCameraGroupTrack".GetHashCode();

        // 拖拽状态在重绘之间保持。
        private static int activeHandle = -1;
        private static int activeGroupNode = -1;

        private static GUIStyle downSnatchStyle;

        /// <inheritdoc />
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!HasSerializedChildren(property))
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            float line = EditorGUIUtility.singleLineHeight;
            float space = EditorGUIUtility.standardVerticalSpacing;

            float height = line;                       // Cascade Count
            height += space + line;                    // Update Mode
            height += space + SliderTotalHeight;       // 级联分割滑条

            if (GetCascadeCount(property) == (int)CascadeCountType.Eight)
            {
                height += space + GroupTrackHeight;    // 8 级分组交界轨道
            }

            if (GetSelectedCascade(property) > 0)
            {
                height += space + line;                // Near Boundary
            }
            height += space + line;                    // Far Boundary

            SerializedProperty updateModeProperty = property.FindPropertyRelative("shadowUpdateMode");
            if (updateModeProperty != null
                && updateModeProperty.intValue == (int)ShadowUpdateModeType.Custom)
            {
                height += space + line;                // Update Interval
            }

            return height + 2f;
        }

        /// <inheritdoc />
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty countProperty = property.FindPropertyRelative("cascadeCount");
            SerializedProperty splitsProperty = property.FindPropertyRelative("cascadeSplits");
            SerializedProperty updateModeProperty = property.FindPropertyRelative("shadowUpdateMode");
            SerializedProperty timeSlicesProperty = property.FindPropertyRelative("cascadeTimeSlices");

            if (!HasSerializedChildren(property)
                || splitsProperty.arraySize < MaxCascadeCount
                || timeSlicesProperty.arraySize < MaxCascadeCount)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            int cascadeCount = NormalizeCascadeCount(countProperty.intValue);

            float line = EditorGUIUtility.singleLineHeight;
            float space = EditorGUIUtility.standardVerticalSpacing;
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = InspectorLabelWidth;

            float y = position.y;

            DrawCascadeCount(new Rect(position.x, y, position.width, line), countProperty);
            y += line + space;

            DrawUpdateMode(new Rect(position.x, y, position.width, line), updateModeProperty);
            y += line + space;

            int selectedCascade = DrawCascadeSlider(
                new Rect(position.x, y, position.width, SliderTotalHeight),
                splitsProperty,
                cascadeCount,
                property.propertyPath);
            y += SliderTotalHeight + space;

            // 分组交界轨道：仅 8 级（合并为 4 组）时需要，其余级数每级自成一组。
            SerializedProperty groupsProperty = property.FindPropertyRelative("cascadeSplitGroups");
            if (cascadeCount == (int)CascadeCountType.Eight
                && groupsProperty != null
                && groupsProperty.arraySize == ShadowCameraSettings.GroupNodeCount)
            {
                DrawGroupTrack(
                    new Rect(position.x, y, position.width, GroupTrackHeight),
                    groupsProperty,
                    splitsProperty,
                    cascadeCount);
                y += GroupTrackHeight + space;
            }

            // 级联信息按需显示：不需要时直接隐藏，而不是灰显。
            if (selectedCascade > 0)
            {
                DrawNearBoundary(
                    new Rect(position.x, y, position.width, line),
                    splitsProperty, cascadeCount, selectedCascade);
                y += line + space;
            }

            DrawFarBoundary(
                new Rect(position.x, y, position.width, line),
                splitsProperty, cascadeCount, selectedCascade);
            y += line + space;

            if (updateModeProperty.intValue == (int)ShadowUpdateModeType.Custom)
            {
                DrawUpdateInterval(
                    new Rect(position.x, y, position.width, line),
                    timeSlicesProperty, selectedCascade);
                y += line + space;
            }

            EditorGUIUtility.labelWidth = previousLabelWidth;
        }

        private static bool HasSerializedChildren(SerializedProperty property)
        {
            return property.FindPropertyRelative("cascadeCount") != null
                && property.FindPropertyRelative("cascadeSplits") != null
                && property.FindPropertyRelative("shadowUpdateMode") != null
                && property.FindPropertyRelative("cascadeTimeSlices") != null;
        }

        private static void DrawCascadeCount(Rect rowRect, SerializedProperty countProperty)
        {
            EditorGUI.BeginChangeCheck();
            int newCount = EditorGUI.IntPopup(
                EditorGUI.PrefixLabel(rowRect, Styles.CascadeCount),
                NormalizeCascadeCount(countProperty.intValue),
                CascadeCountNames,
                CascadeCountValues);
            if (EditorGUI.EndChangeCheck())
            {
                countProperty.intValue = newCount;
            }
        }

        private static void DrawUpdateMode(Rect rowRect, SerializedProperty updateModeProperty)
        {
            EditorGUI.BeginChangeCheck();
            int mode = EditorGUI.IntPopup(
                EditorGUI.PrefixLabel(rowRect, Styles.UpdateMode),
                updateModeProperty.intValue,
                UpdateModeNames,
                UpdateModeValues);
            if (EditorGUI.EndChangeCheck())
            {
                updateModeProperty.intValue = mode;
            }
        }

        private static int DrawCascadeSlider(Rect rowRect, SerializedProperty splitsProperty, int cascadeCount, string propertyPath)
        {
            string selectionKey = "HNRP.ShadowCamera.SelectedCascade." + propertyPath;
            int selected = Mathf.Clamp(SessionState.GetInt(selectionKey, 0), 0, cascadeCount - 1);

            Rect barRect = new Rect(
                rowRect.x + SliderBarMargin,
                rowRect.y + SliderHandleRowHeight,
                rowRect.width - SliderBarMargin * 2f,
                SliderBarHeight);

            float maxDistance = GetSplitValue(splitsProperty, cascadeCount - 1);
            if (maxDistance <= 0f)
            {
                maxDistance = 1f;
            }
            float minGap = Mathf.Max(0.01f, maxDistance * 0.001f);

            float[] splitValues = new float[cascadeCount];
            for (int i = 0; i < cascadeCount; i++)
            {
                splitValues[i] = GetSplitValue(splitsProperty, i);
            }

            Rect[] blockRects = BuildBlockRects(barRect, splitValues, cascadeCount, maxDistance);

            HandleSliderInput(rowRect, barRect, blockRects, splitValues, splitsProperty, cascadeCount, maxDistance, minGap,
                selectionKey, ref selected);

            if (Event.current.type != EventType.Repaint)
            {
                return selected;
            }

            for (int i = 0; i < cascadeCount; i++)
            {
                Color previousBackground = GUI.backgroundColor;
                GUI.backgroundColor = CascadeColors[i % CascadeColors.Length];
                GUI.Box(blockRects[i], GUIContent.none, SliderBackgroundStyle);
                GUI.backgroundColor = previousBackground;

                Color previousColor = GUI.color;
                GUI.color = Color.black;
                GUI.Label(blockRects[i], $"{i}\n{splitValues[i]:F1}m", CenteredLabelStyle);
                GUI.color = previousColor;
            }

            for (int i = 0; i < cascadeCount - 1; i++)
            {
                EditorGUI.DrawRect(
                    new Rect(blockRects[i].xMax - PartitionWidth * 0.5f, barRect.y, PartitionWidth, barRect.height),
                    Color.black);
            }

            DrawSelectionOutline(blockRects[selected]);

            for (int i = 0; i < cascadeCount - 1; i++)
            {
                Rect handleRect = GetHandleRect(blockRects[i].xMax, barRect);
                EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.ResizeHorizontal);
                DrawSnatchHandle(handleRect, CascadeColors[(i + 1) % CascadeColors.Length], activeHandle == i);
            }

            return selected;
        }

        private static void HandleSliderInput(
            Rect rowRect,
            Rect barRect,
            Rect[] blockRects,
            float[] splitValues,
            SerializedProperty splitsProperty,
            int cascadeCount,
            float maxDistance,
            float minGap,
            string selectionKey,
            ref int selected)
        {
            Event currentEvent = Event.current;
            int controlId = GUIUtility.GetControlID(SliderControlHash, FocusType.Passive, barRect);

            int hoveredHandle = -1;
            for (int i = 0; i < cascadeCount - 1; i++)
            {
                if (GetHandleRect(blockRects[i].xMax, barRect).Contains(currentEvent.mousePosition))
                {
                    hoveredHandle = i;
                }
            }

            switch (currentEvent.type)
            {
                case EventType.MouseDown:
                    if (currentEvent.button != 0 || !rowRect.Contains(currentEvent.mousePosition))
                    {
                        break;
                    }

                    if (hoveredHandle >= 0)
                    {
                        GUIUtility.hotControl = controlId;
                        activeHandle = hoveredHandle;
                        selected = hoveredHandle;
                        SessionState.SetInt(selectionKey, selected);
                        currentEvent.Use();
                    }
                    else
                    {
                        int hoveredBlock = HitTestBlock(blockRects, currentEvent.mousePosition);
                        if (hoveredBlock >= 0)
                        {
                            selected = hoveredBlock;
                            SessionState.SetInt(selectionKey, selected);
                            GUI.changed = true;
                            currentEvent.Use();
                        }
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != controlId || activeHandle < 0 || activeHandle >= cascadeCount - 1)
                    {
                        break;
                    }

                    float normalized = (currentEvent.mousePosition.x - barRect.x) / Mathf.Max(1f, barRect.width);
                    float draggedDistance = NormalizedToDistance(normalized, maxDistance);

                    float lower = activeHandle > 0 ? splitValues[activeHandle - 1] : 0f;
                    float upper = splitValues[activeHandle + 1];
                    float value = Mathf.Clamp(draggedDistance, lower + minGap, upper - minGap);

                    splitValues[activeHandle] = value;
                    splitsProperty.GetArrayElementAtIndex(activeHandle).floatValue = value;
                    GUI.changed = true;
                    currentEvent.Use();
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId)
                    {
                        GUIUtility.hotControl = 0;
                        activeHandle = -1;
                        currentEvent.Use();
                    }
                    break;
            }
        }

        /// <summary>
        /// 绘制 8 级 cascade 的分组交界轨道。
        /// 3 个节点只允许落在 cascade 交界处，且保持严格顺序（不可重合）。
        /// </summary>
        private static void DrawGroupTrack(
            Rect rowRect,
            SerializedProperty groupsProperty,
            SerializedProperty splitsProperty,
            int cascadeCount)
        {
            Rect barRect = new Rect(
                rowRect.x + SliderBarMargin,
                rowRect.y,
                rowRect.width - SliderBarMargin * 2f,
                rowRect.height);

            float maxDistance = GetSplitValue(splitsProperty, cascadeCount - 1);
            if (maxDistance <= 0f)
            {
                maxDistance = 1f;
            }

            HandleGroupTrackInput(rowRect, barRect, groupsProperty, splitsProperty, cascadeCount, maxDistance);

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            int nodeCount = ShadowCameraSettings.GroupNodeCount;

            // 组区间着色：用组内首级的配色铺底，直观呈现"哪些级共享剔除结果"。
            int previousBoundary = 0;
            for (int group = 0; group <= nodeCount; group++)
            {
                int endBoundary = group < nodeCount
                    ? GetGroupNodeValue(groupsProperty, group)
                    : cascadeCount;

                float xMin = BoundaryX(barRect, splitsProperty, previousBoundary, maxDistance);
                float xMax = BoundaryX(barRect, splitsProperty, endBoundary, maxDistance);

                Color previousBackground = GUI.backgroundColor;
                GUI.backgroundColor = CascadeColors[Mathf.Clamp(previousBoundary, 0, CascadeColors.Length - 1)];
                GUI.Box(
                    new Rect(xMin, rowRect.y + 1f, Mathf.Max(0f, xMax - xMin), rowRect.height - 2f),
                    GUIContent.none,
                    SliderBackgroundStyle);
                GUI.backgroundColor = previousBackground;

                previousBoundary = endBoundary;
            }

            for (int i = 0; i < nodeCount; i++)
            {
                float x = BoundaryX(barRect, splitsProperty, GetGroupNodeValue(groupsProperty, i), maxDistance);
                Rect nodeRect = GetGroupNodeRect(x, barRect);
                EditorGUIUtility.AddCursorRect(nodeRect, MouseCursor.ResizeHorizontal);
                DrawSnatchHandle(nodeRect, SelectionColor, activeGroupNode == i);
            }
        }

        private static void HandleGroupTrackInput(
            Rect rowRect,
            Rect barRect,
            SerializedProperty groupsProperty,
            SerializedProperty splitsProperty,
            int cascadeCount,
            float maxDistance)
        {
            Event currentEvent = Event.current;
            int controlId = GUIUtility.GetControlID(GroupTrackControlHash, FocusType.Passive, barRect);
            int nodeCount = ShadowCameraSettings.GroupNodeCount;

            int hoveredNode = -1;
            for (int i = 0; i < nodeCount; i++)
            {
                float x = BoundaryX(barRect, splitsProperty, GetGroupNodeValue(groupsProperty, i), maxDistance);
                if (GetGroupNodeRect(x, barRect).Contains(currentEvent.mousePosition))
                {
                    hoveredNode = i;
                }
            }

            switch (currentEvent.type)
            {
                case EventType.MouseDown:
                    if (currentEvent.button != 0
                        || hoveredNode < 0
                        || !rowRect.Contains(currentEvent.mousePosition))
                    {
                        break;
                    }

                    GUIUtility.hotControl = controlId;
                    activeGroupNode = hoveredNode;
                    currentEvent.Use();
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != controlId
                        || activeGroupNode < 0
                        || activeGroupNode >= nodeCount)
                    {
                        break;
                    }

                    float normalized = (currentEvent.mousePosition.x - barRect.x) / Mathf.Max(1f, barRect.width);
                    float draggedDistance = NormalizedToDistance(normalized, maxDistance);
                    int boundary = FindNearestBoundary(splitsProperty, cascadeCount, draggedDistance);

                    // 严格顺序：只能落在相邻节点之间的交界上，不允许重合（重合会产生零宽组）。
                    int lower = activeGroupNode > 0 ? GetGroupNodeValue(groupsProperty, activeGroupNode - 1) : 0;
                    int upper = activeGroupNode < nodeCount - 1
                        ? GetGroupNodeValue(groupsProperty, activeGroupNode + 1)
                        : cascadeCount;

                    SetGroupNodeValue(
                        groupsProperty,
                        activeGroupNode,
                        Mathf.Clamp(boundary, lower + 1, upper - 1));
                    GUI.changed = true;
                    currentEvent.Use();
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId)
                    {
                        GUIUtility.hotControl = 0;
                        activeGroupNode = -1;
                        currentEvent.Use();
                    }
                    break;
            }
        }

        private static int GetGroupNodeValue(SerializedProperty groupsProperty, int index)
        {
            if (groupsProperty.arraySize == 0)
            {
                return 1;
            }

            index = Mathf.Clamp(index, 0, groupsProperty.arraySize - 1);
            int value = groupsProperty.GetArrayElementAtIndex(index).intValue;
            return Mathf.Clamp(value, 1, ShadowCameraSettings.MaxCascadeCount - 1);
        }

        private static void SetGroupNodeValue(SerializedProperty groupsProperty, int index, int value)
        {
            if (index < 0 || index >= groupsProperty.arraySize)
            {
                return;
            }

            groupsProperty.GetArrayElementAtIndex(index).intValue =
                Mathf.Clamp(value, 1, ShadowCameraSettings.MaxCascadeCount - 1);
        }

        /// <summary>
        /// 把 cascade 交界（边界索引 <c>0..级数</c>）映射到轨道 x 坐标。
        /// 边界 <c>b</c> 的位置即第 <c>b-1</c> 级的远边界。
        /// </summary>
        private static float BoundaryX(
            Rect barRect,
            SerializedProperty splitsProperty,
            int boundaryIndex,
            float maxDistance)
        {
            if (boundaryIndex <= 0)
            {
                return barRect.x;
            }

            return barRect.x
                + DistanceToNormalized(GetSplitValue(splitsProperty, boundaryIndex - 1), maxDistance) * barRect.width;
        }

        /// <summary>
        /// 按世界距离找最近的 cascade 交界索引（吸附用）。
        /// </summary>
        private static int FindNearestBoundary(SerializedProperty splitsProperty, int cascadeCount, float distance)
        {
            int nearest = 1;
            float nearestDelta = float.MaxValue;
            for (int boundary = 1; boundary < cascadeCount; boundary++)
            {
                float delta = Mathf.Abs(GetSplitValue(splitsProperty, boundary - 1) - distance);
                if (delta < nearestDelta)
                {
                    nearestDelta = delta;
                    nearest = boundary;
                }
            }

            return nearest;
        }

        private static Rect GetGroupNodeRect(float boundaryX, Rect barRect)
        {
            return new Rect(
                boundaryX - GroupNodeWidth * 0.5f,
                barRect.y + (barRect.height - GroupNodeHeight) * 0.5f,
                GroupNodeWidth,
                GroupNodeHeight);
        }

        private static int GetCascadeCount(SerializedProperty property)
        {
            SerializedProperty countProperty = property.FindPropertyRelative("cascadeCount");
            return countProperty != null ? NormalizeCascadeCount(countProperty.intValue) : 4;
        }

        private static int GetSelectedCascade(SerializedProperty property)
        {
            string selectionKey = "HNRP.ShadowCamera.SelectedCascade." + property.propertyPath;
            int cascadeCount = NormalizeCascadeCount(property.FindPropertyRelative("cascadeCount").intValue);
            return Mathf.Clamp(SessionState.GetInt(selectionKey, 0), 0, cascadeCount - 1);
        }

        private static void DrawNearBoundary(Rect rect, SerializedProperty splitsProperty, int cascadeCount, int selected)
        {
            float maxDistance = GetSplitValue(splitsProperty, cascadeCount - 1);
            float minGap = Mathf.Max(0.01f, maxDistance * 0.001f);

            float nearBoundary = GetSplitValue(splitsProperty, selected - 1);
            float farBoundary = GetSplitValue(splitsProperty, selected);

            EditorGUI.BeginChangeCheck();
            float newNear = EditorGUI.FloatField(rect, Styles.NearBoundary, nearBoundary);
            if (EditorGUI.EndChangeCheck())
            {
                float lower = selected >= 2 ? GetSplitValue(splitsProperty, selected - 2) : 0f;
                newNear = Mathf.Clamp(newNear, lower + minGap, farBoundary - minGap);
                splitsProperty.GetArrayElementAtIndex(selected - 1).floatValue = newNear;
            }
        }

        private static void DrawFarBoundary(Rect rect, SerializedProperty splitsProperty, int cascadeCount, int selected)
        {
            float maxDistance = GetSplitValue(splitsProperty, cascadeCount - 1);
            float minGap = Mathf.Max(0.01f, maxDistance * 0.001f);

            float nearBoundary = selected == 0 ? 0f : GetSplitValue(splitsProperty, selected - 1);
            float farBoundary = GetSplitValue(splitsProperty, selected);

            EditorGUI.BeginChangeCheck();
            float newFar = EditorGUI.FloatField(rect, Styles.FarBoundary, farBoundary);
            if (EditorGUI.EndChangeCheck())
            {
                float upper = selected < cascadeCount - 1 ? GetSplitValue(splitsProperty, selected + 1) : float.MaxValue;
                newFar = Mathf.Clamp(newFar, nearBoundary + minGap, upper - minGap);
                splitsProperty.GetArrayElementAtIndex(selected).floatValue = newFar;
            }
        }

        private static void DrawUpdateInterval(Rect rect, SerializedProperty timeSlicesProperty, int selected)
        {
            int interval = timeSlicesProperty.GetArrayElementAtIndex(selected).intValue;

            EditorGUI.BeginChangeCheck();
            int newInterval = EditorGUI.IntField(rect, Styles.UpdateInterval, interval);
            if (EditorGUI.EndChangeCheck())
            {
                timeSlicesProperty.GetArrayElementAtIndex(selected).intValue = Mathf.Max(0, newInterval);
            }
        }

        private static Rect[] BuildBlockRects(Rect barRect, float[] splitValues, int cascadeCount, float maxDistance)
        {
            Rect[] blockRects = new Rect[cascadeCount];
            float x = barRect.x;

            for (int i = 0; i < cascadeCount; i++)
            {
                float right = i == cascadeCount - 1
                    ? barRect.xMax
                    : barRect.x + DistanceToNormalized(splitValues[i], maxDistance) * barRect.width;

                blockRects[i] = new Rect(x, barRect.y, right - x, barRect.height);
                x = right;
            }

            return blockRects;
        }

        /// <summary>
        /// 把世界空间距离映射到 [0,1] 的滑条位置。
        /// 采用对数映射，使近处级联占据更大宽度，避免近处几级在滑条上重叠。
        /// </summary>
        private static float DistanceToNormalized(float distance, float maxDistance)
        {
            float denominator = Mathf.Log(1f + maxDistance);
            if (denominator <= 0f)
            {
                return 0f;
            }

            return Mathf.Log(1f + Mathf.Clamp(distance, 0f, maxDistance)) / denominator;
        }

        /// <summary>
        /// <see cref="DistanceToNormalized"/> 的逆运算，把滑条位置还原成世界空间距离。
        /// </summary>
        private static float NormalizedToDistance(float normalized, float maxDistance)
        {
            return Mathf.Exp(Mathf.Clamp01(normalized) * Mathf.Log(1f + maxDistance)) - 1f;
        }

        private static int HitTestBlock(Rect[] blockRects, Vector2 mousePosition)
        {
            for (int i = 0; i < blockRects.Length; i++)
            {
                if (blockRects[i].Contains(mousePosition))
                {
                    return i;
                }
            }
            return -1;
        }

        private static Rect GetHandleRect(float boundaryX, Rect barRect)
        {
            return new Rect(
                boundaryX - HandleWidth * 0.5f,
                barRect.y - SliderHandleRowHeight + 1f,
                HandleWidth,
                HandleHeight);
        }

        private static void DrawSelectionOutline(Rect rect)
        {
            const float thickness = 2f;
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), SelectionColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), SelectionColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), SelectionColor);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), SelectionColor);
        }

        private static void DrawSnatchHandle(Rect rect, Color color, bool active)
        {
            GUIStyle style = GetDownSnatchStyle();
            Color previousColor = GUI.color;
            GUI.color = color * (active ? 1.4f : 1.0f);
            style.Draw(rect, false, false, false, false);
            GUI.color = previousColor;
        }

        private static GUIStyle GetDownSnatchStyle()
        {
            if (downSnatchStyle == null)
            {
                Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Packages/com.unity.render-pipelines.core/Editor/Lighting/Icons/DownSnatch.png");
                Texture2D focused = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Packages/com.unity.render-pipelines.core/Editor/Lighting/Icons/DownSnatchFocused.png");

                downSnatchStyle = new GUIStyle
                {
                    normal = { background = normal },
                    hover = { background = normal },
                    focused = { background = focused }
                };
            }

            return downSnatchStyle;
        }

        private static float GetSplitValue(SerializedProperty splitsProperty, int index)
        {
            index = Mathf.Clamp(index, 0, splitsProperty.arraySize - 1);
            return splitsProperty.GetArrayElementAtIndex(index).floatValue;
        }

        private static int NormalizeCascadeCount(int rawValue)
        {
            return rawValue == 1 || rawValue == 2 || rawValue == 4 || rawValue == 8 ? rawValue : 4;
        }

        private static class Styles
        {
            public static readonly GUIContent CascadeCount = new GUIContent("Cascade Count");
            public static readonly GUIContent UpdateMode = new GUIContent("Update Mode");
            public static readonly GUIContent NearBoundary = new GUIContent("Near Boundary");
            public static readonly GUIContent FarBoundary = new GUIContent("Far Boundary");
            public static readonly GUIContent UpdateInterval = new GUIContent("Update Interval");
        }
    }
}
