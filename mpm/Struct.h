#pragma once

#include <string>
#include <vector>

struct WorldDirectoriesNameList
{
	std::vector<std::string> world_directory_list;
	std::vector<std::string> world_name_list;
};

struct WorldDirectoriesName
{
	std::string world_directory;
	std::string world_name;
};

struct UserInfo
{
	std::string user_name;
	std::string uuid;
	std::string expiresOn;
};

struct PlayerInfo_AS
{
	std::string path;
	std::string uuid;
};

struct PlayerInfo_Data
{
	std::string dat_path;
	std::string dat_old_path;
	std::string cosarmor_path;
	std::string uuid;
	std::string old_uuid;
	std::string cosarmor_uuid;
};

struct playerinworldinfo
{
	std::string worldname;
	std::string uuid;
	std::string adv_path;
	std::string pd_path;
	std::string pd_old_path;
	std::string cosarmor_path;
	std::string st_path;
};