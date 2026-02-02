#include "BackgroundRunning.h"

int BgRun()
{
	LOG_CREATE_MODEL_NAME("BgRun");

	while (true)
	{
		SharedMemory sm;
		if (!sm.Init())
		{
			LOG_ERROR("初始化失败");
			sm.Clearup();
		}

		sm.RunLoop();
	}

	return 0;
}
