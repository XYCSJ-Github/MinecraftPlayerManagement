//Struct.h 声明结构体
#pragma once

#include "Enums.h"
#include <string>
#include <vector>
#include <Windows.h>

//共享内存缓冲区大小
#define SHARED_MEMORY_BUF_SIZE 1024

/*
* 存档路径列表与名称列表
* @param world_directory_list 存档路径列表
* @param world_name_list 存档名称列表
*/
struct WorldDirectoriesNameList
{
	//存档路径列表
	std::vector<std::string> world_directory_list;
	//存档名称列表
	std::vector<std::string> world_name_list;

	/*
	* 用于序列化WorldDirectoriesNameList结构体的函数
	* @param StructData 传入缓冲区
	* @return 缓冲区大小
	*/
	size_t SerializeToFixedArray(BYTE StructData[SHARED_MEMORY_BUF_SIZE]) const
	{
		const size_t buffer_size = SHARED_MEMORY_BUF_SIZE;
		size_t offset = 0;

		// 1. 写入两个列表的大小
		uint32_t dir_count = static_cast<uint32_t>(world_directory_list.size());
		uint32_t name_count = static_cast<uint32_t>(world_name_list.size());

		// 检查是否有足够空间写入列表大小
		if (offset + sizeof(dir_count) + sizeof(name_count) > buffer_size)
			return (size_t)0;

		std::memcpy(StructData + offset, &dir_count, sizeof(dir_count));
		offset += sizeof(dir_count);

		std::memcpy(StructData + offset, &name_count, sizeof(name_count));
		offset += sizeof(name_count);

		// 2. 写入存档路径列表
		for (const auto& dir : world_directory_list)
		{
			uint32_t str_len = static_cast<uint32_t>(dir.length());

			// 检查是否有足够空间写入字符串长度和内容
			if (offset + sizeof(str_len) + str_len > buffer_size)
				return (size_t)0;

			std::memcpy(StructData + offset, &str_len, sizeof(str_len));
			offset += sizeof(str_len);

			if (str_len > 0)
			{
				std::memcpy(StructData + offset, dir.c_str(), str_len);
				offset += str_len;
			}
		}
		return offset;
	}
};

/*
* 存档路径与名称
* @param world_directory 存档路径
* @param world_name 存档名称
*/
struct WorldDirectoriesName
{
	//存档路径
	std::string world_directory;
	//存档名称
	std::string world_name;

	/*
	* 用于序列化WorldDirectoriesName结构体的函数
	* @param StructData 传入缓冲区
	* @return 缓冲区大小
	*/
	size_t SerializeToFixedArray(BYTE StructData[SHARED_MEMORY_BUF_SIZE]) const
	{
		const size_t buffer_size = SHARED_MEMORY_BUF_SIZE;
		size_t offset = 0;

		// 1. 序列化存档路径
		uint32_t dir_len = static_cast<uint32_t>(world_directory.length());

		// 检查是否有足够空间写入存档路径长度和内容
		if (offset + sizeof(dir_len) + dir_len > buffer_size)
			return (size_t)0;

		std::memcpy(StructData + offset, &dir_len, sizeof(dir_len));
		offset += sizeof(dir_len);

		if (dir_len > 0)
		{
			std::memcpy(StructData + offset, world_directory.c_str(), dir_len);
			offset += dir_len;
		}

		// 2. 序列化存档名称
		uint32_t name_len = static_cast<uint32_t>(world_name.length());

		// 检查是否有足够空间写入存档名称长度和内容
		if (offset + sizeof(name_len) + name_len > buffer_size)
			return (size_t)0;

		std::memcpy(StructData + offset, &name_len, sizeof(name_len));
		offset += sizeof(name_len);

		if (name_len > 0)
		{
			std::memcpy(StructData + offset, world_name.c_str(), name_len);
			offset += name_len;
		}

		return offset;
	}
};


/*
* 玩家信息
* @param user_name 玩家昵称
* @param uuid 玩家UUID
* @param expiresOn 玩家令牌过期时间
*/
struct UserInfo
{
	//玩家昵称
	std::string user_name;
	//玩家UUID
	std::string uuid;
	//玩家令牌过期时间
	std::string expiresOn;

