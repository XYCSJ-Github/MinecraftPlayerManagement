#include "func.h"

WorldDirectoriesNameList GetWorldDirectoriesList(const std::string base_path)
{
	LOG_CREATE_MODEL_NAME(model_name, "GetWorldDirectoriesList");

	WorldDirectoriesNameList world_directories_name_list;
	std::string base_path_copy = base_path;

	base_path_copy += "\\saves\\";
	LOG_DEBUG("最终路径为：" + base_path_copy, model_name);
	LOG_DEBUG("路径长度：" + std::to_string(base_path_copy.length()), model_name);

	std::string world_name;
	for (const auto d : std::filesystem::directory_iterator(base_path_copy))
	{
		LOG_DEBUG("发现世界目录：" + d.path().string(), model_name);
		world_directories_name_list.world_directory_list.push_back(d.path().string());
		world_name = d.path().string();
		world_name.erase(0, base_path_copy.length());
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

std::vector<PlayerInfo_ADS> GetWorldPlayerAdvancements(const std::string base_path)
{
	LOG_CREATE_MODEL_NAME(model_name, "GetWorldPlayerAdvancements");
	std::vector<PlayerInfo_ADS> pa_list;

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
		PlayerInfo_ADS advancements_list;
		advancements_list.path = d.path().string();
		std::string uuid = d.path().string();
		advancements_list.uuid = uuid.substr(advancements_path.length() + 1);
		advancements_list.uuid.erase(advancements_list.uuid.length() - 5, 5); // 去掉文件后缀名 ".json"
		LOG_DEBUG("发现玩家进度文件：\n路径：" + advancements_list.path + "\nuuid：" + advancements_list.uuid, model_name);
		pa_list.push_back(advancements_list);
	}

	return pa_list;
}

std::vector<PlayerInfo_ADS> GetWorldPlayerData(const std::string base_path)
{
	LOG_CREATE_MODEL_NAME(model_name, "GetWorldPlayerData");
	std::vector<PlayerInfo_ADS> pd_list;

	std::string playerdata_path = base_path + "\\playerdata";
	LOG_DEBUG("最终路径为：" + playerdata_path, model_name);
	LOG_DEBUG("路径长度：" + std::to_string(playerdata_path.length()), model_name);

	if (!std::filesystem::exists(playerdata_path))
	{
		LOG_DEBUG("路径不存在，请检查后重新输入！", model_name);
		throw UnknownPath();
	}

	for (const auto d : std::filesystem::directory_iterator(playerdata_path))
	{
		PlayerInfo_ADS playerdata_list, playerdata_lod_list;
		playerdata_list.path = d.path().string();
		std::string uuid = d.path().string();
		playerdata_list.uuid = uuid.substr(playerdata_path.length() + 1);

		if (playerdata_list.uuid == "lod.json")
		{

		}
		playerdata_list.uuid.erase(playerdata_list.uuid.length() - 5, 5); // 去掉文件后缀名 ".json"
		LOG_DEBUG("发现玩家数据文件：\n路径：" + playerdata_list.path + "\nuuid：" + playerdata_list.uuid, model_name);
		pd_list.push_back(playerdata_list);
	}

	return pd_list;
}

std::vector<PlayerInfo_ADS> GetWorldPlayerStats(const std::string base_path)
{
	LOG_CREATE_MODEL_NAME(model_name, "GetWorldPlayerStats");
	std::vector<PlayerInfo_ADS> ps_list;

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
		PlayerInfo_ADS stats_list;
		stats_list.path = d.path().string();
		std::string uuid = d.path().string();
		stats_list.uuid = uuid.substr(stats_path.length() + 1);
		stats_list.uuid.erase(stats_list.uuid.length() - 5, 5); // 去掉文件后缀名 ".json"
		LOG_DEBUG("发现玩家统计文件：\n路径：" + stats_list.path + "\nuuid：" + stats_list.uuid, model_name);
		ps_list.push_back(stats_list);
	}

	return ps_list;
}
