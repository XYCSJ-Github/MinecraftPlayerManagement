#pragma warning(disable : 28159)
#pragma warning(disable : 4996)
#include "func.h"

WorldDirectoriesNameList GetWorldDirectoriesList(const std::string base_path, int mode)
{
	LOG_CREATE_MODEL_NAME(model_name, "GetWorldDirectoriesList");

	WorldDirectoriesNameList world_directories_name_list;
	std::string base_path_copy = base_path;

	if (mode == MOD_CLIENT)
		base_path_copy += "\\saves\\";

	LOG_DEBUG("最终路径为：" + base_path_copy, model_name);
	LOG_DEBUG("路径长度：" + std::to_string(base_path_copy.length()), model_name);

	std::string world_name;
	for (const auto d : std::filesystem::directory_iterator(base_path_copy))
	{
		if (fs::is_directory(d.path()) == false)
			continue;
		LOG_DEBUG("发现世界目录：" + d.path().string(), model_name);
		world_directories_name_list.world_directory_list.push_back(d.path().string());
		world_name = d.path().string();
		if (mode == MOD_SERVER)
		{
			world_name.erase(0, base_path_copy.length() + 1);
		}
		else
		{
			world_name.erase(0, base_path_copy.length());
		}
		LOG_DEBUG("世界名称：" + world_name, model_name);
		world_directories_name_list.world_name_list.push_back(world_name);
	}

	return world_directories_name_list;
}

std::string ProcessingInputPath(const std::string input_path)
{
	LOG_CREATE_MODEL_NAME(model_name, "PathInput");

	std::string input_path_copy = input_path;

	if (input_path_copy.find('\"') != std::string::npos)
	{
		input_path_copy.erase(0, 1);
		input_path_copy.erase(input_path_copy.length() - 1, 1);
	}
	LOG_DEBUG("输入的路径为：" + input_path_copy, model_name);

	if (!std::filesystem::exists(input_path_copy))
	{
		LOG_DEBUG("路径不存在，请检查后重新输入！", model_name);
		throw UnknownPath();
	}

	return input_path_copy;
}

std::vector<UserInfo> GetUserInfo(const std::string base_path)
{
	LOG_CREATE_MODEL_NAME(model_name, "GetUserInfo");

	std::string base_path_copy = base_path;
	std::vector<UserInfo> userslist;

	base_path_copy += "\\usercache.json";

	LOG_DEBUG("最终路径为：" + base_path_copy, model_name);
	LOG_DEBUG("路径长度：" + std::to_string(base_path_copy.length()), model_name);

	std::ifstream user_file(base_path_copy);
	json user_info;
	if (user_file.is_open())
	{
		user_file >> user_info;
		user_file.close();
	}
	else
	{
		LOG_DEBUG("无法打开用户信息文件！", model_name);
		throw NotOpen();
	}

	if (user_info.is_array())
	{
		std::string op = "JSON格式为数组，包含元素" + std::to_string(user_info.size());
		LOG_DEBUG(op, model_name);

		try
		{
			for (size_t i = 0; i < user_info.size(); i++)
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

				LOG_DEBUG("用户 " + std::to_string(i) + ": ", model_name);
				LOG_DEBUG("  name: " + name, model_name);
				LOG_DEBUG("  uuid: " + uuid, model_name);
				LOG_DEBUG("  expiresOn: " + expiresOn, model_name);
			}
		}
		catch (const std::exception& e)
		{
			LOG_ERROR(e.what(), model_name);
			throw ReadError();
		}
	}

	return userslist;
}

std::vector<PlayerInfo_AS> GetWorldPlayerAdvancements(const std::string base_path)
{
	LOG_CREATE_MODEL_NAME(model_name, "GetWorldPlayerAdvancements");
	std::vector<PlayerInfo_AS> pa_list;

	std::string advancements_path = base_path + "\\advancements";
	LOG_DEBUG("最终路径为：" + advancements_path, model_name);
	LOG_DEBUG("路径长度：" + std::to_string(advancements_path.length()), model_name);

	if (!std::filesystem::exists(advancements_path))
	{
		LOG_DEBUG("路径不存在，请检查后重新输入！", model_name);
		throw UnknownPath();
	}

	for (const auto d : std::filesystem::directory_iterator(advancements_path))
	{
		PlayerInfo_AS advancements_list;
		advancements_list.path = d.path().string();
		std::string uuid = d.path().string();
		advancements_list.uuid = uuid.substr(advancements_path.length() + 1);
		advancements_list.uuid.erase(advancements_list.uuid.length() - 5, 5); // 去掉文件后缀名 ".json"
		LOG_DEBUG("发现玩家进度文件：\n路径：" + advancements_list.path + "\nuuid：" + advancements_list.uuid, model_name);
		pa_list.push_back(advancements_list);
	}

	return pa_list;
}