	/*
	* 用于序列化UserInfo结构体的函数
	* @param StructData 传入缓冲区
	* @return 缓冲区大小
	*/
	size_t SerializeToFixedArray(BYTE StructData[SHARED_MEMORY_BUF_SIZE]) const
	{
		const size_t buffer_size = SHARED_MEMORY_BUF_SIZE;
		size_t offset = 0;

		// 1. 序列化玩家昵称
		uint32_t name_len = static_cast<uint32_t>(user_name.length());

		// 检查是否有足够空间写入玩家昵称长度和内容
		if (offset + sizeof(name_len) + name_len > buffer_size)
			return (size_t)0;

		std::memcpy(StructData + offset, &name_len, sizeof(name_len));
		offset += sizeof(name_len);

		if (name_len > 0)
		{
			std::memcpy(StructData + offset, user_name.c_str(), name_len);
			offset += name_len;
		}

		// 2. 序列化玩家UUID
		uint32_t uuid_len = static_cast<uint32_t>(uuid.length());

		// 检查是否有足够空间写入UUID长度和内容
		if (offset + sizeof(uuid_len) + uuid_len > buffer_size)
			return (size_t)0;

		std::memcpy(StructData + offset, &uuid_len, sizeof(uuid_len));
		offset += sizeof(uuid_len);

		if (uuid_len > 0)
		{
			std::memcpy(StructData + offset, uuid.c_str(), uuid_len);
			offset += uuid_len;
		}

		// 3. 序列化令牌过期时间
		uint32_t expire_len = static_cast<uint32_t>(expiresOn.length());

		// 检查是否有足够空间写入过期时间长度和内容
		if (offset + sizeof(expire_len) + expire_len > buffer_size)
			return (size_t)0;

		std::memcpy(StructData + offset, &expire_len, sizeof(expire_len));
		offset += sizeof(expire_len);

		if (expire_len > 0)
		{
			std::memcpy(StructData + offset, expiresOn.c_str(), expire_len);
			offset += expire_len;
		}

		return offset;
	}
};

/*
* 玩家信息（进度与统计）
* @param path 文件路径
* @param uuid 文件UUID（文件名）
*/
struct PlayerInfo_AS
{
	//文件路径
	std::string path;
	//文件UUID（文件名）
	std::string uuid;

	/*
	* 用于序列化PlayerInfo_AS结构体的函数
	* @param StructData 传入缓冲区
	* @return 缓冲区大小
	*/
	size_t SerializeToFixedArray(BYTE StructData[SHARED_MEMORY_BUF_SIZE]) const
	{
		const size_t buffer_size = SHARED_MEMORY_BUF_SIZE;
		size_t offset = 0;

		// 1. 序列化文件路径
		uint32_t path_len = static_cast<uint32_t>(path.length());

		// 检查是否有足够空间写入路径长度和内容
		if (offset + sizeof(path_len) + path_len > buffer_size)
			return (size_t)0;

		std::memcpy(StructData + offset, &path_len, sizeof(path_len));
		offset += sizeof(path_len);

		if (path_len > 0)
		{
			std::memcpy(StructData + offset, path.c_str(), path_len);
			offset += path_len;
		}

		// 2. 序列化文件UUID（文件名）
		uint32_t uuid_len = static_cast<uint32_t>(uuid.length());

		// 检查是否有足够空间写入UUID长度和内容
		if (offset + sizeof(uuid_len) + uuid_len > buffer_size)
			return (size_t)0;

		std::memcpy(StructData + offset, &uuid_len, sizeof(uuid_len));
		offset += sizeof(uuid_len);

		if (uuid_len > 0)
		{
			std::memcpy(StructData + offset, uuid.c_str(), uuid_len);
			offset += uuid_len;
		}

		return offset;
	}
};

/*
* 玩家信息（数据）
* @param dat_path 数据文件路径
* @param dat_old_path 旧数据文件路径
* @param cosarmor_path 装饰盔甲数据文件路径
* @param uuid 数据文件UUID
* @param old_uuid 旧数据文件UUID
* @param cosarmor_uuid 装饰盔甲数据文件UUID
*/
struct PlayerInfo_Data
{
	//数据文件路径
	std::string dat_path;
	//旧数据文件路径
	std::string dat_old_path;
	//装饰盔甲数据文件路径
	std::string cosarmor_path;

	//数据文件UUID
	std::string uuid;
	//旧数据文件UUID
	std::string old_uuid;
	//装饰盔甲数据文件UUID
	std::string cosarmor_uuid;

