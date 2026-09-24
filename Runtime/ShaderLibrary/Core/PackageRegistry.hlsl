#ifndef HNRP_PACKAGE_REGISTRY_INCLUDED
#define HNRP_PACKAGE_REGISTRY_INCLUDED

// 组合头（Generated/*.hlsl）会在包含基底之前定义实际的包 varyings 浮点总数；
// 无包 / 未生成时兜底为 0（方案 4.3）。
#ifndef VARYINGS_FLOAT_COUNT_PACKAGES
    #define VARYINGS_FLOAT_COUNT_PACKAGES 0
#endif

#include "PackageAnchors.hlsl"

#endif
