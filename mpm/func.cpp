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
		LOG_WARNING("路径不存在，请检查后重新输入！", "PathInput");
	}

	return input_path_copy;
}

void GetUserInfo(const std::string base_path)
{
	LOG_CREATE_MODEL_NAME(model_name, "GetUserInfo");

	std::string base_path_copy = base_path;

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
		LOG_ERROR("无法打开用户信息文件！", model_name);
		return;
	}

	try
	{
		LOG_DEBUG(user_info["name"], model_name);
		LOG_DEBUG(user_info["uuid"], model_name);
	}
	catch (const std::exception& e)
	{
		LOG_ERROR(e.what(), model_name);
	}



	return;
}
