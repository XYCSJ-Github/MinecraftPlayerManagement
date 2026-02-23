//声明所有枚举类型

namespace g_mpm.Enums
{
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
    public enum WriteStatus
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
        /// 就绪
        /// </summary>
        REDAY,
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
    public enum StructDataType
    {
        /// <summary>
        /// 无结构体
        /// </summary>
        EMPTY_STRUCT,
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
    /// 命令
    /// </summary>
    public enum Command
    {
        /// <summary>
        /// 退出
        /// </summary>
        EXIT,
        /// <summary>
        /// 返回
        /// </summary>
        BREAK,
        /// <summary>
        /// open world
        /// </summary>
        OPEN_WORLD,
        /// <summary>
        /// open player
        /// </summary>
        OPEN_PLAYER,
        /// <summary>
        /// list world
        /// </summary>
        LIST_WORLD,
        /// <summary>
        /// list player
        /// </summary>
        LIST_PLAYER,
        /// <summary>
        /// del player
        /// </summary>
        DEL_PLAYER,
        /// <summary>
        /// del world
        /// </summary>
        DEL_WORLD,
        /// <summary>
        /// del pw
        /// </summary>
        DEL_PW,
        //del js
        DEL_JS,
        /// <summary>
        /// unknown command
        /// </summary>
        NULL_BACK,
        /// <summary>
        /// refresh
        /// </summary>
        REFRESH
    }

    /// <summary>
    /// C++程序状态
    /// </summary>
    public enum ProgramStatus
    {
        /// <summary>
        /// 启动中
        /// </summary>
        STARTING,
        /// <summary>
        /// 运行中
        /// </summary>
        RUNNING,
        /// <summary>
        /// 就绪
        /// </summary>
        READY,
        /// <summary>
        /// 停止中
        /// </summary>
        STOPPING,
        /// <summary>
        /// 已停止
        /// </summary>
        STOP
    }
}
