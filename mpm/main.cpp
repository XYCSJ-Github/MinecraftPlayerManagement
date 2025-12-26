//main.cpp 人口点文件
#include "CC.h"
#include "Logout.h"

int main(int argc, char* argv[])
{
#if _DEBUG//如果生成模式为debug则开启log_debug输出
	LOG_DEBUG_OUT
#endif
		;
	LOG_CREATE_MODEL_NAME("Main");

	bool StartWithArgv = false;
	


	return 0;
}