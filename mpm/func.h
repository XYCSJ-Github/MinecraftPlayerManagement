#pragma once

#include "include/nlohmann/json.hpp"
#include "Logout.h"
#include "throw_error.h"
#include "Struct.h"
#include <filesystem>
#include <fstream>
#include <shellapi.h>
#include <vector>
#include <windows.h>

#define MOD_CLIENT 0
#define MOD_SERVER 1

using json = nlohmann::json;
namespace fs = std::filesystem;

WorldDirectoriesNameList GetWorldDirectoriesList(const std::string base_path, int mod);
std::string ProcessingInputPath(const std::string input_path);
std::vector<UserInfo> GetUserInfo(const std::string base_path);
std::vector<PlayerInfo_AS> GetWorldPlayerAdvancements(const std::string base_path);
std::vector<PlayerInfo_Data> GetWorldPlayerData(const std::string base_path);
std::vector<PlayerInfo_AS> GetWorldPlayerStats(const std::string base_path);
std::string getLastComponent(const std::string& path);
bool folderExists(const fs::path& base_path, const std::string& folder_name);
bool isPathValid(const std::string& pathStr);
bool MoveToRecycleBinWithPS(const std::string& filepath);
bool ExecuteCommand(const std::string& cmd);
std::vector<std::string> splitString(const std::string& str, char delimiter);