namespace mpm_GUI.Models;

/// <summary>存档（世界）条目。</summary>
public sealed record WorldEntry(string Name, string Directory);

/// <summary>玩家（usercache）条目。</summary>
public sealed record PlayerEntry(string Name, string Uuid, string ExpiresOn);

/// <summary>单个玩家在某存档中的文件存在情况（来自 OPEN_WORLD / OPEN_PLAYER）。</summary>
public sealed record PlayerInWorldPresence(
    string WorldName,
    string WorldDirectory,
    string PlayerName,
    string PlayerUuid,
    bool HasAdvancement,
    bool HasPlayerData,
    bool HasOldPlayerData,
    bool HasCosArmor,
    bool HasStats);

/// <summary>反序列化后的 PIWIL 结构。</summary>
public sealed record PlayerInWorldInfoList(
    IReadOnlyList<PlayerInWorldPresence> Rows);
