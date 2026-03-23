// BackgroundRunning.cpp - 修改后的后台运行
#include "BackgroundRunning.h"
#include <chrono>
#include <thread>

int BgRun()
{
	LOG_CREATE_MODEL_NAME("BgRun");
	LOG_INFO("后台模式启动");

	while (true)
	{
		SharedMemory sm;

		LOG_INFO("尝试初始化共享内存连接...");

		if (sm.Init())
		{
			LOG_INFO("共享内存初始化成功，开始运行循环");
			sm.RunLoop();
		}
		else
		{
			LOG_ERROR("共享内存初始化失败，5秒后重试");
			std::this_thread::sleep_for(std::chrono::seconds(5));
		}

		sm.Clearup();
		LOG_INFO("连接已关闭，准备重新连接");

		// 短暂等待后重新尝试连接
		std::this_thread::sleep_for(std::chrono::seconds(1));
	}

	return 0;
}