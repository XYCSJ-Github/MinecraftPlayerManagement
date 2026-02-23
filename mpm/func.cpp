//func.cpp 包含程序主要功能函数的实现

#pragma warning(disable : 4996)
#include "func.h"

/*
* 获取存档路径列表
* @param base_path 包含存档的文件夹路径
* @param mod 加载模式
* @return 包含存档路径和名称的容器结构体
*/
WorldDirectoriesNameList GetWorldDirectoriesList(const std::string base_path, LoadMode mod)
{
	LOG_CREATE_MODEL_NAME("GetWorldDirectoriesList");//设置logout模块名称

	WorldDirectoriesNameList world_directories_name_list;
	std::string base_path_copy = base_path;

	if (mod == LoadMode::CLIENT)
		base_path_copy += "\\saves\\";//如果传参路径为客户端文件夹则在路径后加\saves\去检查世界

	LOG_DEBUG("最终路径为：" + base_path_copy);
	LOG_DEBUG("路径长度：" + std::to_string(base_path_copy.length()));

	std::string world_name;
	for (const std::filesystem::directory_entry d : std::filesystem::directory_iterator(base_path_copy))
	{
		if (fs::is_directory(d.path()) == false)//如果路径不为空，将其装入WorldDirectoriesNameList
			continue;
		LOG_DEBUG("发现世界目录：" + d.path().string());
		world_directories_name_list.world_directory_list.push_back(d.path().string());
		world_name = d.path().string();
		if (mod == LoadMode::SERVER)//如果是服务器目录，切出目录名会多切一个字符。所以作出判断
		{
			world_name.erase(0, base_path_copy.length() + 1);
		}
		else
		{
			world_name.erase(0, base_path_copy.length());
		}
		LOG_DEBUG("世界名称：" + world_name);
		world_directories_name_list.world_name_list.push_back(world_name);//将世界名称装入WorldDirectoriesNameList
	}

	return world_directories_name_list;
}

/*
* 获取存档路径列表（容器）
* @param base_path 包含存档的文件夹路径
* @param mod 加载模式
* @return 包含存档路径和名称的结构体容器
*/
std::vector<WorldDirectoriesName> GetWorldDirectories(const std::string base_path, LoadMode mod)
{
	LOG_CREATE_MODEL_NAME("GetWorldDirectories");

	WorldDirectoriesName wdn;
	std::vector<WorldDirectoriesName> wdnl;
	std::string base_path_copy = base_path;

	if (mod == LoadMode::CLIENT)
		base_path_copy += "\\saves\\";//如果传参路径为客户端文件夹则在路径后加\saves\去检查世界

	LOG_DEBUG("最终路径为：" + base_path_copy);
	LOG_DEBUG("路径长度：" + std::to_string(base_path_copy.length()));

	std::string world_name;
	for (const std::filesystem::directory_entry d : std::filesystem::directory_iterator(base_path_copy))
	{
		if (fs::is_directory(d.path()) == false)//如果路径不为空，将其装入WorldDirectoriesNameList
			continue;

		LOG_DEBUG("发现世界目录：" + d.path().string());
		wdn.world_directory = d.path().string();
		world_name = d.path().string();
		if (mod == LoadMode::SERVER)//如果是服务器目录，切出目录名会多切一个字符。所以作出判断
		{
			world_name.erase(0, base_path_copy.length() + 1);
		}
		else
		{
			world_name.erase(0, base_path_copy.length());
		}
		LOG_DEBUG("世界名称：" + world_name);
		wdn.world_name = world_name;
		wdnl.push_back(wdn);
	}

	return wdnl;
}

/*
* 处理路径字符串-去除“”
* @param input_path 未处理的路径
* @return 处理后的路径
*/
std::string ProcessingInputPath(const std::string input_path)
{
	LOG_CREATE_MODEL_NAME("PathInput");

	std::string input_path_copy = input_path;

	if (input_path_copy.find('\"') != std::string::npos)//去除路径双括号
	{
		input_path_copy.erase(0, 1);
		input_path_copy.erase(input_path_copy.length() - 1, 1);
	}
	LOG_DEBUG("输入的路径为：" + input_path_copy);

	if (!std::filesystem::exists(input_path_copy))
	{
		LOG_DEBUG("路径不存在，请检查后重新输入！");
		throw UnknownPath();
	}

	return input_path_copy;
}

