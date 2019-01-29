using System;

class Utils
{
    public static string UA(long DID) => 
        $"NintendoSDK Firmware/6.2.0-1.0 (platform:NX; did:{DID:x16}; eid:lp1)";
    public static string Aqua(long DID) => 
        $"https://aqua.hac.lp1.d4c.nintendo.net/required_system_update_meta?device_id={DID:x16}";
    public static string Superfly(string TID) => 
        $"https://superfly.hac.lp1.d4c.nintendo.net/v1/a/{TID}/dv";
    public static string MetaURL(char Type, string TID, string Ver, long DID) => 
        $"https://atum.hac.lp1.d4c.nintendo.net/t/{Type}/{TID}/{Ver}?device_id={DID:x16}";
    public static string ContentURL(char Type, string NCAID) => 
        $"https://atum.hac.lp1.d4c.nintendo.net/c/{Type}/{NCAID}";
    public static string nMetaURL(char Type, string TID, string Ver, long DID) =>
    $"https://atumn.hac.lp1.d4c.nintendo.net/t/{Type}/{TID}/{Ver}?device_id={DID:x16}";
    public static string nContentURL(char Type, string NCAID) =>
        $"https://atumn.hac.lp1.d4c.nintendo.net/c/{Type}/{NCAID}";
    public static string ControlURL(string TID) => 
        $"https://atum.hac.lp1.d4c.nintendo.net/a/d/{TID}";
    public static string CETKURL(string RID) => 
        $"https://atum.hac.lp1.d4c.nintendo.net/r/t/{RID}";
    public const string SunUrl = "https://sun.hac.lp1.d4c.nintendo.net/v1/system_update_meta";
    public static string GetBaseTID(string TID) => 
        $"{Convert.ToUInt64(TID, 16) & 0xffffffffffffe000:x16}";
}