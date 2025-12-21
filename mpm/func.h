//func.h 声明主要功能函数
#pragma once

#include "include/nlohmann/json.hpp"
#include "Logout.h"
#include "Struct.h"
#include "throw_error.h"
#include <filesystem>
#include <fstream>
#include <shellapi.h>
#include <windows.h>

#define MOD_CLIENT  101
#define MOD_SERVER  102

using json = nlohmann::json;
namespace fs = std::filesystem;

WorldDirectoriesNameList GetWorldDirectoriesList(const std::string base_path, int mod);//获得存档目录路径|返回包含路径和名称的容器的结构体
std::string ProcessingInputPath(const std::string input_path);//预处理路径，去除有双引号。|返回字符串
std::vector<UserInfo> GetUserInfo(const std::string base_path);//加工原始路径，读取用户缓存文件。|返回包含用户名和uuid的结构体容器
std::vector<PlayerInfo_AS> GetWorldPlayerAdvancements(const std::string base_path);//加工存档路径，识别文件路径并裁切对应的uuid|返回包含文件路径和对应uuid结构体的容器
std::vector<PlayerInfo_Data> GetWorldPlayerData(const std::string base_path);//加工存档路径，识别文件路径并裁切对应的uuid|返回包含文件路径和对应uuid结构体的容器
std::vector<PlayerInfo_AS> GetWorldPlayerStats(const std::string base_path);//加工存档路径，识别文件路径并裁切对应的uuid|返回包含文件路径和对应uuid结构体的容器
std::string getLastComponent(const std::string& path);//提取路径最后一级的名称|返回字符串
bool folderExists(const fs::path& base_path, const std::string& folder_name);//查找目录名称
bool MoveToRecycleBinWithPS(const std::string& filepath);//用powershell删除文件
bool ExecuteCommand(const std::string& cmd);//与上一个绑定，用于执行参数
std::vector<std::string> splitString(const std::string& str, char delimiter);//分割字符串|返回数组
bool DeletePlayerJSON(std::string JSON_path, std::string playerName);
bool DeletePlayerInUserCache(std::string JSON_path, std::string playerName);//从usercache中删除玩家
bool DeletePlayerInUserNmaeCache(std::string JSON_path, std::string playerName);//从usernamecache中删除玩家
