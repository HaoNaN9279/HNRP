using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// 包在材质面板中的自定义绘制接口。默认实现按解析出的参数元数据、依类型只绘制相关控件；
    /// 包可提供名为 <c>Package&lt;包名&gt;Editor</c> 的类（继承本类）覆盖默认绘制。
    /// </summary>
    public abstract class PackageGUI
    {
        private const string ClassPrefix = "Package";
        private const string ClassSuffix = "Editor";

        private static readonly Dictionary<string, Type> TypeCache = new Dictionary<string, Type>(StringComparer.Ordinal);

        /// <summary>绘制该包的参数面板。</summary>
        /// <param name="materialEditor">材质编辑器。</param>
        /// <param name="entry">包条目（定义 + 解析内容）。</param>
        /// <param name="properties">材质属性数组。</param>
        public abstract void DrawGUI(
            MaterialEditor materialEditor,
            HNRPPackageEntry entry,
            MaterialProperty[] properties);

        /// <summary>材质校验：按参数值同步关键字。</summary>
        /// <param name="material">目标材质。</param>
        /// <param name="entry">包条目。</param>
        public virtual void OnValidateMaterial(Material material, HNRPPackageEntry entry)
        {
        }

        /// <summary>
        /// 为包解析面板实现：按约定类名 <c>Package&lt;包名&gt;Editor</c> 反射查找，
        /// 未定义时退回声明式实现。
        /// </summary>
        /// <param name="entry">包条目。</param>
        /// <returns>面板实现。</returns>
        public static PackageGUI Resolve(HNRPPackageEntry entry)
        {
            if (entry != null && entry.Definition != null)
            {
                string className = ClassPrefix + entry.Definition.FileStem + ClassSuffix;
                Type type = FindType(className);
                if (type != null && typeof(PackageGUI).IsAssignableFrom(type) && !type.IsAbstract)
                {
                    try
                    {
                        return (PackageGUI)Activator.CreateInstance(type);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning(
                            $"[PackageGUI] 无法实例化自定义面板 '{className}'：{exception.Message}；退回声明式面板。");
                    }
                }
            }

            return new DefaultPackageGUI();
        }

        private static Type FindType(string className)
        {
            if (TypeCache.TryGetValue(className, out Type cached))
            {
                return cached;
            }

            Type found = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException)
                {
                    continue;
                }

                foreach (Type type in types)
                {
                    if (type.IsAbstract || !typeof(PackageGUI).IsAssignableFrom(type))
                    {
                        continue;
                    }

                    if (type.Name == className)
                    {
                        found = type;
                        break;
                    }
                }

                if (found != null)
                {
                    break;
                }
            }

            TypeCache[className] = found;
            return found;
        }
    }

    /// <summary>
    /// 声明式包面板：按参数类型只绘制相关控件。
    /// </summary>
    public sealed class DefaultPackageGUI : PackageGUI
    {
        /// <inheritdoc />
        public override void DrawGUI(
            MaterialEditor materialEditor,
            HNRPPackageEntry entry,
            MaterialProperty[] properties)
        {
            string safeId = entry.SafeId;
            foreach (HNRPPackageParameter parameter in entry.Content.Parameters)
            {
                MaterialProperty property = FindProperty(properties, parameter.GetPropertyName(safeId));
                if (property == null)
                {
                    continue;
                }

                var label = new GUIContent(
                    string.IsNullOrEmpty(parameter.DisplayName) ? parameter.Name : parameter.DisplayName,
                    parameter.Tooltip);

                switch (parameter.Type)
                {
                    case HNRPPackageParameterType.Texture2D:
                    case HNRPPackageParameterType.Cube:
                        // 仅纹理类型显示 tiling / offset。
                        materialEditor.TexturePropertySingleLine(label, property);
                        materialEditor.TextureScaleOffsetProperty(property);
                        break;

                    case HNRPPackageParameterType.Enum:
                        if (parameter.EnumLabels != null && parameter.EnumLabels.Length > 1)
                        {
                            materialEditor.PopupShaderProperty(property, label, parameter.EnumLabels);
                        }
                        else
                        {
                            materialEditor.ShaderProperty(property, label);
                        }

                        break;

                    default:
                        materialEditor.ShaderProperty(property, label);
                        break;
                }
            }
        }

        /// <inheritdoc />
        public override void OnValidateMaterial(Material material, HNRPPackageEntry entry)
        {
            string safeId = entry.SafeId;
            foreach (HNRPPackageParameter parameter in entry.Content.Parameters)
            {
                if (parameter.Type != HNRPPackageParameterType.Toggle)
                {
                    continue;
                }

                string keyword = parameter.GetKeywordName(safeId);
                if (string.IsNullOrEmpty(keyword))
                {
                    continue;
                }

                string propertyName = parameter.GetPropertyName(safeId);
                if (!material.HasProperty(propertyName))
                {
                    continue;
                }

                if (material.GetFloat(propertyName) > 0.5f)
                {
                    material.EnableKeyword(keyword);
                }
                else
                {
                    material.DisableKeyword(keyword);
                }
            }
        }

        private static MaterialProperty FindProperty(MaterialProperty[] properties, string name)
        {
            if (properties == null)
            {
                return null;
            }

            foreach (MaterialProperty property in properties)
            {
                if (property.name == name)
                {
                    return property;
                }
            }

            return null;
        }
    }
}
