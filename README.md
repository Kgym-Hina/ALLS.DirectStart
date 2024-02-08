# ALLS.DirectStart
帮助直播机台自动开启B站直播的小程序

## 配置

```cs
public class StartConfig
{
    // 分区ID
    public int AreaId { get; set; }
    // 直播间号
    public int RoomId { get; set; }
    // 父分区ID
    public int ParentAreaId { get; set; }
    // Cookies 整段粘贴进来即可
    public string Cookie { get; set; }
    // OBS 连接信息
    public string OBSWebsocketUrl { get; set; }
    public string OBSWebsocketPassword { get; set; }
    // 开播使用协议： 0: RTMP, 1: SRT
    public int StreamURLType { get; set; }

    // 无用的提示
    public string Description { get; set; }
}
```

初次运行会在工作目录下生成默认配置文件 `Config.json` 并尝试启动，关闭程序自行编辑即可
