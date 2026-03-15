using g_mpm.Enums;
using g_mpm.SharedMemoryConfig;
using System.Runtime.InteropServices;
using System.Text;

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
        /// 程序状态 枚举ProgramStatus
        /// </summary>
        public LoadMode LoadMod;

        /// <summary>
        /// 执行命令 枚举MemoryCommand
        /// </summary>
        public Command DefCommand;
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
        /// 标题名称
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.BufferSize)]
        public string TitleName;

        /// <summary>
        /// 结构体数据类型 枚举StructDataType
        /// </summary>
        public StructDataType StructDataType;

        ///<summary>
        /// 序列化数据缓冲区
        ///</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.BufferSize)]
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

        /// <summary>
        /// 从字节数组反序列化
        /// </summary>
        /// <param name="data">字节数组</param>
        /// <returns>反序列化后的对象</returns>
        public static WorldDirectoriesNameList FromBytes(byte[] data)
        {
            WorldDirectoriesNameList result = new WorldDirectoriesNameList();
            result.world_directory_list = new List<string>();
            result.world_name_list = new List<string>();

            int offset = 0;

            // 1. 反序列化两个列表的大小
            int dir_count = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);

            int name_count = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);

            // 2. 反序列化存档路径列表
            for (int i = 0; i < dir_count; i++)
            {
                int str_len = BitConverter.ToInt32(data, offset);
                offset += sizeof(int);

                if (str_len > 0)
                {
                    string dir = Encoding.UTF8.GetString(data, offset, str_len);
                    result.world_directory_list.Add(dir);
                    offset += str_len;
                }
                else
                {
                    result.world_directory_list.Add(string.Empty);
                }
            }

            // 3. 反序列化存档名称列表
            for (int i = 0; i < name_count; i++)
            {
                int str_len = BitConverter.ToInt32(data, offset);
                offset += sizeof(int);

                if (str_len > 0)
                {
                    string name = Encoding.UTF8.GetString(data, offset, str_len);
                    result.world_name_list.Add(name);
                    offset += str_len;
                }
                else
                {
                    result.world_name_list.Add(string.Empty);
                }
            }

            return result;
        }
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

        /// <summary>
        /// 从字节数组反序列化
        /// </summary>
        public static WorldDirectoriesName FromBytes(byte[] data)
        {
            WorldDirectoriesName result = new WorldDirectoriesName();
            int offset = 0;

            // 1. 反序列化存档路径
            int dir_len = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);
            if (dir_len > 0)
            {
                result.world_directory = Encoding.UTF8.GetString(data, offset, dir_len);
            }
            else
            {
                result.world_directory = string.Empty;
            }
            offset += dir_len;

            // 2. 反序列化存档名称
            int name_len = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);
            if (name_len > 0)
            {
                result.world_name = Encoding.UTF8.GetString(data, offset, name_len);
            }
            else
            {
                result.world_name = string.Empty;
            }

            return result;
        }
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

        /// <summary>
        /// 从字节数组反序列化（对应C++的SerializeToFixedArray）
        /// </summary>
        /// <param name="data">完整的字节数组</param>
        /// <returns>反序列化后的UserInfo对象</returns>
        public static UserInfo FromBytes(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            UserInfo result = new UserInfo();
            int offset = 0;

            // 1. 反序列化玩家昵称
            if (offset + sizeof(int) > data.Length)
                throw new Exception("数据不完整：无法读取玩家昵称长度");

            int name_len = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);

            if (name_len < 0 || name_len > data.Length - offset)
                throw new Exception($"玩家昵称长度无效: {name_len}");

            if (name_len > 0)
            {
                result.user_name = Encoding.UTF8.GetString(data, offset, name_len);
                offset += name_len;
            }
            else
            {
                result.user_name = string.Empty;
            }

            // 2. 反序列化玩家UUID
            if (offset + sizeof(int) > data.Length)
                throw new Exception("数据不完整：无法读取UUID长度");

            int uuid_len = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);

            if (uuid_len < 0 || uuid_len > data.Length - offset)
                throw new Exception($"UUID长度无效: {uuid_len}");

            if (uuid_len > 0)
            {
                result.uuid = Encoding.UTF8.GetString(data, offset, uuid_len);
                offset += uuid_len;
            }
            else
            {
                result.uuid = string.Empty;
            }

            // 3. 反序列化令牌过期时间
            if (offset + sizeof(int) > data.Length)
                throw new Exception("数据不完整：无法读取过期时间长度");

            int expire_len = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);

            if (expire_len < 0 || expire_len > data.Length - offset)
                throw new Exception($"过期时间长度无效: {expire_len}");

            if (expire_len > 0)
            {
                result.expiresOn = Encoding.UTF8.GetString(data, offset, expire_len);
                // offset += expire_len; // 最后一个字段不需要增加偏移量
            }
            else
            {
                result.expiresOn = string.Empty;
            }

            return result;
        }

        /// <summary>
        /// 从字节数组指定位置开始反序列化（对应C++的SerializeToBuffer）
        /// </summary>
        /// <param name="data">字节数组</param>
        /// <param name="offset">起始偏移量，反序列化后会更新</param>
        /// <returns>反序列化后的UserInfo对象</returns>
        public static UserInfo FromBytes(byte[] data, ref int offset)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (offset < 0 || offset >= data.Length)
                throw new ArgumentException($"偏移量无效: {offset}，数据长度: {data.Length}");

            UserInfo result = new UserInfo();
            int originalOffset = offset;

            try
            {
                // 1. 反序列化玩家昵称
                if (offset + sizeof(int) > data.Length)
                    throw new Exception("数据不完整：无法读取玩家昵称长度");

                int name_len = BitConverter.ToInt32(data, offset);
                offset += sizeof(int);

                if (name_len < 0 || name_len > data.Length - offset)
                    throw new Exception($"玩家昵称长度无效: {name_len}");

                if (name_len > 0)
                {
                    result.user_name = Encoding.UTF8.GetString(data, offset, name_len);
                    offset += name_len;
                }
                else
                {
                    result.user_name = string.Empty;
                }

                // 2. 反序列化玩家UUID
                if (offset + sizeof(int) > data.Length)
                    throw new Exception("数据不完整：无法读取UUID长度");

                int uuid_len = BitConverter.ToInt32(data, offset);
                offset += sizeof(int);

                if (uuid_len < 0 || uuid_len > data.Length - offset)
                    throw new Exception($"UUID长度无效: {uuid_len}");

                if (uuid_len > 0)
                {
                    result.uuid = Encoding.UTF8.GetString(data, offset, uuid_len);
                    offset += uuid_len;
                }
                else
                {
                    result.uuid = string.Empty;
                }

                // 3. 反序列化令牌过期时间
                if (offset + sizeof(int) > data.Length)
                    throw new Exception("数据不完整：无法读取过期时间长度");

                int expire_len = BitConverter.ToInt32(data, offset);
                offset += sizeof(int);

                if (expire_len < 0 || expire_len > data.Length - offset)
                    throw new Exception($"过期时间长度无效: {expire_len}");

                if (expire_len > 0)
                {
                    result.expiresOn = Encoding.UTF8.GetString(data, offset, expire_len);
                    offset += expire_len;
                }
                else
                {
                    result.expiresOn = string.Empty;
                }

                return result;
            }
            catch (Exception ex)
            {
                // 发生错误时恢复偏移量
                offset = originalOffset;
                throw new Exception($"反序列化UserInfo失败，偏移量={originalOffset}, 数据长度={data.Length}", ex);
            }
        }

        /// <summary>
        /// 获取序列化后的字节大小（用于预计算缓冲区大小）
        /// </summary>
        public int GetSerializedSize()
        {
            int size = 0;

            // 玩家昵称长度字段 + 内容
            size += sizeof(int) + (user_name?.Length ?? 0);

            // UUID长度字段 + 内容
            size += sizeof(int) + (uuid?.Length ?? 0);

            // 过期时间长度字段 + 内容
            size += sizeof(int) + (expiresOn?.Length ?? 0);

            return size;
        }
    }

    /// <summary>
    /// UserInfo列表的序列化辅助类（对应C++的SerializeVector）
    /// </summary>
    public static class UserInfoListSerializer
    {
        /// <summary>
        /// 从字节数组反序列化UserInfo列表
        /// </summary>
        /// <param name="data">完整的字节数组</param>
        /// <returns>反序列化后的UserInfo列表</returns>
        public static List<UserInfo> FromBytes(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.Length < sizeof(int))
                throw new Exception("数据长度不足，无法读取元素个数");

            List<UserInfo> result = new List<UserInfo>();
            int offset = 0;

            // 读取元素个数
            int count = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);

            if (count < 0)
                throw new Exception($"元素个数无效: {count}");

            if (count > 10000) // 添加合理的上限检查，防止异常数据导致内存问题
                throw new Exception($"元素个数过大: {count}");

            // 读取每个元素
            for (int i = 0; i < count; i++)
            {
                if (offset >= data.Length)
                    throw new Exception($"数据不完整：只读取了 {i}/{count} 个元素");

                UserInfo user = UserInfo.FromBytes(data, ref offset);
                result.Add(user);
            }

            return result;
        }

        /// <summary>
        /// 从字节数组指定位置开始反序列化UserInfo列表
        /// </summary>
        /// <param name="data">字节数组</param>
        /// <param name="offset">起始偏移量，反序列化后会更新</param>
        /// <returns>反序列化后的UserInfo列表</returns>
        public static List<UserInfo> FromBytes(byte[] data, ref int offset)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (offset < 0 || offset + sizeof(int) > data.Length)
                throw new ArgumentException($"偏移量无效: {offset}，数据长度: {data.Length}");

            List<UserInfo> result = new List<UserInfo>();
            int originalOffset = offset;

            try
            {
                // 读取元素个数
                int count = BitConverter.ToInt32(data, offset);
                offset += sizeof(int);

                if (count < 0)
                    throw new Exception($"元素个数无效: {count}");

                if (count > 10000)
                    throw new Exception($"元素个数过大: {count}");

                // 读取每个元素
                for (int i = 0; i < count; i++)
                {
                    if (offset >= data.Length)
                        throw new Exception($"数据不完整：只读取了 {i}/{count} 个元素");

                    UserInfo user = UserInfo.FromBytes(data, ref offset);
                    result.Add(user);
                }

                return result;
            }
            catch (Exception ex)
            {
                // 发生错误时恢复偏移量
                offset = originalOffset;
                throw new Exception($"反序列化UserInfo列表失败，偏移量={originalOffset}, 数据长度={data.Length}", ex);
            }
        }
    }

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

        /// <summary>
        /// 从字节数组反序列化
        /// </summary>
        /// <returns>反序列化后的对象</returns>
        public static PlayerInfo_AS FromBytes(byte[] data)
        {
            PlayerInfo_AS result = new PlayerInfo_AS();
            int offset = 0;

            // 1. 反序列化文件路径
            int path_len = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);
            if (path_len > 0)
            {
                result.path = Encoding.UTF8.GetString(data, offset, path_len);
            }
            else
            {
                result.path = string.Empty;
            }
            offset += path_len;

            // 2. 反序列化文件UUID（文件名）
            int uuid_len = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);
            if (uuid_len > 0)
            {
                result.uuid = Encoding.UTF8.GetString(data, offset, uuid_len);
            }
            else
            {
                result.uuid = string.Empty;
            }

            return result;
        }
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

        /// <summary>
        /// 从字节数组反序列化
        /// </summary>
        /// <returns>反序列化后的对象</returns>
        public static PlayerInfo_Data FromBytes(byte[] data)
        {
            PlayerInfo_Data result = new PlayerInfo_Data();
            int offset = 0;

            // 1. 反序列化数据文件路径
            int dat_path_len = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);
            if (dat_path_len > 0)
            {
                result.dat_path = Encoding.UTF8.GetString(data, offset, dat_path_len);
            }
            else
            {
                result.dat_path = string.Empty;
            }
            offset += dat_path_len;

            // 2. 反序列化旧数据文件路径
            int dat_old_path_len = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);
            if (dat_old_path_len > 0)
            {
                result.dat_old_path = Encoding.UTF8.GetString(data, offset, dat_old_path_len);
            }
            else
            {
                result.dat_old_path = string.Empty;
            }
            offset += dat_old_path_len;

            // 3. 反序列化装饰盔甲数据文件路径
            int cosarmor_path_len = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);
            if (cosarmor_path_len > 0)
            {
                result.cosarmor_path = Encoding.UTF8.GetString(data, offset, cosarmor_path_len);
            }
            else
            {
                result.cosarmor_path = string.Empty;
            }
            offset += cosarmor_path_len;

            // 4. 反序列化数据文件UUID
            int uuid_len = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);
            if (uuid_len > 0)
            {
                result.uuid = Encoding.UTF8.GetString(data, offset, uuid_len);
            }
            else
            {
                result.uuid = string.Empty;
            }
            offset += uuid_len;

            // 5. 反序列化旧数据文件UUID
            int old_uuid_len = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);
            if (old_uuid_len > 0)
            {
                result.old_uuid = Encoding.UTF8.GetString(data, offset, old_uuid_len);
            }
            else
            {
                result.old_uuid = string.Empty;
            }
            offset += old_uuid_len;

            // 6. 反序列化装饰盔甲数据文件UUID
            int cosarmor_uuid_len = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);
            if (cosarmor_uuid_len > 0)
            {
                result.cosarmor_uuid = Encoding.UTF8.GetString(data, offset, cosarmor_uuid_len);
            }
            else
            {
                result.cosarmor_uuid = string.Empty;
            }

            return result;
        }
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

        /// <summary>
        /// 从字节数组反序列化
        /// </summary>
        /// <returns>反序列化后的对象</returns>
        public static PlayerInWorldInfo FromBytes(byte[] data)
        {
            PlayerInWorldInfo result = new PlayerInWorldInfo();
            int offset = 0;

            // 1. 反序列化存档信息
            // 需要先知道WorldDirectoriesName序列化后的大小
            byte[] worldDirData = new byte[GetWorldDirectoriesNameSize(data, offset)];
            Array.Copy(data, offset, worldDirData, 0, worldDirData.Length);
            result.world_dir_name = WorldDirectoriesName.FromBytes(worldDirData);
            offset += worldDirData.Length;

            // 2. 反序列化玩家信息
            byte[] playerData = new byte[GetUserInfoSize(data, offset)];
            Array.Copy(data, offset, playerData, 0, playerData.Length);
            result.player = UserInfo.FromBytes(playerData);
            offset += playerData.Length;

            // 3. 反序列化进度文件路径
            int adv_path_len = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);
            if (adv_path_len > 0)
            {
                result.adv_path = Encoding.UTF8.GetString(data, offset, adv_path_len);
            }
            else
            {
                result.adv_path = string.Empty;
            }
            offset += adv_path_len;

            // 4. 反序列化数据文件路径
            int pd_path_len = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);
            if (pd_path_len > 0)
            {
                result.pd_path = Encoding.UTF8.GetString(data, offset, pd_path_len);
            }
            else
            {
                result.pd_path = string.Empty;
            }
            offset += pd_path_len;

            // 5. 反序列化旧数据文件路径
            int pd_old_path_len = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);
            if (pd_old_path_len > 0)
            {
                result.pd_old_path = Encoding.UTF8.GetString(data, offset, pd_old_path_len);
            }
            else
            {
                result.pd_old_path = string.Empty;
            }
            offset += pd_old_path_len;

            // 6. 反序列化装饰盔甲数据文件路径
            int cosarmor_path_len = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);
            if (cosarmor_path_len > 0)
            {
                result.cosarmor_path = Encoding.UTF8.GetString(data, offset, cosarmor_path_len);
            }
            else
            {
                result.cosarmor_path = string.Empty;
            }
            offset += cosarmor_path_len;

            // 7. 反序列化统计文件路径
            int st_path_len = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);
            if (st_path_len > 0)
            {
                result.st_path = Encoding.UTF8.GetString(data, offset, st_path_len);
            }
            else
            {
                result.st_path = string.Empty;
            }

            return result;
        }

        private static int GetWorldDirectoriesNameSize(byte[] data, int offset)
        {
            int tempOffset = offset;
            int size = 0;

            // 存档路径长度 + 长度字段
            int dir_len = BitConverter.ToInt32(data, tempOffset);
            size += sizeof(int) + dir_len;
            tempOffset += sizeof(int) + dir_len;

            // 存档名称长度 + 长度字段
            int name_len = BitConverter.ToInt32(data, tempOffset);
            size += sizeof(int) + name_len;

            return size;
        }

        private static int GetUserInfoSize(byte[] data, int offset)
        {
            int tempOffset = offset;
            int size = 0;

            // 玩家昵称长度 + 长度字段
            int name_len = BitConverter.ToInt32(data, tempOffset);
            size += sizeof(int) + name_len;
            tempOffset += sizeof(int) + name_len;

            // 玩家UUID长度 + 长度字段
            int uuid_len = BitConverter.ToInt32(data, tempOffset);
            size += sizeof(int) + uuid_len;
            tempOffset += sizeof(int) + uuid_len;

            // 过期时间长度 + 长度字段
            int expire_len = BitConverter.ToInt32(data, tempOffset);
            size += sizeof(int) + expire_len;

            return size;
        }
    }

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

        /// <summary>
        /// 从字节数组反序列化
        /// </summary>
        /// <returns>反序列化后的对象</returns>
        public static PlayerInWorldInfoList FromBytes(byte[] data)
        {
            PlayerInWorldInfoList result = new PlayerInWorldInfoList();
            result.advancements_list = new List<PlayerInfo_AS>();
            result.playerdata_list = new List<PlayerInfo_Data>();
            result.stats_list = new List<PlayerInfo_AS>();
            result.playerinworldinfo_list = new List<PlayerInWorldInfo>();

            int offset = 0;

            // 1. 反序列化advancements_list的大小
            int advancements_count = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);

            // 2. 反序列化advancements_list中的每个元素
            for (int i = 0; i < advancements_count; i++)
            {
                // 获取当前PlayerInfo_AS对象的大小
                int itemSize = GetPlayerInfo_ASSize(data, offset);
                byte[] itemData = new byte[itemSize];
                Array.Copy(data, offset, itemData, 0, itemSize);

                PlayerInfo_AS item = PlayerInfo_AS.FromBytes(itemData);
                result.advancements_list.Add(item);
                offset += itemSize;
            }

            // 3. 反序列化playerdata_list的大小
            int playerdata_count = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);

            // 4. 反序列化playerdata_list中的每个元素
            for (int i = 0; i < playerdata_count; i++)
            {
                // 获取当前PlayerInfo_Data对象的大小
                int itemSize = GetPlayerInfo_DataSize(data, offset);
                byte[] itemData = new byte[itemSize];
                Array.Copy(data, offset, itemData, 0, itemSize);

                PlayerInfo_Data item = PlayerInfo_Data.FromBytes(itemData);
                result.playerdata_list.Add(item);
                offset += itemSize;
            }

            // 5. 反序列化stats_list的大小
            int stats_count = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);

            // 6. 反序列化stats_list中的每个元素
            for (int i = 0; i < stats_count; i++)
            {
                // 获取当前PlayerInfo_AS对象的大小
                int itemSize = GetPlayerInfo_ASSize(data, offset);
                byte[] itemData = new byte[itemSize];
                Array.Copy(data, offset, itemData, 0, itemSize);

                PlayerInfo_AS item = PlayerInfo_AS.FromBytes(itemData);
                result.stats_list.Add(item);
                offset += itemSize;
            }

            // 7. 反序列化playerinworldinfo_list的大小
            int playerinworldinfo_count = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);

            // 8. 反序列化playerinworldinfo_list中的每个元素
            for (int i = 0; i < playerinworldinfo_count; i++)
            {
                // 获取当前PlayerInWorldInfo对象的大小
                int itemSize = GetPlayerInWorldInfoSize(data, offset);
                byte[] itemData = new byte[itemSize];
                Array.Copy(data, offset, itemData, 0, itemSize);

                PlayerInWorldInfo item = PlayerInWorldInfo.FromBytes(itemData);
                result.playerinworldinfo_list.Add(item);
                offset += itemSize;
            }

            return result;
        }

        private static int GetPlayerInfo_ASSize(byte[] data, int offset)
        {
            int tempOffset = offset;
            int size = 0;

            // path长度 + 长度字段
            int path_len = BitConverter.ToInt32(data, tempOffset);
            size += sizeof(int) + path_len;
            tempOffset += sizeof(int) + path_len;

            // uuid长度 + 长度字段
            int uuid_len = BitConverter.ToInt32(data, tempOffset);
            size += sizeof(int) + uuid_len;

            return size;
        }

        private static int GetPlayerInfo_DataSize(byte[] data, int offset)
        {
            int tempOffset = offset;
            int size = 0;

            // dat_path长度 + 长度字段
            int dat_path_len = BitConverter.ToInt32(data, tempOffset);
            size += sizeof(int) + dat_path_len;
            tempOffset += sizeof(int) + dat_path_len;

            // dat_old_path长度 + 长度字段
            int dat_old_path_len = BitConverter.ToInt32(data, tempOffset);
            size += sizeof(int) + dat_old_path_len;
            tempOffset += sizeof(int) + dat_old_path_len;

            // cosarmor_path长度 + 长度字段
            int cosarmor_path_len = BitConverter.ToInt32(data, tempOffset);
            size += sizeof(int) + cosarmor_path_len;
            tempOffset += sizeof(int) + cosarmor_path_len;

            // uuid长度 + 长度字段
            int uuid_len = BitConverter.ToInt32(data, tempOffset);
            size += sizeof(int) + uuid_len;
            tempOffset += sizeof(int) + uuid_len;

            // old_uuid长度 + 长度字段
            int old_uuid_len = BitConverter.ToInt32(data, tempOffset);
            size += sizeof(int) + old_uuid_len;
            tempOffset += sizeof(int) + old_uuid_len;

            // cosarmor_uuid长度 + 长度字段
            int cosarmor_uuid_len = BitConverter.ToInt32(data, tempOffset);
            size += sizeof(int) + cosarmor_uuid_len;

            return size;
        }

        private static int GetPlayerInWorldInfoSize(byte[] data, int offset)
        {
            int tempOffset = offset;
            int size = 0;

            // world_dir_name大小
            int worldDirSize = GetWorldDirectoriesNameSize(data, tempOffset);
            size += worldDirSize;
            tempOffset += worldDirSize;

            // player大小
            int playerSize = GetUserInfoSize(data, tempOffset);
            size += playerSize;
            tempOffset += playerSize;

            // adv_path长度 + 长度字段
            int adv_path_len = BitConverter.ToInt32(data, tempOffset);
            size += sizeof(int) + adv_path_len;
            tempOffset += sizeof(int) + adv_path_len;

            // pd_path长度 + 长度字段
            int pd_path_len = BitConverter.ToInt32(data, tempOffset);
            size += sizeof(int) + pd_path_len;
            tempOffset += sizeof(int) + pd_path_len;

            // pd_old_path长度 + 长度字段
            int pd_old_path_len = BitConverter.ToInt32(data, tempOffset);
            size += sizeof(int) + pd_old_path_len;
            tempOffset += sizeof(int) + pd_old_path_len;

            // cosarmor_path长度 + 长度字段
            int cosarmor_path_len = BitConverter.ToInt32(data, tempOffset);
            size += sizeof(int) + cosarmor_path_len;
            tempOffset += sizeof(int) + cosarmor_path_len;

            // st_path长度 + 长度字段
            int st_path_len = BitConverter.ToInt32(data, tempOffset);
            size += sizeof(int) + st_path_len;

            return size;
        }

        private static int GetWorldDirectoriesNameSize(byte[] data, int offset)
        {
            int tempOffset = offset;
            int size = 0;

            int dir_len = BitConverter.ToInt32(data, tempOffset);
            size += sizeof(int) + dir_len;
            tempOffset += sizeof(int) + dir_len;

            int name_len = BitConverter.ToInt32(data, tempOffset);
            size += sizeof(int) + name_len;

            return size;
        }

        private static int GetUserInfoSize(byte[] data, int offset)
        {
            int tempOffset = offset;
            int size = 0;

            int name_len = BitConverter.ToInt32(data, tempOffset);
            size += sizeof(int) + name_len;
            tempOffset += sizeof(int) + name_len;

            int uuid_len = BitConverter.ToInt32(data, tempOffset);
            size += sizeof(int) + uuid_len;
            tempOffset += sizeof(int) + uuid_len;

            int expire_len = BitConverter.ToInt32(data, tempOffset);
            size += sizeof(int) + expire_len;

            return size;
        }
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