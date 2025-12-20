//main.cpp 入口点文件

#include "func.h"//功能
#include "Logout.h"//日志输出

int main(int argc, char* argv[])
{
#if _DEBUG//如果生成模式为debug则开启log_debug输出
	LOG_DEBUG_OUT
#endif
		;//保持正常缩进
	LOG_CREATE_MODEL_NAME(model_name, "Main");//设置logout模块名称

	bool StartwithArgv = false;
	std::string input_path;

	if (argc > 1)//如果有参启动，将StartwithArgv设为true，并提取输入参数
	{
		LOG_DEBUG("使用命令行参数作为初始路径输入。", model_name);
		StartwithArgv = true;
		input_path = argv[1];
	}

	std::string world_path = {};
	bool mRun = true;

	while (mRun)//开始主循环
	{
	MainWhile:
		if (StartwithArgv == true)//检查启动参数，如果没有就要求输入，反之将StartwithArgv标记为false录入路径
		{
			StartwithArgv = false;
			world_path = input_path;
		}
		else
		{
			std::cout << "打开文件夹：";
			std::getline(std::cin, world_path);
		}

		std::string pip;//预处理路径
		try
		{
			pip = ProcessingInputPath(world_path);
		}
		catch (const std::exception& e)
		{
			LOG_ERROR(e.what(), model_name);
			goto MainWhile;
		}

		WorldDirectoriesNameList world_name_list;//声明世界列表，检测路径类型并执行对应函数获取

		if (folderExists(pip, "saves") == false)
		{
			LOG_INFO("打开(服务端)" + getLastComponent(pip), model_name);
			world_name_list = GetWorldDirectoriesList(pip, MOD_SERVER);
		}
		else
		{
			LOG_INFO("打开(客户端)" + getLastComponent(pip), model_name);
			world_name_list = GetWorldDirectoriesList(pip, MOD_CLIENT);
		}


		std::vector<UserInfo> user_info_list;//创建用户列表

		for (int i = 0; i < world_name_list.world_name_list.size(); ++i)
		{
			LOG_INFO("\n存档名称：" + world_name_list.world_name_list[i] + "\n存档路径" + world_name_list.world_directory_list[i] + "\n", model_name);
		}

		try
		{
			user_info_list = GetUserInfo(pip);//通过uesrceach.json获取用户信息
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

		while (true)//进入命令模式
		{
		OpenWorldWhile:
			std::string comm;
			std::getline(std::cin, comm);//获取命令
			LOG_CREATE_MODEL_NAME(model_name, "CommandProcessing");

			if (comm == "exit")
			{
				LOG_DEBUG("识别命令：" + comm, model_name);//关闭循环并退出
				mRun = false;
				break;
			}

			if (comm == "break")
			{
				LOG_DEBUG("识别命令：" + comm, model_name);//退出循环
				break;
			}

			std::string pc = comm.substr(0, 4);//裁出命令
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
					LOG_ERROR(comm + "<-[HERE]", model_name);//有报错则认为命令错误
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
						LOG_ERROR(comm + "<-[HERE]", model_name);
						goto OpenWorldWhile;
					}

					LOG_DEBUG("打开存档：" + ow, model_name);
					for (int i = 0; i < world_name_list.world_name_list.size(); ++i)
					{
						if (ow == world_name_list.world_name_list[i])//从存档列表中提取正确的存档
						{
							std::string open_path = world_name_list.world_directory_list[i];
							LOG_INFO("正在打开存档：" + open_path, model_name);

							std::vector<PlayerInfo_AS> sc_advancements_list;
							std::vector<PlayerInfo_Data> sc_playerdata_list;
							std::vector<PlayerInfo_AS> sc_stats_list;

							try
							{
								sc_advancements_list = GetWorldPlayerAdvancements(open_path);//提取出文件路径加入容器
								sc_playerdata_list = GetWorldPlayerData(open_path);
								sc_stats_list = GetWorldPlayerStats(open_path);
							}
							catch (const std::exception& e)
							{
								LOG_ERROR(e.what(), model_name);
							}

							LOG_INFO("存档打开完成！", model_name);

							std::string out = "\n世界：" + world_name_list.world_name_list[i] + "\n路径：" + open_path + "\n";

							for (int i = 0; i < user_info_list.size(); i++)//根据存档中advanvement的uuid筛出玩家，并认为该存档中有这些玩家
							{
								std::string adv_path = {}, pd_path = {}, pd_old_path = {}, cosarmor_path = {}, st_path = {};
								for (int j = 0; j < sc_advancements_list.size(); j++)
								{
									if (user_info_list[i].uuid == sc_advancements_list[j].uuid)
									{
										adv_path = sc_advancements_list[j].path;
									}
								}

								for (int j = 0; j < sc_playerdata_list.size(); j++)
								{
									if (user_info_list[i].uuid == sc_playerdata_list[j].uuid)
									{
										pd_path = sc_playerdata_list[j].dat_path;
										pd_old_path = sc_playerdata_list[j].dat_old_path;
										cosarmor_path = sc_playerdata_list[j].cosarmor_path;
									}
								}

								for (int j = 0; j < sc_stats_list.size(); j++)
								{
									if (user_info_list[i].uuid == sc_stats_list[j].uuid)
									{
										st_path = sc_stats_list[j].path;
									}
								}

								if (adv_path.length() != 0 || pd_path.length() != 0 || st_path.length() != 0)
								{
									out = "\n玩家 " + user_info_list[i].user_name + "\nUUID：" + user_info_list[i].uuid + "\n成就：" + adv_path + "\n玩家数据：" + pd_path;
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
								else
								{
									out += "\n无数据";
									LOG_INFO(out + "\n", model_name);
									goto OpenWorldWhile;
								}
							}
						}
					}

				}

				ppc = comm.substr(5, 6);
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
						LOG_ERROR(comm + "<-[HERE]", model_name);
						goto OpenWorldWhile;
					}

					std::vector<PlayerInfo_AS> c_advancements_list;
					std::vector<PlayerInfo_Data> c_playerdata_list;
					std::vector<PlayerInfo_AS> c_stats_list;
					std::vector<playerinworldinfo> piw_list;

					for (const auto& user_info : user_info_list)
					{
						if (ow == user_info.user_name)//根据玩家uuid筛选所有世界中的advanvement的uuid,并认为该玩家存在于世界中
						{
							for (int i = 0; i < world_name_list.world_name_list.size(); ++i)//收集该玩家在本次循环世界中所有数据信息，并记录进playerinworldinfo piw_list
							{
								playerinworldinfo piw = { "否", "否", "否", "否", "否", "否", "否" };
								piw.uuid = user_info.uuid;
								piw.world_name = world_name_list.world_name_list[i];
								std::string open_path = world_name_list.world_directory_list[i];
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

								for (int j = 0; j < c_advancements_list.size(); j++)
								{
									if (piw.uuid == c_advancements_list[j].uuid)
									{
										if (c_advancements_list[j].path.length() != 0)
										{
											piw.adv_path = "有";
										}
									}
								}

								for (int j = 0; j < c_playerdata_list.size(); j++)
								{
									if (piw.uuid == c_playerdata_list[j].uuid)
									{
										if (c_playerdata_list[j].dat_path.length() != 0)
										{
											piw.pd_path = "有";
										}
										if (c_playerdata_list[j].dat_old_path.length() != 0)
										{
											piw.pd_old_path = "有";
										}
										if (c_playerdata_list[j].cosarmor_path.length() != 0)
										{
											piw.cosarmor_path = "有";
										}
									}
								}

								for (int j = 0; j < c_stats_list.size(); j++)
								{
									if (piw.uuid == c_stats_list[j].uuid)
									{
										if (c_stats_list[j].path.length() != 0)
										{
											piw.st_path = "有";
										}
									}
								}

								piw_list.push_back(piw);
							}
						}
					}

					if (piw_list.size() == 0)
					{
						LOG_WARNING("未找到该玩家!", model_name);
						goto OpenWorldWhile;
					}


					std::string show_str = "\n玩家：" + ow + "\nUUID：" + piw_list[0].uuid + "\n世界：\n";

					for (const auto& show : piw_list)//只展示路径是否存在
					{
						if (show.adv_path == "有" || show.pd_path == "有" || show.pd_old_path == "有" || show.cosarmor_path == "有" || show.st_path == "有")
						{
							show_str += show.world_name + "|进度：" + show.adv_path + "|数据：" + show.pd_path + "|旧数据：" + show.pd_old_path + "|其他数据：" + show.cosarmor_path + "|统计：" + show.st_path + "\n";
						}
					}

					LOG_INFO(show_str + "\n", model_name);
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
					std::vector<PlayerInfo_AS> is_world_player;
					std::vector<UserInfo> is_world_user;

					for (int i = 0; i < world_name_list.world_name_list.size(); ++i)//用Advancements的uuid来证明玩家在此世界存在
					{
						is_world_player = GetWorldPlayerAdvancements(world_name_list.world_directory_list[i]);
						is_world_user = GetUserInfo(pip);

						std::string out = "\n存档：" + world_name_list.world_name_list[i] + "\n路径：" + world_name_list.world_directory_list[i];

						for (const auto& wpl : is_world_player)//获取单个世界玩家列表
						{
							for (const auto& uwl : is_world_user)//获取全部用户信息
							{
								if (wpl.uuid == uwl.uuid)
								{
									out += "\n玩家：" + uwl.user_name + " | UUID：" + uwl.uuid;
								}
							}
						}

						LOG_INFO(out + "\n", model_name);
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

			std::string hp = comm.substr(0, 6);
			if (hp == "delete")
			{
				LOG_DEBUG("识别命令：" + hp, model_name);

				std::string os;
				try
				{
					os = comm.substr(7);
				}
				catch (const std::exception&)
				{
					LOG_ERROR(comm + "<-[HERE]", model_name);
					goto OpenWorldWhile;
				}

				std::string oss = comm.substr(7, 6);
				if (oss == "player")
				{
					LOG_DEBUG("识别命令：" + oss, model_name);

					std::string osss;
					try
					{
						osss = comm.substr(14);
					}
					catch (const std::exception&)
					{
						LOG_ERROR(comm + "<-[HERE]", model_name);
						goto OpenWorldWhile;
					}

					std::vector<UserInfo> player_uuid;

					try
					{
						player_uuid = GetUserInfo(pip);
					}
					catch (const std::exception& e)
					{
						LOG_ERROR(e.what(), model_name);
						break;
					}

					std::string finded_player_uuid = {};

					for (const UserInfo& findlist : player_uuid)//查找玩家uuid
					{
						if (osss == findlist.user_name)
						{
							finded_player_uuid = findlist.uuid;
						}
					}

					if (finded_player_uuid.empty())
					{
						LOG_INFO("找不到此玩家", model_name);
						goto OpenWorldWhile;
					}

					LOG_INFO("删除" + osss, model_name);

					std::vector<PlayerInfo_AS> d_advancements_list;
					std::vector<PlayerInfo_Data> d_playerdata_list;
					std::vector<PlayerInfo_AS> d_stats_list;
					playerinworldinfo delete_file_list;

					std::string del_info = "\n从所有世界删除玩家：" + osss + "\nUUID：" + finded_player_uuid;

					for (int i = 0; i < world_name_list.world_directory_list.size(); i++)
					{
						try
						{
							d_advancements_list = GetWorldPlayerAdvancements(world_name_list.world_directory_list[i]);
							d_playerdata_list = GetWorldPlayerData(world_name_list.world_directory_list[i]);
							d_stats_list = GetWorldPlayerStats(world_name_list.world_directory_list[i]);
						}
						catch (const std::exception& e)
						{
							LOG_ERROR(e.what(), model_name);
							goto OpenWorldWhile;
						}

						for (int j = 0; j < d_advancements_list.size(); j++)//遍历列表，匹配uuid
						{
							if (finded_player_uuid == d_advancements_list[j].uuid)
							{
								if (d_advancements_list[j].path.length() != 0)
								{
									delete_file_list.adv_path = d_advancements_list[j].path;
								}
							}
						}

						for (int j = 0; j < d_playerdata_list.size(); j++)
						{
							if (finded_player_uuid == d_playerdata_list[j].uuid)
							{
								if (d_playerdata_list[j].dat_path.length() != 0)
								{
									delete_file_list.pd_path = d_playerdata_list[j].dat_path;
								}
								if (d_playerdata_list[j].dat_old_path.length() != 0)
								{
									delete_file_list.pd_old_path = d_playerdata_list[j].dat_old_path;
								}
								if (d_playerdata_list[j].cosarmor_path.length() != 0)
								{
									delete_file_list.cosarmor_path = d_playerdata_list[j].cosarmor_path;
								}
							}
						}

						for (int j = 0; j < d_stats_list.size(); j++)
						{
							if (finded_player_uuid == d_stats_list[j].uuid)
							{
								if (d_stats_list[j].path.length() != 0)
								{
									delete_file_list.st_path = d_stats_list[j].path;
								}
							}
						}

						del_info += +"\n世界：" + world_name_list.world_name_list[i] + "\n";
						bool is_see;
						is_see = MoveToRecycleBinWithPS(delete_file_list.adv_path);
						if (is_see == true)
						{
							del_info += "删除：" + delete_file_list.adv_path + "\n";
						}
						else
						{
							del_info += "失败：文件已删除或不存在\n";
						}

						is_see = MoveToRecycleBinWithPS(delete_file_list.cosarmor_path);
						if (is_see == true)
						{
							del_info += "删除：" + delete_file_list.cosarmor_path + "\n";
						}
						else
						{
							del_info += "失败：文件已删除或不存在\n";
						}

						is_see = MoveToRecycleBinWithPS(delete_file_list.pd_old_path);
						if (is_see == true)
						{
							del_info += "删除：" + delete_file_list.pd_old_path + "\n";
						}
						else
						{
							del_info += "失败：文件已删除或不存在\n";
						}

						is_see = MoveToRecycleBinWithPS(delete_file_list.pd_path);
						if (is_see == true)
						{
							del_info += "删除：" + delete_file_list.pd_path + "\n";
						}
						else
						{
							del_info += "失败：文件已删除或不存在\n";
						}

						is_see = MoveToRecycleBinWithPS(delete_file_list.st_path);
						if (is_see == true)
						{
							del_info += "删除：" + delete_file_list.st_path + "\n";
						}
						else
						{
							del_info += "失败：文件已删除或不存在\n";
						}
					}

					LOG_INFO(del_info, model_name);
				}

				oss = comm.substr(7, 5);
				if (oss == "world")
				{
					LOG_DEBUG("识别命令：" + oss, model_name);

					std::string osss;
					try
					{
						osss = comm.substr(13);
					}
					catch (const std::exception&)
					{
						LOG_ERROR(comm + "<-[HERE]", model_name);
						goto OpenWorldWhile;
					}

					LOG_INFO("删除：" + osss, model_name);

					std::vector<PlayerInfo_AS> d_advancements_list;
					std::vector<PlayerInfo_Data> d_playerdata_list;
					std::vector<PlayerInfo_AS> d_stats_list;
					playerinworldinfo delete_file_list;
					std::string del_info;

					for (int i = 0; i < world_name_list.world_name_list.size(); ++i)
					{
						if (osss == world_name_list.world_name_list[i])
						{
							try
							{
								d_advancements_list = GetWorldPlayerAdvancements(world_name_list.world_directory_list[i]);
								d_playerdata_list = GetWorldPlayerData(world_name_list.world_directory_list[i]);
								d_stats_list = GetWorldPlayerStats(world_name_list.world_directory_list[i]);
							}
							catch (const std::exception& e)
							{
								LOG_ERROR(e.what(), model_name);
								goto OpenWorldWhile;
							}

							del_info += "\n世界：" + world_name_list.world_name_list[i] + "\n路径：" + world_name_list.world_directory_list[i] + "\n";
						}
					}

					if (d_advancements_list.size() == 0 && d_playerdata_list.size() == 0 && d_stats_list.size() == 0)
					{
						del_info += "无数据\n";
						LOG_INFO(del_info, model_name);
						goto OpenWorldWhile;
					}

					size_t maxnum;//遍历容器没有遍历计数，所以创一个
					if (d_advancements_list.size() > d_playerdata_list.size())
					{
						if (d_advancements_list.size() > d_stats_list.size())
						{
							maxnum = d_advancements_list.size();
						}
						else
						{
							maxnum = d_stats_list.size();
						}
					}
					else
					{
						if (d_playerdata_list.size() > d_stats_list.size())
						{
							maxnum = d_playerdata_list.size();
						}
						else
						{
							maxnum = d_stats_list.size();
						}
					}

					for (int i = 0; i <= maxnum; i++)//选取三列表里元素最多的做循环计数
					{
						for (const UserInfo& playeruuid : user_info_list)//遍历所有玩家，并匹配该世界内所有的玩家文件，便于展示
						{
							delete_file_list.uuid = playeruuid.uuid;
							delete_file_list.world_name = playeruuid.user_name;

							if (d_advancements_list.size() != 0)
							{
								size_t a_num = 0;
								for (const PlayerInfo_AS& da : d_advancements_list)
								{
									if (playeruuid.uuid == d_advancements_list[a_num].uuid)
									{
										if (d_advancements_list[a_num].path.length() != 0)
										{
											delete_file_list.adv_path = d_advancements_list[a_num].path;
										}
										break;
									}
									a_num++;
								}
							}

							if (d_playerdata_list.size() != 0)
							{
								size_t b_num = 0;
								for (const PlayerInfo_Data& da : d_playerdata_list)
								{
									if (playeruuid.uuid == d_playerdata_list[b_num].uuid)
									{
										if (d_playerdata_list[b_num].dat_path.length() != 0)
										{
											delete_file_list.pd_path = d_playerdata_list[b_num].dat_path;
										}
										if (d_playerdata_list[b_num].dat_old_path.length() != 0)
										{
											delete_file_list.pd_old_path = d_playerdata_list[b_num].dat_old_path;
										}
										if (d_playerdata_list[b_num].cosarmor_path.length() != 0)
										{
											delete_file_list.cosarmor_path = d_playerdata_list[b_num].cosarmor_path;
										}
										break;
									}
									b_num++;
								}
							}

							if (d_stats_list.size() != 0)
							{
								size_t c_num = 0;
								for (const PlayerInfo_AS& da : d_stats_list)
								{
									if (playeruuid.uuid == d_stats_list[c_num].uuid)
									{
										if (d_stats_list[c_num].path.length() != 0)
										{
											delete_file_list.st_path = d_stats_list[c_num].path;
										}
										break;
									}
									c_num++;
								}
							}
							//确认结构体已经填满
							if (delete_file_list.adv_path.length() != 0 && delete_file_list.cosarmor_path.length() != 0 && delete_file_list.pd_old_path.length() != 0 && delete_file_list.pd_path.length() != 0 && delete_file_list.st_path.length() != 0)
							{
								del_info += "\n玩家：" + delete_file_list.world_name + " | UUID：" + delete_file_list.uuid + "\n";
								bool is_see;
								is_see = MoveToRecycleBinWithPS(delete_file_list.adv_path);
								if (is_see == true)
								{
									del_info += "删除：" + delete_file_list.adv_path + "\n";
								}
								else
								{
									del_info += "失败：文件已删除或不存在\n";
								}

								is_see = MoveToRecycleBinWithPS(delete_file_list.cosarmor_path);
								if (is_see == true)
								{
									del_info += "删除：" + delete_file_list.cosarmor_path + "\n";
								}
								else
								{
									del_info += "失败：文件已删除或不存在\n";
								}

								is_see = MoveToRecycleBinWithPS(delete_file_list.pd_old_path);
								if (is_see == true)
								{
									del_info += "删除：" + delete_file_list.pd_old_path + "\n";
								}
								else
								{
									del_info += "失败：文件已删除或不存在\n";
								}

								is_see = MoveToRecycleBinWithPS(delete_file_list.pd_path);
								if (is_see == true)
								{
									del_info += "删除：" + delete_file_list.pd_path + "\n";
								}
								else
								{
									del_info += "失败：文件已删除或不存在\n";
								}

								is_see = MoveToRecycleBinWithPS(delete_file_list.st_path);
								if (is_see == true)
								{
									del_info += "删除：" + delete_file_list.st_path + "\n";
								}
								else
								{
									del_info += "失败：文件已删除或不存在\n";
								}

								for (int x = 0; x <= maxnum; x++)
								{
									if (delete_file_list.uuid == d_advancements_list[x].uuid)
									{
										d_advancements_list.erase(d_advancements_list.begin() + x);
										break;
									}
								}
								for (int x = 0; x <= maxnum; x++)
								{
									if (delete_file_list.uuid == d_playerdata_list[x].uuid)
									{
										d_playerdata_list.erase(d_playerdata_list.begin() + x);
										break;
									}
								}

								for (int x = 0; x <= maxnum; x++)
								{
									if (delete_file_list.uuid == d_stats_list[x].uuid)
									{
										d_stats_list.erase(d_stats_list.begin() + x);
										break;
									}
								}

								//清空结构体
								delete_file_list = { "", "", "", "", "", "", "" };
							}
						}
					}

					LOG_INFO(del_info, model_name);
				}

				oss = comm.substr(7, 2);
				if (oss == "pw")
				{
					LOG_DEBUG("识别命令：" + oss, model_name);

					std::string ossss;
					try
					{
						ossss = comm.substr(9);
					}
					catch (const std::exception&)
					{
						LOG_ERROR(comm + "<-[HERE]", model_name);
						goto OpenWorldWhile;
					}

					std::vector<std::string> p_w = splitString(ossss, ' ');
					if (p_w[0].empty() && p_w[1].empty())
					{
						LOG_ERROR(comm + "<-[HERE]", model_name);
						goto OpenWorldWhile;
					}

					UserInfo player;
					WorldDirectoriesName world;
					player.user_name = p_w[1];
					world.world_name = p_w[2];

					LOG_INFO("\n从 " + world.world_name + " 中删除 " + player.user_name + " \n", model_name);

					for (const UserInfo& puuid : user_info_list)//根据用户名查出uuid
					{
						if (player.user_name == puuid.user_name)
						{
							player.uuid = puuid.uuid;
						}
					}

					for (int i = 0; i < world_name_list.world_name_list.size(); i++)//查到存档路径;
					{
						if (world.world_name == world_name_list.world_name_list[i])
						{
							world.world_directory = world_name_list.world_directory_list[i];
						}
					}

					std::vector<PlayerInfo_AS> pw_ad_list;
					std::vector<PlayerInfo_AS> pw_st_list;
					std::vector<PlayerInfo_Data> pw_da_list;
					playerinworldinfo piwi;
					piwi.world_name = world.world_name;
					piwi.uuid = player.uuid;

					try
					{
						pw_ad_list = GetWorldPlayerAdvancements(world.world_directory);
						pw_da_list = GetWorldPlayerData(world.world_directory);
						pw_st_list = GetWorldPlayerStats(world.world_directory);
					}
					catch (const std::exception& e)
					{
						LOG_ERROR(e.what(), model_name);
						goto OpenWorldWhile;
					}

					for (const PlayerInfo_AS& un : pw_ad_list)//匹配Advancements
					{
						if (player.uuid == un.uuid)
						{
							if (!un.path.empty())
							{
								piwi.adv_path = un.path;
							}

						}
					}

					for (const PlayerInfo_Data& un : pw_da_list)//匹配PlayerData
					{
						if (player.uuid == un.uuid)
						{
							if (!un.dat_path.empty())
							{
								piwi.pd_path = un.dat_path;
							}
						}

						if (player.uuid == un.old_uuid)
						{
							if (!un.dat_old_path.empty())
							{
								piwi.pd_old_path = un.dat_old_path;
							}
						}

						if (player.uuid == un.cosarmor_uuid)
						{
							if (!un.cosarmor_path.empty())
							{
								piwi.cosarmor_path = un.cosarmor_path;
							}
						}
					}

					for (const PlayerInfo_AS& un : pw_st_list)//匹配Stats
					{
						if (player.uuid == un.uuid)
						{
							if (!un.path.empty())
							{
								piwi.st_path = un.path;
							}

						}
					}

					std::string del_info = "\n玩家：" + player.user_name + "|UUID：" + player.uuid + "\n世界：" + world.world_name + "|路径：" + world.world_directory + "\n";

					if (piwi.adv_path.length() != 0 && piwi.cosarmor_path.length() != 0 && piwi.pd_old_path.length() != 0 && piwi.pd_path.length() != 0 && piwi.st_path.length() != 0)
					{
						bool is_see;
						is_see = MoveToRecycleBinWithPS(piwi.adv_path);
						if (is_see == true)
						{
							del_info += "删除：" + piwi.adv_path + "\n";
						}
						else
						{
							del_info += "失败：文件已删除或不存在\n";
						}

						is_see = MoveToRecycleBinWithPS(piwi.cosarmor_path);
						if (is_see == true)
						{
							del_info += "删除：" + piwi.cosarmor_path + "\n";
						}
						else
						{
							del_info += "失败：文件已删除或不存在\n";
						}

						is_see = MoveToRecycleBinWithPS(piwi.pd_old_path);
						if (is_see == true)
						{
							del_info += "删除：" + piwi.pd_old_path + "\n";
						}
						else
						{
							del_info += "失败：文件已删除或不存在\n";
						}

						is_see = MoveToRecycleBinWithPS(piwi.pd_path);
						if (is_see == true)
						{
							del_info += "删除：" + piwi.pd_path + "\n";
						}
						else
						{
							del_info += "失败：文件已删除或不存在\n";
						}

						is_see = MoveToRecycleBinWithPS(piwi.st_path);
						if (is_see == true)
						{
							del_info += "删除：" + piwi.st_path + "\n";
						}
						else
						{
							del_info += "失败：文件已删除或不存在\n";
						}
					}
					else
					{
						del_info += "无数据\n";
					}

					LOG_INFO(del_info + "\n", model_name);
				}
			}
		}
	}

	return 0;
}