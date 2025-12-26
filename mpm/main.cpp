//main.cpp 人口点文件
#include "CC.h"
#include "Logout.h"

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
		LOG_DEBUG("使用命令行参数作为初始路径输入");
		StartWithArgv = true;
		mp.SetInputPath(argv[1]);
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
			for (int i = 0; i < mp.GetWorldList().world_name_list.size(); i++)
			{
				out += "\n存档：" + mp.GetWorldList().world_name_list[i] + "\n路径：" + mp.GetWorldList().world_directory_list[i];
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
				out += "\n用户名：" + a.user_name + "\nUUID：" + a.uuid + "\n过期时间：" + a.expiresOn;
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
			switch (mp.ProcessCommand(comm_))//命令处理
			{
			case COMMAND_EXIT:
			{
				mRun = false;
				break;
			}

			case COMMAND_BREAK:
			{
				break;
			}

			case COMMAND_OPEN_PLAYER:
			{
				COP cop;
				try
				{
					cop >> mp;
					cop.RunCommand();
				}
				catch (const std::exception& e)
				{
					LOG_ERROR(e.what());
				}
				break;
			}

			case COMMAND_OPEN_WORLD:
			{
				COW cow;
				try
				{
					cow >> mp;
					cow.RunCommand();
				}
				catch (const std::exception& e)
				{
					LOG_ERROR(e.what());
				}
				break;
			}

			case COMMAND_LIST_PLAYER:
			{
				CLP clp;
				try
				{
					clp >> mp;
					clp.RunCommand();
				}
				catch (const std::exception& e)
				{
					LOG_ERROR(e.what());
				}
				break;
			}

			case COMMAND_LIST_WORLD:
			{
				CLW clw;
				try
				{
					clw >> mp;
					clw.RunCommand();
				}
				catch (const std::exception& e)
				{
					LOG_ERROR(e.what());
				}
				break;
			}

			case COMMAND_DEL_PLAYER:
			{
				CDP cdp;
				try
				{
					cdp >> mp;
					cdp.RunCommand();
				}
				catch (const std::exception& e)
				{
					LOG_ERROR(e.what());
				}
				break;
			}

			case COMMAND_DEL_WORLD:
			{
				CDW cdw;
				try
				{
					cdw >> mp;
					cdw.RunCommand();
				}
				catch (const std::exception& e)
				{
					LOG_ERROR(e.what());
				}
				break;
			}

			case COMMAND_DEL_PW:
			{
				CDPW cdpw;
				try
				{
					cdpw >> mp;
					cdpw.RunCommand();
				}
				catch (const std::exception& e)
				{
					LOG_ERROR(e.what());
				}
				break;
			}

			case COMMAND_DEL_JS:
			{
				CDJS cdjs;
				try
				{
					cdjs >> mp;
					cdjs.RunCommand();
				}
				catch (const std::exception& e)
				{
					LOG_ERROR(e.what());
				}
				break;
			}

			case COMMAND_NULL_BACK:
				break;

			default:
				break;
			}
		}
	}

	return 0;
}