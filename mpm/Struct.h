//Struct.h 声明结构体
#pragma once

#include <string>
#include <vector>

struct WorldDirectoriesNameList//世界路径列表与名称列表
{
	std::vector<std::string> world_directory_list;
	std::vector<std::string> world_name_list;
};

struct WorldDirectoriesName//世界路径与名称
{
	std::string world_directory;
	std::string world_name;
};

struct UserInfo//玩家名称uuid和过期时间
{
	std::string user_name;
	std::string uuid;
	std::string expiresOn;
};

struct PlayerInfo_AS//文件路径与uuid
{
	std::string path;
	std::string uuid;
};

struct PlayerInfo_Data//同分类文件的路径与uuid
{
	std::string dat_path;
	std::string dat_old_path;
	std::string cosarmor_path;
	std::string uuid;
	std::string old_uuid;
	std::string cosarmor_uuid;
};

struct playerinworldinfo//一次性存储单个玩家的所有数据
{
	WorldDirectoriesName world_dir_name;
	UserInfo player;
	std::string adv_path;
	std::string pd_path;
	std::string pd_old_path;
	std::string cosarmor_path;
	std::string st_path;
};

struct PlayerInWorldInfoList//存储玩家所有数据的容器结构体
{
	std::vector<PlayerInfo_AS> advancements_list;
	std::vector<PlayerInfo_Data> playerdata_list;
	std::vector<PlayerInfo_AS> stats_list;
	std::vector<playerinworldinfo> playerinworldinfo_list;
};