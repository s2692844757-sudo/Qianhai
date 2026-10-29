# 前海 PAD 中控工程

## Scene 修改方式

打开 `Assets/Scenes/SampleScene.unity`。首次导入时，编辑器脚本会在默认空白 SampleScene 中创建一次 UI；之后不会自动重建。所有页面和控件都保存在 Hierarchy 的 `QianhaiControlPad/Canvas` 下，可直接在 Scene / Inspector 中改位置、尺寸、图片和引用。运行时只根据清单刷新缩略图列表。

如需手动初始化，使用 Unity 菜单 `Qianhai > Setup Control Pad Scene`。该菜单仅在已有中控根对象时询问是否重建，并只处理 `QianhaiControlPad` 根对象。

## 六台播放电脑

在 Project 里选中 `Assets/ControlPad/ControlPadConfig.asset`，修改“六台播放电脑”目标的 `host`、`port` 和 `thumbnailManifestUrl`。另在“核心影片播放电脑”单独配置 `host`、`port`；它只接收核心影片的 UDP 控制，不配置缩略图清单。默认端口为 UDP 5005，地址均为占位值，需要按展厅实际网络修改。

PAD 对每台电脑发送 UTF-8 JSON UDP 数据包：

```json
{"command":"play","targetId":"layout-3","assetId":"video-001","value":"","sentAtUnixMs":1793000000000}
```

布局电脑接收 `wake`、`select-layout`、`play` 等布局/素材命令；核心影片电脑使用独立 `targetId: "core-movie"` 接收 `wake`、`play-core`、`pause`、`replay`、`mute`、`volume`、`amplifier`、`language` 等核心影片控制命令。`amplifier` 和 `mute` 以 `value` 字段发送 `true` / `false`；`volume`、`language` 也通过 `value` 传参。核心影片页不会读取或显示缩略图。需要在对应播放电脑端部署 UDP 接收程序，并按相同字段解析；UDP 是无连接协议，PAD 只能确认本机已发出数据，不能据此确认视频已成功播放。

## 缩略图清单

每台电脑需提供可由 PAD 访问的 HTTP/HTTPS JSON URL。例如：

```json
{
  "targetId": "layout-3",
  "items": [
    {
      "id": "video-001",
      "title": "介绍影片",
      "thumbnailUrl": "http://192.168.1.101:8080/thumbs/video-001.jpg",
      "mediaType": "video"
    },
    {
      "id": "image-002",
      "title": "场景图片",
      "thumbnailUrl": "http://192.168.1.101:8080/thumbs/image-002.jpg",
      "mediaType": "image"
    }
  ]
}
```

素材替换后，播放电脑将新缩略图、正式素材加入本机目录，并更新此 JSON 清单。PAD 只下载缩略图；点击“确认播放”时发送清单里的 `id`，播放电脑再用该 ID 查找本机正式素材。PAD 应用不能像本机文件浏览器那样自动列举 SMB 共享目录，所以需要每台播放电脑提供静态 HTTP 服务或能生成上述 JSON 的局域网服务。缩略图和播放电脑均需允许 PAD 所在 Wi-Fi/VLAN 访问。

## 重要配置

- 工程当前目标是 Android，Android 和 iOS build support 均已安装。iOS 构建时工程会自动补充本地网络用途说明和本地 HTTP 访问配置；Android 若使用示例里的 `http://` 清单地址，需要在 Player Settings > Other Settings 中允许 HTTP 下载。若现场服务支持，建议使用 HTTPS。
- 现场播放端需提供 UDP 接收服务；当前工程实现的是发送端，不控制电脑电源，也不直接播放播放端的视频。
- 所有六台电脑和 PAD 应在可互相路由的局域网中；检查防火墙放通目标 UDP 端口及缩略图 HTTP 端口。
- 当前页面采用 1920 × 1080 CanvasScaler 基准，PAD 比例可在 Canvas Scaler 和 RectTransform 锚点中调整。
