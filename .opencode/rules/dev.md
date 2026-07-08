# 项目开发注意点

- 当前项目根目录链接到一个 unity 工程的 Aseets/ 目录下，修改项目文件会同步修改unity工程中的文件。
- unity工程目录："d:/Unity Project/RPTest/"。
- 该unity工程中已经接入 unity MCP，需要操作 unity 时，可以通过mcp操作unity。

## 依赖包路径

RenderPipelineCore: "D:/Unity Project/RPTest/Packages/com.unity.render-pipelines.core@14.0.11"
ShaderGraph: "D:/Unity Project/URPTest/Packages/com.unity.shadergraph@14.0.11"
当需要参考URP和HDRP的源码时：
URP: 
    - "D:/Unity Project/URPTest/Packages/com.unity.render-pipelines.universal@14.0.11"
    - "D:/Unity Project/URPTest/Packages/com.unity.render-pipelines.universal-config@14.0.10"
HDRP:
    - "D:/Unity Project/HDRPTest/Packages/com.unity.render-pipelines.high-definition@14.0.11"
    - "D:/Unity Project/HDRPTest/Packages/com.unity.render-pipelines.high-definition-config@14.0.11"