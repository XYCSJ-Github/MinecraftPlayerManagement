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

		for (int i = 0; i < world_name_list.world_name_list.size(); ++i)
		{
			LOG_INFO("世界名称：" + world_name_list.world_name_list[i], model_name);
		}

		try
		{
			std::vector<UserInfo> user_info_list = GetUserInfo(pip);
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
	}

	system("pause");
	return 0;
}