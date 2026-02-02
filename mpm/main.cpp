//main.cpp 人口点文件
#include "CC.h"
#include "Logout.h"
#include "CommandRunning.h"
#include "BackgroundRunning.h"

int main(int argc, char* argv[])
{
#if _DEBUG//如果生成模式为debug则开启log_debug输出
	LOG_DEBUG_OUT
#endif
		;//保持正常缩进
	LOG_CREATE_MODEL_NAME("Start");//设置logout模块名称

	bool StartWithArgv = false;
	p_mpm mp;

	if (argc > 1)//如果有参启动，将StartwithArgv设为true，并提取输入参数
	{
		

		if (std::strcmp(argv[1],"bg") == 0)
		{
			return BgRun();
		}
		LOG_DEBUG("使用命令行参数作为初始路径输入");
		mp.SetInputPath(argv[1]);
		StartWithArgv = true;
	}
	
	return ComRun(StartWithArgv, mp);
}