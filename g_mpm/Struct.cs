using System.Runtime.InteropServices;

namespace g_mpm
{
    /// <summary>
    /// 定义常量
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// 内存大小
        /// </summary>
        public const int BufferSize = 1024;
    }

    /// <summary>
    /// 共享内存命令传递结构体
    /// <summary>
    public struct SharedMemoryCommand
    {
        /// <summary>
        /// 写入者状态 枚举WriteStatus
        /// </summary>
        public int Writer;

        /// <summary>
        /// 执行命令 枚举MemoryCommand
        /// </summary>
        public int DefCommand;
        ///<summary>
        ///附加命令
        ///</summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.BufferSize)]
        public string AdditionaCcommand;

        /// <summary>
        /// 执行状态 枚举RunStatus
        /// </summary>
        public int RunStatus;
        /// <summary>
        /// 报错信息
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.BufferSize)]
        public string ErrorInfo;

        /// <summary>
        /// 结构体数据类型 枚举StructType
        /// </summary>
        public int StructDataType;
    }

    ///<summary>
    ///枚举
    ///</summary>

    /// <summary>
    /// 连接状态
    /// </summary>
    public enum ConnectStatus
    {
        /// <summary>
        /// 未连接
        /// </summary>
        NOT_CONNECTED,
        /// <summary>
        /// 已连接
        /// </summary>
        CONNECTED,
        /// <summary>
        /// 未初始化
        /// </summary>
        NOT_INITIALIZED,
        /// <summary>
        /// 已初始化
        /// </summary>
        INITIALIZED
    };

    /// <summary>
    /// 写入状态
    /// </summary>
    enum WriteStatus
    {
        /// <summary>
        /// 没有上一任写入者
        /// </summary>
        EMPTY_WRITER,
        /// <summary>
        /// 上一任由mpm写入
        /// </summary>
        WHITEWITHCPP,
        /// <summary>
        /// 上一任由g_mpm写入
        /// </summary>
        WHITEWITHCS
    };

    /// <summary>
    /// 通信命令
    /// </summary>
    public enum MemoryCommand
    {
        /// <summary>
        /// 无命令
        /// </summary>
        EMPTY_COMMAND,
        /// <summary>
        /// 设置加载路径
        /// </summary>
        SET_PATH
    };

    /// <summary>
    /// 执行状态
    /// </summary>
    public enum RunStatus
    {
        //无状态
        EMPTY_STATUS,
        //成功
        SUCCESSFUL,
        //失败
        FAILED
    };

    /// <summary>
    /// 结构体类型
    /// </summary>
    /// <prarm name="WDNL">存档路径列表与名称列表</prarm>
    enum StructType
    {
        /// <summary>
        /// WorldDirectoriesNameList 存档路径列表与名称列表
        /// </summary>
        WDNL,
        /// <summary>
        /// WorldDirectoriesName 存档路径与名称
        /// </summary>
        WDN,
        /// <summary>
        /// UserInfo 玩家信息
        /// </summary>
        UI,
        /// <summary>
        /// PlayerInfo_AS 玩家信息（进度与统计）
        /// </summary>
        PI_AS,
        /// <summary>
        /// PlayerInfo_Data 玩家信息（数据）
        /// </summary>
        PI_D,
        /// <summary>
        /// playerinworldinfo 一次性存储单个玩家的所有数据
        /// </summary>
        PIWI,
        /// <summary>
        /// PlayerInWorldInfoList 存储玩家所有数据的容器结构体
        /// </summary>
        PIWIL
    };

    /// <summary>
    /// 数据承载
    /// </summary>

    /// <summary>
    /// 存档路径列表与名称列表
    /// </summary>
    public struct WorldDirectoriesNameList
    {
        /// <summary>
        /// 存档路径列表
        /// </summary>
        List<string> world_directory_list;
        /// <summary>
        /// 存档名称列表
        /// </summary>
        List<string> world_name_list;
    };

    /// <summary>
    /// 存档路径与名称
    /// </summary>
    public struct WorldDirectoriesName
    {
        /// <summary>
        /// 存档路径
        /// </summary>
        string world_directory;
        /// <summary>
        /// 存档名称
        /// </summary>
        string world_name;
    };

    /// <summary>
    /// 玩家信息
    /// </summary>
    public struct UserInfo
    {
        /// <summary>
        /// 玩家昵称
        /// </summary>
        string user_name;
        /// <summary>
        /// 玩家UUID
        /// </summary>
        string uuid;
        /// <summary>
        /// 玩家令牌过期时间
        /// </summary>
        string expiresOn;
    };

    /// <summary>
    /// 玩家信息（进度与统计）
    /// </summary>
    public struct PlayerInfo_AS
    {
        /// <summary>
        /// 文件路径
        /// </summary>
        string path;
        /// <summary>
        /// 文件UUID（文件名）
        /// </summary>
        string uuid;
    };

    /// <summary>
    /// 玩家信息（数据）
    /// </summary>
    struct PlayerInfo_Data
    {
        /// <summary>
        /// 数据文件路径
        /// </summary>
        string dat_path;
        /// <summary>
        /// 旧数据文件路径
        /// </summary>
        string dat_old_path;
        /// <summary>
        /// 装饰盔甲数据文件路径
        /// </summary>
        string cosarmor_path;

        /// <summary>
        /// 数据文件UUID
        /// </summary>
        string uuid;
        /// <summary>
        /// 旧数据文件UUID
        /// </summary>
        string old_uuid;
        /// <summary>
        /// 饰盔甲数据文件UUID
        /// </summary>
        string cosarmor_uuid;
    };

    /// <summary>
    /// 一次性存储单个玩家的所有数据
    /// </summary>
    public struct PlayerInWorldInfo
    {
        /// <summary>
        /// 存档信息
        /// </summary>
        WorldDirectoriesName world_dir_name;
        /// <summary>
        /// 玩家信息
        /// </summary>
        UserInfo player;
        /// <summary>
        /// 进度文件路径
        /// </summary>
        string adv_path;
        /// <summary>
        /// 数据文件路径
        /// </summary>
        string pd_path;
        /// <summary>
        /// 旧数据文件路径
        /// </summary>
        string pd_old_path;
        /// <summary>
        /// 装饰盔甲数据文件路径
        /// </summary>
        string cosarmor_path;
        /// <summary>
        /// 计文件路径
        /// </summary>
        string st_path;
    };

    /// <summary>
    /// 存储玩家所有数据的容器结构体
    /// </summary>
    struct PlayerInWorldInfoList
    {
        /// <summary>
        /// 进度文件信息
        /// </summary>
        List<PlayerInfo_AS> advancements_list;
        /// <summary>
        /// 玩家信息（数据）
        /// </summary>
        List<PlayerInfo_Data> playerdata_list;
        /// <summary>
        /// 进度文件信息
        /// </summary>
        List<PlayerInfo_AS> stats_list;
        /// <summary>
        /// 一次性存储单个玩家的所有数据
        /// </summary>
        List<PlayerInWorldInfo> playerinworldinfo_list;
    };
}