	/*
	* 用于序列化PlayerInfo_Data结构体的函数
	* @param StructData 传入缓冲区
	* @return 缓冲区大小
	*/
	size_t SerializeToFixedArray(BYTE StructData[SHARED_MEMORY_BUF_SIZE]) const
	{
		const size_t buffer_size = SHARED_MEMORY_BUF_SIZE;
		size_t offset = 0;

		// 1. 序列化数据文件路径
		uint32_t dat_path_len = static_cast<uint32_t>(dat_path.length());
		if (offset + sizeof(dat_path_len) + dat_path_len > buffer_size)
			return (size_t)0;
		std::memcpy(StructData + offset, &dat_path_len, sizeof(dat_path_len));
		offset += sizeof(dat_path_len);
		if (dat_path_len > 0)
		{
			std::memcpy(StructData + offset, dat_path.c_str(), dat_path_len);
			offset += dat_path_len;
		}

		// 2. 序列化旧数据文件路径
		uint32_t dat_old_path_len = static_cast<uint32_t>(dat_old_path.length());
		if (offset + sizeof(dat_old_path_len) + dat_old_path_len > buffer_size)
			return (size_t)0;
		std::memcpy(StructData + offset, &dat_old_path_len, sizeof(dat_old_path_len));
		offset += sizeof(dat_old_path_len);
		if (dat_old_path_len > 0)
		{
			std::memcpy(StructData + offset, dat_old_path.c_str(), dat_old_path_len);
			offset += dat_old_path_len;
		}

		// 3. 序列化装饰盔甲数据文件路径
		uint32_t cosarmor_path_len = static_cast<uint32_t>(cosarmor_path.length());
		if (offset + sizeof(cosarmor_path_len) + cosarmor_path_len > buffer_size)
			return (size_t)0;
		std::memcpy(StructData + offset, &cosarmor_path_len, sizeof(cosarmor_path_len));
		offset += sizeof(cosarmor_path_len);
		if (cosarmor_path_len > 0)
		{
			std::memcpy(StructData + offset, cosarmor_path.c_str(), cosarmor_path_len);
			offset += cosarmor_path_len;
		}

		// 4. 序列化数据文件UUID
		uint32_t uuid_len = static_cast<uint32_t>(uuid.length());
		if (offset + sizeof(uuid_len) + uuid_len > buffer_size)
			return (size_t)0;
		std::memcpy(StructData + offset, &uuid_len, sizeof(uuid_len));
		offset += sizeof(uuid_len);
		if (uuid_len > 0)
		{
			std::memcpy(StructData + offset, uuid.c_str(), uuid_len);
			offset += uuid_len;
		}

		// 5. 序列化旧数据文件UUID
		uint32_t old_uuid_len = static_cast<uint32_t>(old_uuid.length());
		if (offset + sizeof(old_uuid_len) + old_uuid_len > buffer_size)
			return (size_t)0;
		std::memcpy(StructData + offset, &old_uuid_len, sizeof(old_uuid_len));
		offset += sizeof(old_uuid_len);
		if (old_uuid_len > 0)
		{
			std::memcpy(StructData + offset, old_uuid.c_str(), old_uuid_len);
			offset += old_uuid_len;
		}

		// 6. 序列化装饰盔甲数据文件UUID
		uint32_t cosarmor_uuid_len = static_cast<uint32_t>(cosarmor_uuid.length());
		if (offset + sizeof(cosarmor_uuid_len) + cosarmor_uuid_len > buffer_size)
			return (size_t)0;
		std::memcpy(StructData + offset, &cosarmor_uuid_len, sizeof(cosarmor_uuid_len));
		offset += sizeof(cosarmor_uuid_len);
		if (cosarmor_uuid_len > 0)
		{
			std::memcpy(StructData + offset, cosarmor_uuid.c_str(), cosarmor_uuid_len);
			offset += cosarmor_uuid_len;
		}

		return offset;
	}
};

/*
* 一次性存储单个玩家的所有数据
* @param world_dir_name 存档信息
* @param player 玩家信息
* @param adv_path 进度文件路径
* @param pd_path 数据文件路径
* @param pd_old_path 旧数据文件路径
* @param cosarmor_path 装饰盔甲数据文件路径
* @param st_path 统计文件路径
*/
struct PlayerInWorldInfo
{
	/*
	* 存档信息
	* @param world_directory 存档路径
	* @param world_name 存档名称
	*/
	WorldDirectoriesName world_dir_name;
	/*
	* 玩家信息
	* @param user_name 玩家昵称
	* @param uuid 玩家UUID
	* @param expiresOn 玩家令牌过期时间
	*/
	UserInfo player;
	//进度文件路径
	std::string adv_path;
	//数据文件路径
	std::string pd_path;
	//旧数据文件路径
	std::string pd_old_path;
	//装饰盔甲数据文件路径
	std::string cosarmor_path;
	//统计文件路径
	std::string st_path;

