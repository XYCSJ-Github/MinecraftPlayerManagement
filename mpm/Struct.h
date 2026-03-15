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