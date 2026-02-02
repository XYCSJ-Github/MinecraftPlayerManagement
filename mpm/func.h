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


/*
* 加载模式
* @param CLIENT 客户端模式
* @param SERVER 服务器模式
*/
enum LoadMode
{
	//无
	EMPTY,
	//客户端模式
	CLIENT,
	//服务器模式
	SERVER
};

//JSON读写对象重定义
using json = nlohmann::json;
//文件系统命名空间重定义
namespace fs = std::filesystem;

/*
* 获得存档目录路径
* @param base_path 处理后的路径
* @param mod 加载模式-是否在路径后加入存档文件夹路径
* @return 包含路径和名称的容器的结构体
*/
WorldDirectoriesNameList GetWorldDirectoriesList(const std::string base_path, LoadMode mod);
/*
* 获得存档目录路径
* @param base_path 处理后的路径
* @param mod 加载模式-是否在路径后加入存档文件夹路径
* @return 包含路径和名称的结构体的容器
*/
std::vector<WorldDirectoriesName> GetWorldDirectories(const std::string base_path, LoadMode mod);
/*
* 预处理路径，去除有双引号
* @param input_path 原始路径
* @return 处理好的路径
*/
std::string ProcessingInputPath(const std::string input_path);
/*
* 加工原始路径，读取用户缓存文件
* @param base_path 处理后的路径
* @return 包含用户名和uuid的结构体容器
*/
std::vector<UserInfo> GetUserInfo(const std::string base_path);
/*
* 加工存档路径，识别文件路径并裁切对应的uuid
* @param base_path 处理后的路径
* @return 返回包含文件路径和对应uuid结构体的容器
*/
std::vector<PlayerInfo_AS> GetWorldPlayerAdvancements(const std::string base_path);
/*
* 加工存档路径，识别文件路径并裁切对应的uuid
* @param base_path 处理后的路径
* @return 返回包含文件路径和对应uuid结构体的容器
*/
std::vector<PlayerInfo_Data> GetWorldPlayerData(const std::string base_path);
/*
* 加工存档路径，识别文件路径并裁切对应的uuid
* @param base_path 处理后的路径
* @return 返回包含文件路径和对应uuid结构体的容器
*/
std::vector<PlayerInfo_AS> GetWorldPlayerStats(const std::string base_path);
/*
* 提取路径最后一级的名称
* @param path 处理后的路径
* @return 最后一级的名称
*/
std::string getLastComponent(const std::string& path);
/*
* 查找目录名称
* @param base_path 需要查找的目录路径
* @param folder_name 需要查找的目录名称
* @return 是否存在
*/
bool folderExists(const fs::path& base_path, const std::string& folder_name);
/*
* 用powershell删除文件
* @param filepath 要删除的文件路径
* @return 成功与否
*/
bool MoveToRecycleBinWithPS(const std::string& filepath);
bool ExecuteCommand(const std::string& cmd);//与上一个绑定，用于执行参数
/*
* 分割字符串
* @param str 要处理的字符串
* @param delimiter 分割参考
* @return 分割好的字符串容器
*/
std::vector<std::string> splitString(const std::string& str, char delimiter);
/*
* 从usercache、usernamecache中删除玩家
* @param JSON_path JSON文件路径
* @param playerName 玩家名称
* @return 成功与否
*/
bool DeletePlayerJSON(std::string JSON_path, std::string playerName);
/*
* 从usercache中删除玩家
* @param JSON_path JSON文件路径
* @param playerName 玩家名称
* @return 成功与否
*/
bool DeletePlayerInUserCache(std::string JSON_path, std::string playerName);
/*
* 从usernamecache中删除玩家
* @param JSON_path JSON文件路径
* @param playerName 玩家名称
* @return 成功与否
*/
bool DeletePlayerInUserNmaeCache(std::string JSON_path, std::string playerName);
