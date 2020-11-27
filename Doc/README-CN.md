# Unity-3mx

在Unity中加载Bentley ContextCapture生成的[3MX/3MXB](https://docs.bentley.com/LiveContent/web/ContextCapture%20Help-v9/en/GUID-CED0ABE6-2EE3-458D-9810-D87EC3C521BD.html) LOD模型。
> 对于点云，请使用PointCloudMaterial作为Material Override.

![example](./Doc/example.png)

![example_pointcloud](./Doc/example_pointcloud.png)

## 已验证平台
- PC(Win) Standalone
- WebGL
- Android

## 开发环境:
- Windows 10
- Unity2018.4.23f1

## 相关环境
- [lodToolkit](https://github.com/ProjSEED/lodToolkit): level-of-details toolkit(LTK). 将*osgb*模型树转换为*3mx*模型树。将*ply/las/laz/xyz*格式的点云转换为*3mx/osgb*模型树。
- [osgPlugins-3mx](https://github.com/ProjSEED/osgPlugins-3mx): 在OpenSceneGraph中加载*3mx/3mxb*模型树（支持格网和点云）。

## 第三方库 (source codes included for convenience):
- [OpenCTM](https://github.com/BarryWangYang/OpenCTM-Optimizing-GC-)
- [NanoJpeg](https://github.com/Deathspike/NanoJPEG.NET)
- [Newtonsoft.Json-for-Unity](https://github.com/jilleJr/Newtonsoft.Json-for-Unity)

## 已知问题
- 缓存策略未优化。
