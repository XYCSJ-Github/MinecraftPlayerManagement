#pragma once

#include <vector>
#include <filesystem>
#include <fstream>
#include "Logout.h"
#include "include/nlohmann/json.hpp"

using json = nlohmann::json;

struct WorldDirectoriesNameList
{
	std::vector<std::string> world_directory_list;
	std::vector<std::string> world_name_list;
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
};

struct UnknownPath : public std::exception
{
	const char* what() const throw()
	{
		return "路径不存在，请检查后重新输入！";
	}
};

struct NotOpen : public std::exception
{
	const char* what() const throw()
	{
		return "无法打开文件！";
	}
};

struct ReadError : public std::exception
{
	const char* what() const throw()
	{
		return "读取用文件失败！";
	}
};

WorldDirectoriesNameList GetWorldDirectoriesList(const std::string base_path);
std::string ProcessingInputPath(const std::string input_path);
std::vector<UserInfo> GetUserInfo(const std::string base_path);
std::vector<PlayerInfo_AS> GetWorldPlayerAdvancements(const std::string base_path);
std::vector<PlayerInfo_Data> GetWorldPlayerData(const std::string base_path);
std::vector<PlayerInfo_AS> GetWorldPlayerStats(const std::string base_path);
