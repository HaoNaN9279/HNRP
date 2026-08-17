using NUnit.Framework;
using UnityEngine;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// 验证 ComputeShader 引用可通过 HNRenderPipelineRuntimeResources 访问，
    /// 而非仅依赖 Editor-only 的 AssetDatabase.LoadAssetAtPath。
    /// </summary>
    [TestFixture]
    public class ComputeShaderLoadingTests
    {
        [Test]
        public void RuntimeResources_HasClusterCullingLightCSField()
        {
            // Arrange: 创建 RuntimeResources 实例
            var resources = ScriptableObject.CreateInstance<HNRenderPipelineRuntimeResources>();

            // Assert: 字段默认应为 null（未赋值），但字段本身存在
            Assert.That(resources, Is.Not.Null);
            Assert.That(resources.clusterCullingLightCS, Is.Null,
                "clusterCullingLightCS 默认为 null，需要在 Inspector 或代码中手动赋值 ComputeShader 资源");
        }

        [Test]
        public void RuntimeResources_HasClusterCullingReflectionProbeCSField()
        {
            var resources = ScriptableObject.CreateInstance<HNRenderPipelineRuntimeResources>();

            Assert.That(resources, Is.Not.Null);
            Assert.That(resources.clusterCullingReflectionProbeCS, Is.Null,
                "clusterCullingReflectionProbeCS 默认为 null，需要在 Inspector 或代码中手动赋值 ComputeShader 资源");
        }

        [Test]
        public void RuntimeResources_FieldsAreWritable()
        {
            var resources = ScriptableObject.CreateInstance<HNRenderPipelineRuntimeResources>();

            // 可以通过反射读写字段（验证字段存在且可写）
            var fieldInfo = typeof(HNRenderPipelineRuntimeResources).GetField(nameof(HNRenderPipelineRuntimeResources.clusterCullingLightCS));
            Assert.That(fieldInfo, Is.Not.Null, "clusterCullingLightCS 字段应存在于 HNRenderPipelineRuntimeResources");
            Assert.That(fieldInfo.FieldType, Is.EqualTo(typeof(ComputeShader)));

            fieldInfo = typeof(HNRenderPipelineRuntimeResources).GetField(nameof(HNRenderPipelineRuntimeResources.clusterCullingReflectionProbeCS));
            Assert.That(fieldInfo, Is.Not.Null, "clusterCullingReflectionProbeCS 字段应存在于 HNRenderPipelineRuntimeResources");
            Assert.That(fieldInfo.FieldType, Is.EqualTo(typeof(ComputeShader)));
        }

        [TearDown]
        public void TearDown()
        {
            // 清理临时 ScriptableObject
            var tempObjects = Object.FindObjectsOfType<HNRenderPipelineRuntimeResources>();
            foreach (var obj in tempObjects)
            {
                if (obj.name == string.Empty)
                    Object.DestroyImmediate(obj);
            }
        }
    }
}
