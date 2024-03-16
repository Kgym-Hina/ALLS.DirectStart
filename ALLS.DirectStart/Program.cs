using System.Text.Json;
using System.Text.RegularExpressions;
using OBSWebsocketDotNet;

namespace ALLS.DirectStart;

internal static partial class Program
{
    static readonly HttpClient client = new HttpClient();

    private static async Task Main()
    {
        var area_id = 235;
        var room_id = 9372130;
        var parent_area_id = 6;
        var cookie = File.Exists("Cookies.txt") ? await File.ReadAllTextAsync("Cookies.txt") : string.Empty;
        var obsWebsocketUrl = "ws://127.0.0.1:4455";
        var obsWebsocketPassword = string.Empty;
        var streamType = 0;

        if (File.Exists("Config.json"))
        {
            var config = JsonSerializer.Deserialize<StartConfig>(await File.ReadAllTextAsync("Config.json"));
            if (config != null)
            {
                area_id = config.AreaId;
                room_id = config.RoomId;
                parent_area_id = config.ParentAreaId;
                cookie = config.Cookie;
                obsWebsocketUrl = config.OBSWebsocketUrl;
                obsWebsocketPassword = config.OBSWebsocketPassword;
                streamType = config.StreamURLType;
            }
        }
        else
        {
            var config = new StartConfig()
            {
                AreaId = area_id,
                RoomId = room_id,
                ParentAreaId = parent_area_id,
                Cookie = cookie,
                OBSWebsocketUrl = obsWebsocketUrl,
                OBSWebsocketPassword = obsWebsocketPassword,
                StreamURLType = 0,
                Description = "以上是默认配置，请修改 Config.json 文件以更改配置, 否则无法启动直播. 请注意, 请不要泄露您的 Cookies."
            };
            await File.WriteAllTextAsync("Config.json", JsonSerializer.Serialize(config));
        }

        // Add cookies to HttpClient
        client.DefaultRequestHeaders.Add("Cookie", cookie);
        
        // Extract bili_jct (csrf_token and csrf) from cookie
        var biliJctMatch = ExtractToken().Match(cookie);
        var biliJct = biliJctMatch.Success ? biliJctMatch.Groups[1].Value : string.Empty;

        await Console.Out.WriteLineAsync("bili_jct: " + biliJct);
        
        // First API call - switch room
        // seems like this API call is not necessary
        var request1 = new HttpRequestMessage(HttpMethod.Get,
            $"https://api.live.bilibili.com/xlive/app-blink/v1/index/getNewRoomSwitch?platform=pc&area_parent_id={parent_area_id}&area_id={area_id}");

        request1.Headers.Add("authority", "api.live.bilibili.com");
        request1.Headers.Add("method", "GET");
        request1.Headers.Add("path",
            $"/xlive/app-blink/v1/index/getNewRoomSwitch?platform=pc&area_parent_id=6&area_id={area_id}");

        var response1 = await client.SendAsync(request1);

        var responseString1 = await response1.Content.ReadAsStringAsync();

        Console.WriteLine(responseString1);

        // Second API call - start live
        var request2 = new HttpRequestMessage(HttpMethod.Post, "https://api.live.bilibili.com/room/v1/Room/startLive");

        request2.Headers.Add("authority", "api.live.bilibili.com");
        request2.Headers.Add("accept", "application/json, text/plain, */*");
        request2.Headers.Add("origin", "https://link.bilibili.com");
        request2.Headers.Add("referer", "https://link.bilibili.com/p/center/index");

        var formdata2 = new Dictionary<string, string>
        {
            { "room_id", $"{room_id}" },
            { "platform", "pc" },
            { "area_v2", $"{area_id}" },
            { "backup_stream", "0" },
            { "csrf_token", biliJct },
            { "csrf", biliJct }
        };

        request2.Content = new FormUrlEncodedContent(formdata2);

        var response2 = await client.SendAsync(request2);

        var responseString2 = await response2.Content.ReadAsStringAsync();

        Console.WriteLine(responseString2);

        // Third API call - get RTMP server address
        var request3 = new HttpRequestMessage(HttpMethod.Post,
            "https://api.live.bilibili.com/xlive/app-blink/v1/live/FetchWebUpStreamAddr");

        request3.Headers.Add("authority", "api.live.bilibili.com");
        request3.Headers.Add("accept", "application/json, text/plain, */*");
        request3.Headers.Add("accept-language", "zh-CN,zh;q=0.9,en-US;q=0.8,en;q=0.7");

        var formdata3 = new Dictionary<string, string>
        {
            { "platform", "pc" },
            { "backup_stream", "0" },
            { "csrf_token", biliJct },
            { "csrf", biliJct }
        };

        request3.Content = new FormUrlEncodedContent(formdata3);

        var response3 = await client.SendAsync(request3);

        var responseString3 = await response3.Content.ReadAsStringAsync();

        Console.WriteLine(responseString3);
        
        // Json parsing
        var jsonDocument = JsonDocument.Parse(responseString3);

        // Extract RTMP server address and SRT server address
        var rtmp_addr = string.Empty;
        var srt_addr = string.Empty;

        if (jsonDocument.RootElement.TryGetProperty("data", out var dataElement))
        {
            string code;
            if (dataElement.TryGetProperty("addr", out var addrElement))
            {
                try
                {
                    rtmp_addr = addrElement.GetProperty("addr").GetString();
                    code = addrElement.GetProperty("code").GetString()!;

                    Console.WriteLine($"Addr: {rtmp_addr}");
                    Console.WriteLine($"Code: {code}");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            
            if (dataElement.TryGetProperty("srt_addr", out var srtAddrElement))
            {
                try
                {
                    srt_addr = srtAddrElement.GetProperty("addr").GetString();
                    code = srtAddrElement.GetProperty("code").GetString()!;

                    Console.WriteLine($"SRT Addr: {srt_addr}");
                    Console.WriteLine($"Code: {code}");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
        
        // Let OBS connect to the RTMP server
        var obsClient = new OBSWebsocket();
        obsClient.ConnectAsync(obsWebsocketUrl, obsWebsocketPassword);
        obsClient.Connected += (_, _) => StartStream(obsClient, rtmp_addr!, srt_addr!, streamType);

        Console.ReadLine();
    }

    /// <summary>
    /// Start streaming
    /// </summary>
    /// <param name="obsClient">Reference to OBS Client</param>
    /// <param name="rtmp_addr">RTMP</param>
    /// <param name="srt_addr">SRT</param>
    /// <param name="streamUrlType">Stream type</param>
    /// <param name="code">RTMP Code</param>
    private static void StartStream(IOBSWebsocket obsClient, string rtmp_addr, string srt_addr, int streamUrlType, string code = "")
    {
        var settings = obsClient.GetStreamServiceSettings();
        if (streamUrlType == 0)
        {
            settings.Settings.Server = rtmp_addr;
            settings.Settings.Key = code;
        }
        else
        {
            settings.Settings.Server = srt_addr;
        }
        obsClient.SetStreamServiceSettings(settings);
        obsClient.StartStream();
        
        // kill the process after 5 seconds
        Task.Delay(5000).ContinueWith(_ => Environment.Exit(0));
    }

    /// <summary>
    /// Extract bili_jct from cookie
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex("bili_jct=([^;]*)")]
    private static partial Regex ExtractToken();
}