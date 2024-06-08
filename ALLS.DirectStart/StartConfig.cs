namespace ALLS.DirectStart;

public class StartConfig
{
    public int AreaId { get; set; }
    public int RoomId { get; set; }
    public int ParentAreaId { get; set; }
    public string Cookie { get; set; }
    public string OBSWebsocketUrl { get; set; }
    public string OBSWebsocketPassword { get; set; }
    // 0: RTMP, 1: SRT
    public int StreamURLType { get; set; }
    public int StartType { get; set; }
    public string ConfigFilePath { get; set; }
    public string Description { get; set; }
}