/*
* 从usercache.json中获取玩家信息
* @param base_path 由ProcessingInputPath()处理后的路径
* @return 包含玩家信息的结构体容器
*/
std::vector<UserInfo> GetUserInfo(const std::string base_path)
{
	LOG_CREATE_MODEL_NAME("GetUserInfo");

	std::string base_path_copy = base_path;
	std::vector<UserInfo> userslist;

	base_path_copy += "\\usercache.json";//在处理后的原始路径上加入文件路径

	LOG_DEBUG("最终路径为：" + base_path_copy);
	LOG_DEBUG("路径长度：" + std::to_string(base_path_copy.length()));

	std::ifstream user_file(base_path_copy);
	json user_info;
	if (user_file.is_open())//将读出数据存入变量
	{
		user_file >> user_info;
		user_file.close();
	}
	else
	{
		LOG_DEBUG("无法打开用户信息文件！");
		throw NotOpen();
	}

	if (user_info.is_array())//检查JSON格式(数组|对象)
	{
		LOG_DEBUG("JSON格式为数组，包含元素" + std::to_string(user_info.size()));

		try
		{
			for (size_t i = 0; i < user_info.size(); i++)//遍历变量将数据存入结构体
			{
				json tmp_data = user_info[i];

				std::string name = tmp_data.at("name").get<std::string>();
				std::string uuid = tmp_data.at("uuid").get<std::string>();
				std::string expiresOn = tmp_data.at("expiresOn").get<std::string>();

				UserInfo users;
				users.user_name = name;
				users.uuid = uuid;
				users.expiresOn = expiresOn;

				userslist.push_back(users);

				LOG_DEBUG("用户 " + std::to_string(i) + ": ");
				LOG_DEBUG("  name: " + name);
				LOG_DEBUG("  uuid: " + uuid);
				LOG_DEBUG("  expiresOn: " + expiresOn);
			}
		}
		catch (const std::exception& e)
		{
			LOG_ERROR(e.what());
			throw ReadError();
		}
	}

	return userslist;
}

/*
* 从Advancements中获取玩家信息
* @param base_path 由ProcessingInputPath()处理后的路径
* @return 包含玩家信息的结构体容器
*/
std::vector<PlayerInfo_AS> GetWorldPlayerAdvancements(const std::string base_path)
{
	LOG_CREATE_MODEL_NAME("GetWorldPlayerAdvancements");
	std::vector<PlayerInfo_AS> pa_list;

	std::string advancements_path = base_path + "\\advancements";//加工世界存档路径，再进一步，读取所有进度文件
	LOG_DEBUG("最终路径为：" + advancements_path);
	LOG_DEBUG("路径长度：" + std::to_string(advancements_path.length()));

	if (!std::filesystem::exists(advancements_path))
	{
		LOG_DEBUG("路径不存在，请检查后重新输入！");
		throw UnknownPath();
	}

	for (const std::filesystem::directory_entry d : std::filesystem::directory_iterator(advancements_path))//遍历容器，每次新建一个结构体，将文件完整路径和裁切出的uuid存入结构体，再将结构体存入容器
	{
		PlayerInfo_AS advancements_list;
		advancements_list.path = d.path().string();
		std::string uuid = d.path().string();
		advancements_list.uuid = uuid.substr(advancements_path.length() + 1);
		advancements_list.uuid.erase(advancements_list.uuid.length() - 5, 5); // 去掉文件后缀名 ".json"
		LOG_DEBUG("发现玩家进度文件：\n路径：" + advancements_list.path + "\nuuid：" + advancements_list.uuid);
		pa_list.push_back(advancements_list);
	}

	return pa_list;//返回包含数据结构体的容器
}