	/*
	* 用于序列化PlayerInWorldInfo结构体的函数
	* @param StructData 传入缓冲区
	* @return 缓冲区大小
	*/
	size_t SerializeToFixedArray(BYTE StructData[SHARED_MEMORY_BUF_SIZE]) const
	{
		const size_t buffer_size = SHARED_MEMORY_BUF_SIZE;
		size_t offset = 0;
		size_t result = 0;

		// 1. 序列化存档信息
		result = world_dir_name.SerializeToFixedArray(StructData + offset);
		if (result == 0) return (size_t)0;
		offset += result;

		// 2. 序列化玩家信息
		result = player.SerializeToFixedArray(StructData + offset);
		if (result == 0) return (size_t)0;
		offset += result;

		// 3. 序列化进度文件路径
		uint32_t adv_path_len = static_cast<uint32_t>(adv_path.length());
		if (offset + sizeof(adv_path_len) + adv_path_len > buffer_size)
			return (size_t)0;
		std::memcpy(StructData + offset, &adv_path_len, sizeof(adv_path_len));
		offset += sizeof(adv_path_len);
		if (adv_path_len > 0)
		{
			std::memcpy(StructData + offset, adv_path.c_str(), adv_path_len);
			offset += adv_path_len;
		}

		// 4. 序列化数据文件路径
		uint32_t pd_path_len = static_cast<uint32_t>(pd_path.length());
		if (offset + sizeof(pd_path_len) + pd_path_len > buffer_size)
			return (size_t)0;
		std::memcpy(StructData + offset, &pd_path_len, sizeof(pd_path_len));
		offset += sizeof(pd_path_len);
		if (pd_path_len > 0)
		{
			std::memcpy(StructData + offset, pd_path.c_str(), pd_path_len);
			offset += pd_path_len;
		}

		// 5. 序列化旧数据文件路径
		uint32_t pd_old_path_len = static_cast<uint32_t>(pd_old_path.length());
		if (offset + sizeof(pd_old_path_len) + pd_old_path_len > buffer_size)
			return (size_t)0;
		std::memcpy(StructData + offset, &pd_old_path_len, sizeof(pd_old_path_len));
		offset += sizeof(pd_old_path_len);
		if (pd_old_path_len > 0)
		{
			std::memcpy(StructData + offset, pd_old_path.c_str(), pd_old_path_len);
			offset += pd_old_path_len;
		}

		// 6. 序列化装饰盔甲数据文件路径
		uint32_t cosarmor_path_len = static_cast<uint32_t>(cosarmor_path.length());
		if (offset + sizeof(cosarmor_path_len) + cosarmor_path_len > buffer_size)
			return (size_t)0;
		std::memcpy(StructData + offset, &cosarmor_path_len, sizeof(cosarmor_path_len));
		offset += sizeof(cosarmor_path_len);
		if (cosarmor_path_len > 0)
		{
			std::memcpy(StructData + offset, cosarmor_path.c_str(), cosarmor_path_len);
			offset += cosarmor_path_len;
		}

		// 7. 序列化统计文件路径
		uint32_t st_path_len = static_cast<uint32_t>(st_path.length());
		if (offset + sizeof(st_path_len) + st_path_len > buffer_size)
			return (size_t)0;
		std::memcpy(StructData + offset, &st_path_len, sizeof(st_path_len));
		offset += sizeof(st_path_len);
		if (st_path_len > 0)
		{
			std::memcpy(StructData + offset, st_path.c_str(), st_path_len);
			offset += st_path_len;
		}

		return offset;
	}
};

/*
* 存储玩家所有数据的容器结构体
* @param advancements_list 进度文件信息
* @param playerdata_list 玩家信息（数据）
* @param stats_list 进度文件信息
* @param playerinworldinfo_list 一次性存储单个玩家的所有数据
*/
struct PlayerInWorldInfoList
{
	/*
	* 进度文件信息
	* @param path 文件路径
	* @param uuid UUID
	*/
	std::vector<PlayerInfo_AS> advancements_list;
	/*
	* 玩家信息（数据）
	* @param dat_path 数据文件路径
	* @param dat_old_path 旧数据文件路径
	* @param cosarmor_path 装饰盔甲数据文件路径
	* @param uuid 数据文件UUID
	* @param old_uuid 旧数据文件UUID
	* @param cosarmor_uuid 装饰盔甲数据文件UUID
	*/
	std::vector<PlayerInfo_Data> playerdata_list;
	/*
	* 进度文件信息
	* @param path 文件路径
	* @param uuid UUID
	*/
	std::vector<PlayerInfo_AS> stats_list;
	/*
	* 一次性存储单个玩家的所有数据
	* @param world_dir_name 存档信息
	* @param player 玩家信息
	* @param adv_path 进度文件路径
	* @param pd_path 数据文件路径
	* @param pd_old_path 旧数据文件路径
	* @param cosarmor_path 装饰盔甲数据文件路径
	* @param st_path 统计文件路径
	*/
	std::vector<PlayerInWorldInfo> playerinworldinfo_list;

