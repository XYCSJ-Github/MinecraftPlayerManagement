#include <vector>
#include "Logout.h"
#include "func.h"

int main()
{
	LOG_DEBUG_OUT

	std::string world_path = {};
	bool mRun = true;

	while (mRun)
	{
		std::cout << "打开客户端文件夹：";
		std::getline(std::cin, world_path);

		std::string pip = ProcessingInputPath(world_path);

		WorldDirectoriesNameList world_name_list = GetWorldDirectoriesList(pip);

		for (int i = 0; i < world_name_list.world_name_list.size(); ++i)
		{
			LOG_INFO("世界名称：" + world_name_list.world_name_list[i], "Main");
		}

		GetUserInfo(pip);
	}

	system("pause");
	return 0;
}