#include <vector>
#include "Logout.h"
#include "func.h"

int main()
{
#if _DEBUG
LOG_DEBUG_OUT
#endif
	
	LOG_CREATE_MODEL_NAME(model_name, "Main");

	std::string world_path = {};
	bool mRun = true;

	while (mRun)
	{
		std::cout << "打开客户端文件夹：";
		std::getline(std::cin, world_path);

		std::string pip;
		try
		{
			pip = ProcessingInputPath(world_path);
		}
		catch (const std::exception& e)
		{
			LOG_ERROR(e.what(), model_name);
			break;
		}

		WorldDirectoriesNameList world_name_list = GetWorldDirectoriesList(pip);
		std::vector<UserInfo> user_info_list;

		for (int i = 0; i < world_name_list.world_name_list.size(); ++i)
		{
			LOG_INFO("\n存档名称：" + world_name_list.world_name_list[i] + "\n存档路径" + world_name_list.world_directory_list[i] + "\n", model_name);
		}

		try
		{
			user_info_list = GetUserInfo(pip);
			if (user_info_list.size() == 0)
			{
				LOG_INFO("未找到用户信息！", model_name);
			}
			else
			{
				for (const auto& user_info : user_info_list)
				{
					LOG_INFO("\n用户名：" + user_info.user_name + "\nUUID：" + user_info.uuid + "\n过期时间：" + user_info.expiresOn + "\n", model_name);
				}
			}
		}
		catch (const std::exception& e)
		{
			LOG_ERROR(e.what(), model_name);
		}

		while (true)
		{
			std::string comm;
			std::getline(std::cin, comm);
			LOG_CREATE_MODEL_NAME(model_name, "CommandProcessing");

			if (comm == "exit")
			{
				LOG_DEBUG("执行退出", model_name);
				mRun = false;
				break;
			}
			else if(comm == "break")
			{
				LOG_DEBUG("执行返回", model_name);
				break;
			}

			std::string pc = comm.substr(0, 9);
			if (pc == "OpenWorld"|| pc == "openworld")
			{
				LOG_DEBUG("执行打开存档命令", model_name);
				LOG_DEBUG("识别命令：" + pc, model_name);


				std::string ow = comm.substr(10);
				LOG_DEBUG("打开存档：" + ow, model_name);
				for (int i = 0; i < world_name_list.world_name_list.size(); ++i)
				{
					if (ow == world_name_list.world_name_list[i])
					{
						std::string open_path = world_name_list.world_directory_list[i];
						LOG_INFO("正在打开存档：" + open_path, model_name);

						std::vector<PlayerInfo_ADS> advancements_list;
						std::vector<PlayerInfo_ADS> playerdata_list;
						std::vector<PlayerInfo_ADS> stats_list;

						try
						{
							advancements_list = GetWorldPlayerAdvancements(open_path);
							playerdata_list = GetWorldPlayerData(open_path);
							stats_list = GetWorldPlayerStats(open_path);
						}
						catch (const std::exception& e)
						{
							LOG_ERROR(e.what(), model_name);
						}

						LOG_INFO("存档打开完成！", model_name);

						for (int i = 0; i < advancements_list.size(); i++)
						{
							for (int j = 0; j < user_info_list.size(); j++)
							{
								if (user_info_list[j].uuid == advancements_list[i].uuid)
								{
									LOG_INFO("进度信息：\n玩家：" + user_info_list[j].user_name + "\nUUID：" + user_info_list[j].uuid + "\n文件路径：" + advancements_list[i].path + "\n", model_name);
								}
							}
						}

						for (int i = 0; i < playerdata_list.size(); i++)
						{
							for (int j = 0; j < user_info_list.size(); j++)
							{
								if (user_info_list[j].uuid == playerdata_list[i].uuid)
								{
									LOG_INFO("玩家数据信息：\n玩家：" + user_info_list[j].user_name + "\nUUID：" + user_info_list[j].uuid + "\n文件路径：" + playerdata_list[i].path + "\n", model_name);
								}
							}
						}

						for (int i = 0; i < stats_list.size(); i++)
						{
							for (int j = 0; j < user_info_list.size(); j++)
							{
								if (user_info_list[j].uuid == stats_list[i].uuid)
								{
									LOG_INFO("统计数据信息：\n玩家：" + user_info_list[j].user_name + "\nUUID：" + user_info_list[j].uuid + "\n文件路径：" + stats_list[i].path + "\n", model_name);
								}
							}
						}

						break;
					}
				}
			}
			else
			{
				std::cout << "输入错误，请重新输入！(exit-退出 or break-返回)" << std::endl;
			}
		}
	}

	system("pause");
	return 0;
}