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
		if (StartWithArgv != true)//检查启动参数，如果没有就要求输入，反之将StartwithArgv标记为false录入路径
		{
			std::string ip;
			std::cout << "打开文件夹：";
			std::getline(std::cin, ip);
			mp.SetInputPath(ip);
		}
	}

	return 0;
}