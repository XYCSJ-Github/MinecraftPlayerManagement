//main.cpp 人口点文件
#include "CC.h"
#include "Logout.h"
#include "SharedMemoryClient.h"

p_mpm mp;//所有CommandClass的父类

int main(int argc, char* argv[])
{
#if _DEBUG//如果生成模式为debug则开启log_debug输出
	LOG_DEBUG_OUT
#endif
		;//保持正常缩进
	LOG_CREATE_MODEL_NAME("Main");//设置logout模块名称

	bool StartWithArgv = false;

	if (argc > 1)//如果有参启动，将StartwithArgv设为true，并提取输入参数
	{
		if (std::strcmp(argv[1],"bg") == 0)
		{
			SharedMemoryClient smc;
			smc.Initialize();
			smc.RunLoop();
			return 0;
		}
		LOG_DEBUG("使用命令行参数作为初始路径输入");
		mp.SetInputPath(argv[1]);
		StartWithArgv = true;
	}

	bool mRun = true;

	while (mRun)
	{
	MainWhile:
		if (StartWithArgv != true)//检查启动参数，如果没有就要求输入，反之将StartwithArgv标记为false录入路径
		{
			std::string ip;
			std::cout << "打开文件夹：";
			std::getline(std::cin, ip);
			mp.SetInputPath(ip);
		}

		try//处理路径
		{
			mp.ProcessingPath();
		}
		catch (const std::exception& e)
		{
			LOG_ERROR(e.what());
			StartWithArgv = false;
			goto MainWhile;
		}

		try//检测路径类型
		{
			mp.PathLoadTpye();
			if (mp.GetPathLoadType() == MOD_CLIENT)
			{
				LOG_INFO("打开(服务端)" + getLastComponent(mp.GetProcessingPath()));
			}
			else if (mp.GetPathLoadType() == MOD_CLIENT)
			{
				LOG_INFO("打开(客户端)" + getLastComponent(mp.GetProcessingPath()));
			}
		}
		catch (const std::exception& e)
		{
			LOG_ERROR(e.what());
		}

		try//获取世界列表
		{
			std::string out;
			mp.LoadWorldList();
			mp.LoadWorldListSTL();
			for (int i = 0; i < mp.GetWorldList().world_name_list.size(); i++)
			{
				out += "\n存档：" + mp.GetWorldList().world_name_list[i] + "\n路径：" + mp.GetWorldList().world_directory_list[i] + "\n";
			}
			LOG_INFO(out);
		}
		catch (const std::exception& e)
		{
			LOG_ERROR(e.what());
		}

		try//获取玩家列表
		{
			std::string out;
			mp.LoadUserList();
			for (const UserInfo& a : mp.GetUserInfoList())
			{
				out += "\n用户名：" + a.user_name + "\nUUID：" + a.uuid + "\n过期时间：" + a.expiresOn + "\n";
			}
			LOG_INFO(out);
		}
		catch (const std::exception& e)
		{
			LOG_ERROR(e.what());
		}

		while (true)
		{
			LOG_CREATE_MODEL_NAME("CommandProcess");

			std::string comm_;
			std::cout << ">";
			std::getline(std::cin, comm_);

			int Signal;
			try 
			{
				Signal = mp.ProcessCommand(comm_);
			}
			catch (const CommandError& e)
			{
				LOG_ERROR(e.what());
				Signal = 211;
			}

			switch (Signal)//命令处理
			{
			case COMMAND_EXIT:
			{
				LOG_DEBUG("识别命令：exit");
				return 0;
			}

			case COMMAND_BREAK:
			{
				LOG_DEBUG("识别命令：break");
				goto MainWhile;
			}

			case COMMAND_OPEN_PLAYER:
			{
				LOG_DEBUG("识别命令：open player");
				COP cop;
				try
				{
					cop >> mp;
					cop.RunCommand();
					LOG_INFO(cop.GetShow());
				}
				catch (const std::exception& e)
				{
					LOG_ERROR(e.what());
				}
				break;
			}

			case COMMAND_OPEN_WORLD:
			{
				LOG_DEBUG("识别命令：open world");
				COW cow;
				try
				{
					cow >> mp;
					cow.RunCommand();
					LOG_INFO(cow.GetShow());
				}
				catch (const std::exception& e)
				{
					LOG_ERROR(e.what());
				}
				break;
			}

			case COMMAND_LIST_PLAYER:
			{
				LOG_DEBUG("识别命令：list player");
				CLP clp;
				try
				{
					clp >> mp;
					clp.RunCommand();
					LOG_INFO(clp.GetShow());
				}
				catch (const std::exception& e)
				{
					LOG_ERROR(e.what());
				}
				break;
			}

			case COMMAND_LIST_WORLD:
			{
				LOG_DEBUG("识别命令：list world");
				CLW clw;
				try
				{
					clw >> mp;
					clw.RunCommand();
					LOG_INFO(clw.GetShow());
				}
				catch (const std::exception& e)
				{
					LOG_ERROR(e.what());
				}
				break;
			}

			case COMMAND_DEL_PLAYER:
			{
				LOG_DEBUG("识别命令：delete player");
				CDP cdp;
				try
				{
					cdp >> mp;
					cdp.RunCommand();
					LOG_INFO(cdp.GetShow());
				}
				catch (const std::exception& e)
				{
					LOG_ERROR(e.what());
				}
				break;
			}

			case COMMAND_DEL_WORLD:
			{
				LOG_DEBUG("识别命令：delete world");
				CDW cdw;
				try
				{
					cdw >> mp;
					cdw.RunCommand();
					LOG_INFO(cdw.GetShow());
				}
				catch (const std::exception& e)
				{
					LOG_ERROR(e.what());
				}
				break;
			}

			case COMMAND_DEL_PW:
			{
				LOG_DEBUG("识别命令：delete pw");
				CDPW cdpw;
				try
				{
					cdpw >> mp;
					cdpw.RunCommand();
					LOG_INFO(cdpw.GetShow());
				}
				catch (const std::exception& e)
				{
					LOG_ERROR(e.what());
				}
				break;
			}

			case COMMAND_DEL_JS:
			{
				LOG_DEBUG("识别命令：delete js");
				CDJS cdjs;
				try
				{
					cdjs >> mp;
					cdjs.RunCommand();
					LOG_INFO(cdjs.GetShow());
				}
				catch (const std::exception& e)
				{
					LOG_ERROR(e.what());
				}
				break;
			}

			case COMMAND_NULL_BACK:
				LOG_DEBUG("未识别命令");
				break;

			case COMMAND_REFRESH:
			{
				LOG_DEBUG("识别命令：refresh");
				try
				{
					mp.LoadUserList();
					mp.LoadWorldList();
					mp.GetSTLWorldList();
				}
				catch (const std::exception& e)
				{
					LOG_ERROR(e.what());
				}
				break;
			}

			default:
				break;
			}
		}
	}

	return 0;
}