std::vector<PlayerInfo_Data> GetWorldPlayerData(const std::string base_path)
{
	LOG_CREATE_MODEL_NAME(model_name, "GetWorldPlayerData");
	std::vector<PlayerInfo_Data> pd_list;

	std::string playerdata_path = base_path + "\\playerdata";
	LOG_DEBUG("最终路径为：" + playerdata_path, model_name);
	LOG_DEBUG("路径长度：" + std::to_string(playerdata_path.length()), model_name);

	if (!std::filesystem::exists(playerdata_path))
	{
		LOG_DEBUG("路径不存在，请检查后重新输入！", model_name);
		throw UnknownPath();
	}

	PlayerInfo_Data playerdata_list;

	for (const auto d : std::filesystem::directory_iterator(playerdata_path))
	{
		std::string datorold = d.path().string();
		datorold.erase(0, d.path().string().length() - 4);
		if (datorold.find(".dat") != std::string::npos)
		{
			playerdata_list.dat_path = d.path().string();
			std::string uuid = d.path().string();
			playerdata_list.uuid = uuid.substr(playerdata_path.length() + 1);
			playerdata_list.uuid.erase(playerdata_list.uuid.length() - 4, 4);
			LOG_DEBUG("发现玩家数据文件：\n路径：" + playerdata_list.dat_path + "\nuuid：" + playerdata_list.uuid, model_name);
		}
		if (datorold.find("_old") != std::string::npos)
		{
			playerdata_list.dat_old_path = d.path().string();
			std::string uuid = d.path().string();
			playerdata_list.old_uuid = uuid.substr(playerdata_path.length() + 1);
			playerdata_list.old_uuid.erase(playerdata_list.old_uuid.length() - 8, 8);
			LOG_DEBUG("发现玩家数据文件：\n路径：" + playerdata_list.dat_old_path + "\nuuid：" + playerdata_list.old_uuid, model_name);
		}
		if (d.path().string().find(".cosa") != std::string::npos)
		{
			playerdata_list.cosarmor_path = d.path().string();
			std::string uuid = d.path().string();
			playerdata_list.cosarmor_uuid = uuid.substr(playerdata_path.length() + 1);
			playerdata_list.cosarmor_uuid.erase(playerdata_list.cosarmor_uuid.length() - 9, 9);
			LOG_DEBUG("发现玩家数据文件：\n路径：" + playerdata_list.cosarmor_path + "\nuuid：" + playerdata_list.cosarmor_uuid, model_name);
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

std::vector<PlayerInfo_AS> GetWorldPlayerStats(const std::string base_path)
{
	LOG_CREATE_MODEL_NAME(model_name, "GetWorldPlayerStats");
	std::vector<PlayerInfo_AS> ps_list;

	std::string stats_path = base_path + "\\stats";
	LOG_DEBUG("最终路径为：" + stats_path, model_name);
	LOG_DEBUG("路径长度：" + std::to_string(stats_path.length()), model_name);

	if (!std::filesystem::exists(stats_path))
	{
		LOG_DEBUG("路径不存在，请检查后重新输入！", model_name);
		throw UnknownPath();
	}

	for (const auto d : std::filesystem::directory_iterator(stats_path))
	{
		PlayerInfo_AS stats_list;
		stats_list.path = d.path().string();
		std::string uuid = d.path().string();
		stats_list.uuid = uuid.substr(stats_path.length() + 1);
		stats_list.uuid.erase(stats_list.uuid.length() - 5, 5); // 去掉文件后缀名 ".json"
		LOG_DEBUG("发现玩家统计文件：\n路径：" + stats_list.path + "\nuuid：" + stats_list.uuid, model_name);
		ps_list.push_back(stats_list);
	}

	return ps_list;
}

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

bool folderExists(const fs::path& base_path, const std::string& folder_name) 
{
	fs::path full_path = base_path / folder_name;
	return fs::exists(full_path) && fs::is_directory(full_path);
}

bool isPathValid(const std::string& pathStr) 
{
	try {
		// 构造 path 对象，这会进行基本的语法检查
		fs::path p(pathStr);

		// 检查路径是否为空
		if (p.empty()) {
			return false;
		}

		// 检查是否有非法字符（Windows 特别需要注意）
		// path 构造时会自动处理转义，但我们可以检查一些明显的问题

		return true;
	}
	catch (const fs::filesystem_error& e) {
		std::cerr << "Filesystem error: " << e.what() << std::endl;
		return false;
	}
	catch (...) {
		return false;
	}
}

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

bool ExecuteCommand(const std::string& cmd) 
{
	LOG_DEBUG("执行shell命令：" + cmd + "\n", "DeleteFile");
	int result = std::system(cmd.c_str());
	return (result == 0);
}

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