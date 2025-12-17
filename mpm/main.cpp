#include "func.h"
#include "Logout.h"
#include <vector>

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

		LOG_INFO("打开客户端中...", model_name);

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
		OpenWorldWhile:
			std::string comm;
			std::getline(std::cin, comm);
			LOG_CREATE_MODEL_NAME(model_name, "CommandProcessing");

			if (comm == "exit")
			{
				LOG_DEBUG("识别命令：" + comm, model_name);
				mRun = false;
				break;
			}

			if (comm == "break")
			{
				LOG_DEBUG("识别命令：" + comm, model_name);
				break;
			}

			std::string pc = comm.substr(0, 4);
			if (pc == "open")
			{
				LOG_DEBUG("识别命令：" + pc, model_name);
				std::string ow;
				try
				{
					ow = comm.substr(5);
				}
				catch (const std::exception&)
				{
					LOG_ERROR(pc + "<-[HERE]", model_name);
					goto OpenWorldWhile;
				}

				std::string ppc = comm.substr(5, 5);
				if (ppc == "world")
				{
					LOG_DEBUG("识别命令：" + ppc, model_name);
					try
					{
						ow = comm.substr(11);
					}
					catch (const std::exception&)
					{
						LOG_ERROR(ppc + "<-[HERE]", model_name);
						goto OpenWorldWhile;
					}

					LOG_DEBUG("打开存档：" + ow, model_name);
					for (int i = 0; i < world_name_list.world_name_list.size(); ++i)
					{
						if (ow == world_name_list.world_name_list[i])
						{
							std::string open_path = world_name_list.world_directory_list[i];
							LOG_INFO("正在打开存档：" + open_path, model_name);

							std::vector<PlayerInfo_AS> c_advancements_list;
							std::vector<PlayerInfo_Data> c_playerdata_list;
							std::vector<PlayerInfo_AS> c_stats_list;

							try
							{
								c_advancements_list = GetWorldPlayerAdvancements(open_path);
								c_playerdata_list = GetWorldPlayerData(open_path);
								c_stats_list = GetWorldPlayerStats(open_path);
							}
							catch (const std::exception& e)
							{
								LOG_ERROR(e.what(), model_name);
							}

							LOG_INFO("存档打开完成！", model_name);

							for (int i = 0; i < user_info_list.size(); i++)
							{
								std::string adv_path = {}, pd_path = {}, pd_old_path = {}, cosarmor_path = {}, st_path = {};
								for (int j = 0; j < c_advancements_list.size(); j++)
								{
									if (user_info_list[i].uuid == c_advancements_list[j].uuid)
									{
										adv_path = c_advancements_list[j].path;
									}
								}

								for (int j = 0; j < c_playerdata_list.size(); j++)
								{
									if (user_info_list[i].uuid == c_playerdata_list[j].uuid)
									{
										pd_path = c_playerdata_list[j].dat_path;
										pd_old_path = c_playerdata_list[j].dat_old_path;
										cosarmor_path = c_playerdata_list[j].cosarmor_path;
									}
								}

								for (int j = 0; j < c_stats_list.size(); j++)
								{
									if (user_info_list[i].uuid == c_stats_list[j].uuid)
									{
										st_path = c_stats_list[j].path;
									}
								}

								if (adv_path.length() != 0 || pd_path.length() != 0 || st_path.length() != 0)
								{
									std::string out = "\n玩家 " + user_info_list[i].user_name + "\nUUID：" + user_info_list[i].uuid + "\n成就：" + adv_path + "\n玩家数据：" + pd_path;
									if (pd_old_path.length() != 0)
									{
										out += "\n旧玩家数据：" + pd_old_path;
									}
									if (cosarmor_path.length() != 0)
									{
										out += "\n装饰盔甲数据：" + cosarmor_path;
									}

									out += "\n玩家统计：" + st_path + "\n";

									LOG_INFO(out, model_name);
								}
							}
						}
					}
					
				}

				ppc = comm.substr(5, 5);
				if (ppc == "player")
				{
					LOG_DEBUG("识别命令：" + ppc, model_name);
					std::string ow;
					try
					{
						ow = comm.substr(12);
					}
					catch (const std::exception&)
					{
						LOG_ERROR(ppc + "<-[HERE]", model_name);
						goto OpenWorldWhile;
					}

					LOG_DEBUG("打开玩家：" + ow, model_name);
					//TODO: 打开玩家数据
				}
			}

			std::string ps = comm.substr(0, 4);
			if (ps == "list")
			{
				LOG_DEBUG("识别命令：" + ps, model_name);
				std::string ow;
				try
				{
					ow = comm.substr(5);
				}
				catch (const std::exception&)
				{
					LOG_ERROR(ps + "<-[HERE]", model_name);
					goto OpenWorldWhile;
				}

				std::string pps = comm.substr(5, 5);
				if (pps == "world")
				{
					LOG_DEBUG("识别命令：" + pps, model_name);

					for (int i = 0; i < world_name_list.world_name_list.size(); ++i)
					{
						LOG_INFO("\n存档名称：" + world_name_list.world_name_list[i] + "\n存档路径" + world_name_list.world_directory_list[i] + "\n", model_name);
					}
				}

				pps = comm.substr(5, 6);
				if (pps == "player")
				{
					LOG_DEBUG("识别命令：" + pps, model_name);

					for (const auto& user_info : user_info_list)
					{
						LOG_INFO("\n用户名：" + user_info.user_name + "\nUUID：" + user_info.uuid + "\n过期时间：" + user_info.expiresOn + "\n", model_name);
					}
				}
			}
		}
	}

	return 0;
}