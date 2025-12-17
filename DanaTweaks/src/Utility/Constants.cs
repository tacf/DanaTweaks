using Vintagestory.API.Config;

namespace DanaTweaks;

public static class Constants
{
    public const string ConfigServerName = "DanaTweaks-Server.json";
    public const string ConfigClientName = "DanaTweaks-Client.json";

    #region crate tweaks
    public const string DefaultLabel = "paper-empty";

    public const string OpenCloseLidHotkey = "opencloselid";
    public const string RemoveOrAddLabelHotkey = "removeoraddlabel";

    public static readonly string OpenCloseLidName = Lang.Get("danatweaks:Hotkey.OpenCloseLid.Name");
    public static readonly string RemoveOrAddLabelName = Lang.Get("danatweaks:Hotkey.RemoveOrAddLabel.Name");

    public static readonly string NoCrate = Lang.Get("danatweaks:Hotkey.Error.NoCrate");
    public static readonly string NoLabel = Lang.Get("danatweaks:Hotkey.Error.NoLabel");
    public static readonly string HasDiffLabel = Lang.Get("danatweaks:Hotkey.Error.HasDiffLabel");
    #endregion crate tweaks

    #region Codes
    public const string ParchmentCode = "paper-parchment";
    public const string ResinCode = "resin";
    public const string PitkilnCode = "pitkiln";
    #endregion Codes

    public const string failureOmniRotatable = "danatweaks-omnirotatable";

    public class ModdedSounds
    {
        public const string PlaceDirt = "sounds/block/dirt1";

    }
}