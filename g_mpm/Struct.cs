using g_mpm.Enums;
using System.Runtime.InteropServices;

namespace g_mpm.Structs
{
    /// <summary>
    /// 共享内存命令传递结构体
    /// <summary>
    public struct SharedMemoryCommand
    {
        /// <summary>
        /// 写入者状态 枚举WriteStatus
        /// </summary>
        public WriteStatus Writer;
        /// <summary>
        /// 执行命令 枚举MemoryCommand
        /// </summary>
        public int DefCommand;
        ///<summary>
        ///附加命令
        ///</summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.BufferSize)]
        public string AdditionaCommand;

        /// <summary>
        /// 执行状态 枚举RunStatus
        /// </summary>
        public RunStatus RunStatus;
        /// <summary>
        /// 报错信息
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.BufferSize)]
        public string ErrorInfo;

        /// <summary>
        /// 结构体数据类型 枚举StructType
        /// </summary>
        public StructDataType StructDataType;

        ///<summary>
        /// 序列化数据缓冲区
        ///</summary>
        public byte[] StructData;
    }

    /// <summary>
    /// 存档路径列表与名称列表
    /// </summary>
    public struct WorldDirectoriesNameList
    {
        /// <summary>
        /// 存档路径列表
        /// </summary>
        public List<string> world_directory_list;
        /// <summary>
        /// 存档名称列表
        /// </summary>
        public List<string> world_name_list;
    };

    /// <summary>
    /// 存档路径与名称
    /// </summary>
    public struct WorldDirectoriesName
    {
        /// <summary>
        /// 存档路径
        /// </summary>
        public string world_directory;
        /// <summary>
        /// 存档名称
        /// </summary>
        public string world_name;
    };

    /// <summary>
    /// 玩家信息
    /// </summary>
    public struct UserInfo
    {
        /// <summary>
        /// 玩家昵称
        /// </summary>
        public string user_name;
        /// <summary>
        /// 玩家UUID
        /// </summary>
        public string uuid;
        /// <summary>
        /// 玩家令牌过期时间
        /// </summary>
        public string expiresOn;
    };

    /// <summary>
    /// 玩家信息（进度与统计）
    /// </summary>
    public struct PlayerInfo_AS
    {
        /// <summary>
        /// 文件路径
        /// </summary>
        public string path;
        /// <summary>
        /// 文件UUID（文件名）
        /// </summary>
        public string uuid;
    };

    /// <summary>
    /// 玩家信息（数据）
    /// </summary>
    public struct PlayerInfo_Data
    {
        /// <summary>
        /// 数据文件路径
        /// </summary>
        public string dat_path;
        /// <summary>
        /// 旧数据文件路径
        /// </summary>
        public string dat_old_path;
        /// <summary>
        /// 装饰盔甲数据文件路径
        /// </summary>
        public string cosarmor_path;

        /// <summary>
        /// 数据文件UUID
        /// </summary>
        public string uuid;
        /// <summary>
        /// 旧数据文件UUID
        /// </summary>
        public string old_uuid;
        /// <summary>
        /// 饰盔甲数据文件UUID
        /// </summary>
        public string cosarmor_uuid;
    };

    /// <summary>
    /// 一次性存储单个玩家的所有数据
    /// </summary>
    public struct PlayerInWorldInfo
    {
        /// <summary>
        /// 存档信息
        /// </summary>
        public WorldDirectoriesName world_dir_name;
        /// <summary>
        /// 玩家信息
        /// </summary>
        public UserInfo player;
        /// <summary>
        /// 进度文件路径
        /// </summary>
        public string adv_path;
        /// <summary>
        /// 数据文件路径
        /// </summary>
        public string pd_path;
        /// <summary>
        /// 旧数据文件路径
        /// </summary>
        public string pd_old_path;
        /// <summary>
        /// 装饰盔甲数据文件路径
        /// </summary>
        public string cosarmor_path;
        /// <summary>
        /// 计文件路径
        /// </summary>
        public string st_path;
    };

    /// <summary>
    /// 存储玩家所有数据的容器结构体
    /// </summary>
    public struct PlayerInWorldInfoList
    {
        /// <summary>
        /// 进度文件信息
        /// </summary>
        public List<PlayerInfo_AS> advancements_list;
        /// <summary>
        /// 玩家信息（数据）
        /// </summary>
        public List<PlayerInfo_Data> playerdata_list;
        /// <summary>
        /// 进度文件信息
        /// </summary>
        public List<PlayerInfo_AS> stats_list;
        /// <summary>
        /// 一次性存储单个玩家的所有数据
        /// </summary>
        public List<PlayerInWorldInfo> playerinworldinfo_list;
    };

    ///<summary>
    /// 句柄指针
    ///</summary>
    public struct HandlePtr
    {
        /// <summary>
        /// 共享内存句柄
        /// </summary>
        public IntPtr _hMapFile;
        /// <summary>
        /// 主互斥锁句柄
        /// </summary>
        public IntPtr _hMutex;
        /// <summary>
        /// 发送互斥锁句柄
        /// </summary>
        public IntPtr _hEvent_Send;
        /// <summary>
        /// 接受互斥锁句柄
        /// </summary>
        public IntPtr _hEvent_Recv;
        /// <summary>
        /// 初始化互斥锁句柄
        /// </summary>
        public IntPtr _hInitEvent;
        /// <summary>
        /// 共享内存内容指针
        /// </summary>
        public IntPtr sharedMemoryCommand;
    }
}