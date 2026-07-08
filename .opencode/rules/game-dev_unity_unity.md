# unity 项目全局规则

这些规则适用于任何 unity 项目。

## 参考文档

### unity 文档

遇到不清楚的 unity 机制，优先查询文档，不要随意猜测。
默认使用 unity 2022.3 版本，如果使用其他版本的 unity 切换到对应版本的文档：
- unity 手册地址：https://docs.unity3d.com/2022.3/Documentation/Manual/index.html
- unity API文档地址：https://docs.unity3d.com/2022.3/Documentation/ScriptReference/index.html
- unity cs reference：https://github.com/Unity-Technologies/UnityCsReference/tree/2022.3

## MCP 工具使用

- 任何 unity 中的操作优先使用 unity MCP ，如果该 MCP 工具无法实现，则先向用户汇报，再尝试其他方法。
- 需要对修改进行测试时，使用 unity MCP 工具来触发编译、调用Editor工具、读取Console信息来实现测试循环。
- 任何创建移动删除unity脚本，都通过 MCP 在unity内操作，不得使用bash命令直接在目录文件夹下操作。
- 任何创建移动删除 Assembly Definition Asset 的操作，都通过 MCP 在unity内操作，不得使用bash命令直接在目录文件夹下操作。
- 任何创建移动删除unity资源的操作，都通过 MCP 在unity内操作，不得使用bash命令直接在目录文件夹下操作。
- 绝对禁止自行关闭unity进程，如果有需要，向用户提需求。
- 绝对禁止私自下载任何插件或克隆任何项目，如果有需要，向用户提需求。