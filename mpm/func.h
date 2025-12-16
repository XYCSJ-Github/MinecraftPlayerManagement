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
};

WorldDirectoriesNameList GetWorldDirectoriesList(const std::string base_path);
std::string ProcessingInputPath(const std::string input_path);
void GetUserInfo(const std::string base_path);