/*
* 从PlayerData中获取玩家信息
* @param base_path 由ProcessingInputPath()处理后的路径
* @return 包含玩家信息的结构体容器
*/
std::vector<PlayerInfo_Data> GetWorldPlayerData(const std::string base_path)
{
	LOG_CREATE_MODEL_NAME("GetWorldPlayerData");
	std::vector<PlayerInfo_Data> pd_list;

	std::string playerdata_path = base_path + "\\playerdata";
	LOG_DEBUG("最终路径为：" + playerdata_path);
	LOG_DEBUG("路径长度：" + std::to_string(playerdata_path.length()));

	if (!std::filesystem::exists(playerdata_path))
	{
		LOG_DEBUG("路径不存在，请检查后重新输入！");
		throw UnknownPath();
	}

	PlayerInfo_Data playerdata_list;

	for (const std::filesystem::directory_entry d : std::filesystem::directory_iterator(playerdata_path))//遍历变量并判断结构体是否填满，存储进容器并清空进行下一个
	{
		std::string datorold = d.path().string();
		datorold.erase(0, d.path().string().length() - 4);
		if (datorold.find(".dat") != std::string::npos)
		{
			playerdata_list.dat_path = d.path().string();
			std::string uuid = d.path().string();
			playerdata_list.uuid = uuid.substr(playerdata_path.length() + 1);
			playerdata_list.uuid.erase(playerdata_list.uuid.length() - 4, 4);
			LOG_DEBUG("发现玩家数据文件：\n路径：" + playerdata_list.dat_path + "\nuuid：" + playerdata_list.uuid);
		}
		if (datorold.find("_old") != std::string::npos)
		{
			playerdata_list.dat_old_path = d.path().string();
			std::string uuid = d.path().string();
			playerdata_list.old_uuid = uuid.substr(playerdata_path.length() + 1);
			playerdata_list.old_uuid.erase(playerdata_list.old_uuid.length() - 8, 8);
			LOG_DEBUG("发现玩家数据文件：\n路径：" + playerdata_list.dat_old_path + "\nuuid：" + playerdata_list.old_uuid);
		}
		if (d.path().string().find(".cosa") != std::string::npos)
		{
			playerdata_list.cosarmor_path = d.path().string();
			std::string uuid = d.path().string();
			playerdata_list.cosarmor_uuid = uuid.substr(playerdata_path.length() + 1);
			playerdata_list.cosarmor_uuid.erase(playerdata_list.cosarmor_uuid.length() - 9, 9);
			LOG_DEBUG("发现玩家数据文件：\n路径：" + playerdata_list.cosarmor_path + "\nuuid：" + playerdata_list.cosarmor_uuid);
		}
		if (!playerdata_list.dat_path.empty() && !playerdata_list.dat_old_path.empty() && !playerdata_list.cosarmor_path.empty())
		{
			if (playerdata_list.uuid == playerdata_list.old_uuid && playerdata_list.uuid == playerdata_list.cosarmor_uuid)
			{
				pd_list.push_back(playerdata_list);
			}
			playerdata_list.cosarmor_path = {};
			playerdata_list.cosarmor_uuid = {};
			playerdata_list.dat_old_path = {};
			playerdata_list.old_uuid = {};
			playerdata_list.dat_path = {};
			playerdata_list.uuid = {};
		}
	}

	return pd_list;
}

/*
* 从Stats中获取玩家信息
* @param base_path 由ProcessingInputPath()处理后的路径
* @return 包含玩家信息的结构体容器
*/
std::vector<PlayerInfo_AS> GetWorldPlayerStats(const std::string base_path)
{
	LOG_CREATE_MODEL_NAME("GetWorldPlayerStats");
	std::vector<PlayerInfo_AS> ps_list;

	std::string stats_path = base_path + "\\stats";
	LOG_DEBUG("最终路径为：" + stats_path);
	LOG_DEBUG("路径长度：" + std::to_string(stats_path.length()));

	if (!std::filesystem::exists(stats_path))
	{
		LOG_DEBUG("路径不存在，请检查后重新输入！");
		throw UnknownPath();
	}

	for (const std::filesystem::directory_entry d : std::filesystem::directory_iterator(stats_path))
	{
		PlayerInfo_AS stats_list;
		stats_list.path = d.path().string();
		std::string uuid = d.path().string();
		stats_list.uuid = uuid.substr(stats_path.length() + 1);
		stats_list.uuid.erase(stats_list.uuid.length() - 5, 5); // 去掉文件后缀名 ".json"
		LOG_DEBUG("发现玩家统计文件：\n路径：" + stats_list.path + "\nuuid：" + stats_list.uuid);
		ps_list.push_back(stats_list);
	}

	return ps_list;
}

/*
* 获取路径最后一级名称
* @param path 需要处理的路径
* @return 最后一级名称
*/
std::string getLastComponent(const std::string& path)
{
	// 处理空路径
	if (path.empty()) return "";

	// 移除末尾的斜杠
	std::string clean_path = path;
	if (!clean_path.empty() && (clean_path.back() == '/' || clean_path.back() == '\\')) {
		clean_path.pop_back();
	}

	// 查找最后一个分隔符
	size_t pos = clean_path.find_last_of("/\\");
	if (pos == std::string::npos) {
		return clean_path;  // 没有分隔符，整个字符串就是最后一层
	}

	return clean_path.substr(pos + 1);
}

/*
* 检测路径下某文件夹是否存在
* @param base_path 需要寻找的文件夹的上一级路径
* @param folder_name 需要寻找的文件夹名称
*/
bool folderExists(const fs::path& base_path, const std::string& folder_name)
{
	fs::path full_path = base_path / folder_name;
	return fs::exists(full_path) && fs::is_directory(full_path);
}

/*
* 用PowerShell删除文件
* @param filepath 文件路径
*/
bool MoveToRecycleBinWithPS(const std::string& filepath)
{
	// PowerShell 命令
	std::string psCommand =
		"powershell -NonInteractive -Command \""
		"$ErrorActionPreference = 'SilentlyContinue';"
		"Add-Type -AssemblyName Microsoft.VisualBasic;"
		"[Microsoft.VisualBasic.FileIO.FileSystem]::DeleteFile('" + filepath + "','OnlyErrorDialogs','SendToRecycleBin');\"";

	// 如果路径包含特殊字符，需要进一步处理
	std::string finalCommand = "cmd /c \"" + psCommand + " >nul 2>&1\"";

	return ExecuteCommand(finalCommand.c_str());
}