	/*
	* 用于序列化PlayerInWorldInfoList结构体的函数
	* @param StructData 传入缓冲区
	* @return 缓冲区大小
	*/
	size_t SerializeToFixedArray(BYTE StructData[SHARED_MEMORY_BUF_SIZE]) const
	{
		const size_t buffer_size = SHARED_MEMORY_BUF_SIZE;
		size_t offset = 0;
		size_t result = 0;

		// 1. 写入advancements_list的大小
		uint32_t advancements_count = static_cast<uint32_t>(advancements_list.size());
		if (offset + sizeof(advancements_count) > buffer_size)
			return (size_t)0;
		std::memcpy(StructData + offset, &advancements_count, sizeof(advancements_count));
		offset += sizeof(advancements_count);

		// 2. 序列化advancements_list中的每个元素
		for (const auto& item : advancements_list)
		{
			result = item.SerializeToFixedArray(StructData + offset);
			if (result == 0) return (size_t)0;
			offset += result;
		}

		// 3. 写入playerdata_list的大小
		uint32_t playerdata_count = static_cast<uint32_t>(playerdata_list.size());
		if (offset + sizeof(playerdata_count) > buffer_size)
			return (size_t)0;
		std::memcpy(StructData + offset, &playerdata_count, sizeof(playerdata_count));
		offset += sizeof(playerdata_count);

		// 4. 序列化playerdata_list中的每个元素
		for (const auto& item : playerdata_list)
		{
			result = item.SerializeToFixedArray(StructData + offset);
			if (result == 0) return (size_t)0;
			offset += result;
		}

		// 5. 写入stats_list的大小
		uint32_t stats_count = static_cast<uint32_t>(stats_list.size());
		if (offset + sizeof(stats_count) > buffer_size)
			return (size_t)0;
		std::memcpy(StructData + offset, &stats_count, sizeof(stats_count));
		offset += sizeof(stats_count);

		// 6. 序列化stats_list中的每个元素
		for (const auto& item : stats_list)
		{
			result = item.SerializeToFixedArray(StructData + offset);
			if (result == 0) return (size_t)0;
			offset += result;
		}

		// 7. 写入playerinworldinfo_list的大小
		uint32_t playerinworldinfo_count = static_cast<uint32_t>(playerinworldinfo_list.size());
		if (offset + sizeof(playerinworldinfo_count) > buffer_size)
			return (size_t)0;
		std::memcpy(StructData + offset, &playerinworldinfo_count, sizeof(playerinworldinfo_count));
		offset += sizeof(playerinworldinfo_count);

		// 8. 序列化playerinworldinfo_list中的每个元素
		for (const auto& item : playerinworldinfo_list)
		{
			result = item.SerializeToFixedArray(StructData + offset);
			if (result == 0) return (size_t)0;
			offset += result;
		}

		return offset;
	}
};

/*
* 共享内存命令传递结构体
* @param Writer 写入者状态
* @param Program 程序状态
* @param DefCommand 执行命令
* @param AdditionaCommand 附加命令
* @param RunStatus 执行状态
* @param ErrorInfo 报错信息
* @param StructDataType 结构体数据类型
* @param StructData 数据缓冲区
*/
struct SharedMemoryCommand
{
	// 写入者状态 枚举WriteStatus
	WriteStatus Writer;
	// 程序状态 枚举ProgramStatus
	LoadMode LoadMode;

	// 执行命令 枚举Command
	Command DefCommand;
	// 附加命令
	char AdditionaCommand[SHARED_MEMORY_BUF_SIZE];

	// 执行状态 枚举RunStatus
	RunStatus RunStatus;
	// 报错信息
	char ErrorInfo[SHARED_MEMORY_BUF_SIZE];

	//标题名称
	char TitleName[SHARED_MEMORY_BUF_SIZE];

	// 结构体数据类型 枚举StructType
	StructType StructDataType;
	// 数据缓冲区
	BYTE StructData[SHARED_MEMORY_BUF_SIZE];
};