/*
* 执行命令 MoveToRecycleBinWithPS()附属
* @param cmd 命令
*/
bool ExecuteCommand(const std::string& cmd)
{
	LOG_DEBUG_M("执行shell命令：" + cmd + "\n", "DeleteFile");
	int result = std::system(cmd.c_str());
	return (result == 0);
}

/*
* 将字符串分割并存入容器
* @param str 待处理的字符串
* @param delimiter 分割参考
* @return 分割好的字符串打包到容器
*/
std::vector<std::string> splitString(const std::string& str, char delimiter)
{
	std::vector<std::string> parts;
	size_t start = 0;
	size_t end = str.find(delimiter);

	while (end != std::string::npos) {
		parts.push_back(str.substr(start, end - start));
		start = end + 1;
		end = str.find(delimiter, start);
	}
	parts.push_back(str.substr(start));

	return parts;
}

/*
* 从两个json中删除数据
* @param JSON_path json文件路径
* @param playerName 玩家名字
*/
bool DeletePlayerJSON(std::string JSON_path, std::string playerName)
{
	if (DeletePlayerInUserCache(JSON_path + "\\usercache.json", playerName) && DeletePlayerInUserNmaeCache(JSON_path + "\\usernamecache.json", playerName))
	{
		return true;
	}

	return false;
}

/*
* 从UserCache json中删除数据
* @param JSON_path json文件路径
* @param playerName 玩家名字
*/
bool DeletePlayerInUserCache(std::string JSON_path, std::string playerName)
{
	LOG_CREATE_MODEL_NAME("DeletePlayerJSON");

	try
	{
		// 1. 读取JSON文件
		std::ifstream in_file(JSON_path);
		if (!in_file.is_open())
		{
			LOG_ERROR("无法打开文件: " + JSON_path);
			return false;
		}

		json j;
		in_file >> j;
		in_file.close();

		if (!j.is_array()) {
			LOG_ERROR("JSON格式错误：应该是一个数组");
			return false;
		}

		// 2. 查找并删除指定玩家
		bool found = false;
		for (auto it = j.begin(); it != j.end(); )
		{
			std::string a = (*it)["name"];
			if (it->contains("name") && a == playerName)
			{
				it = j.erase(it);
				found = true;
				LOG_DEBUG("已删除玩家: " + playerName);
			}
			else {
				++it;
			}
		}

		// 3. 写回文件
		if (found) {
			std::ofstream out_file(JSON_path);
			out_file << j.dump();
			out_file.close();
			return true;
		}
		else {
			LOG_DEBUG("未找到玩家:" + playerName);
			return false;
		}

	}
	catch (const json::exception& e) {
		LOG_ERROR(e.what());
		return false;
	}
	catch (const std::exception& e) {
		LOG_ERROR(e.what());
		return false;
	}


}

/*
* 从UserNmaeCache json中删除数据
* @param JSON_path json文件路径
* @param playerName 玩家名字
*/
bool DeletePlayerInUserNmaeCache(std::string JSON_path, std::string playerName)
{
	LOG_CREATE_MODEL_NAME("DeletePlayerJSON");

	try {
		LOG_DEBUG("正在删除玩家: " + playerName);

		// 1. 读取JSON文件
		std::ifstream in_file(JSON_path);
		if (!in_file.is_open()) {
			LOG_ERROR("无法打开文件: " + JSON_path);
			return false;
		}

		json j;
		in_file >> j;
		in_file.close();

		// 2. 检查是否为对象（键值对）
		if (!j.is_object()) {
			LOG_ERROR("JSON格式错误：应该是对象类型");
			return false;
		}

		LOG_DEBUG("原始键值对数量: " + std::to_string(j.size()));

		// 3. 查找并删除（反向查找：通过值找键）
		bool found = false;
		std::string foundUUID;

		for (auto it = j.begin(); it != j.end(); ) {
			// 检查值是否为字符串且等于目标玩家名
			if (it.value().is_string() && it.value().get<std::string>() == playerName) {
				foundUUID = it.key();
				it = j.erase(it);
				found = true;
				LOG_DEBUG("删除玩家：" + playerName + "|UUID：" + foundUUID + "\n");
			}
			else {
				++it;
			}
		}

		// 4. 写回文件
		if (found) {
			std::ofstream out_file(JSON_path);
			out_file << j.dump(4);
			out_file.close();

			LOG_DEBUG("删除成功！剩余键值对：" + std::to_string(j.size()));
			return true;
		}

		return false;
	}
	catch (const json::exception& e) {
		LOG_ERROR(e.what());
		return false;
	}
	catch (const std::exception& e) {
		LOG_ERROR(e.what());
		return false;
	}
}