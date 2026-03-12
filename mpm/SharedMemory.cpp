// SharedMemory.cpp - 修改后的实现
#include "SharedMemory.h"
#include <sstream>
#include <string>

bool SharedMemory::OpenSyncObjects()
{
	LOG_CREATE_MODEL_NAME("OpenSyncObjects");
	LOG_INFO("开始打开同步对象");

	int maxRetries = 50;  // 最大重试次数
	DWORD retryInterval = 100;  // 重试间隔 100ms

	for (int i = 0; i < maxRetries; i++)
	{
		// 打开互斥锁
		handles.hMutex = OpenMutex(MUTEX_ALL_ACCESS, FALSE, MUTEX_NAME);

		// 打开事件
		handles.hEventSend = OpenEvent(EVENT_ALL_ACCESS, FALSE, EVENT_SEND);
		handles.hEventRecv = OpenEvent(EVENT_ALL_ACCESS, FALSE, EVENT_RECV);
		handles.hInitEvent = OpenEvent(EVENT_ALL_ACCESS, FALSE, EVENT_INIT);

		// 检查是否全部成功打开
		if (handles.hMutex != NULL &&
			handles.hEventSend != NULL &&
			handles.hEventRecv != NULL &&
			handles.hInitEvent != NULL)
		{
			LOG_INFO("成功打开所有同步对象");
			return true;
		}

		// 清理已打开的对象
		if (handles.hMutex != NULL) CloseHandle(handles.hMutex);
		if (handles.hEventSend != NULL) CloseHandle(handles.hEventSend);
		if (handles.hEventRecv != NULL) CloseHandle(handles.hEventRecv);
		if (handles.hInitEvent != NULL) CloseHandle(handles.hInitEvent);

		handles.hMutex = handles.hEventSend = handles.hEventRecv = handles.hInitEvent = NULL;

		LOG_DEBUG("第" + std::to_string(i + 1) + "次打开同步对象失败，等待重试");
		Sleep(retryInterval);
	}

	LOG_ERROR("无法打开同步对象");
	return false;
}

bool SharedMemory::ConnectMemory()
{
	LOG_CREATE_MODEL_NAME("ConnectMemory");
	LOG_INFO("开始连接共享内存");

	int maxRetries = 50;
	DWORD retryInterval = 100;

	for (int i = 0; i < maxRetries; i++)
	{
		// 打开共享内存
		handles.hMapFile = OpenFileMapping(FILE_MAP_ALL_ACCESS, FALSE, MEMORY_NAME);
		if (handles.hMapFile != NULL)
		{
			// 映射视图
			smc = (SharedMemoryCommand*)MapViewOfFile(
				handles.hMapFile,
				FILE_MAP_ALL_ACCESS,
				0,
				0,
				sizeof(SharedMemoryCommand)
			);

			if (smc != NULL)
			{
				LOG_INFO("成功连接共享内存");
				connect_status = ConnectStatus::CONNECTED;
				return true;
			}
			else
			{
				DWORD error = GetLastError();
				LOG_ERROR("映射共享内存失败，错误码: " + std::to_string(error));
				CloseHandle(handles.hMapFile);
				handles.hMapFile = NULL;
			}
		}

		LOG_DEBUG("第" + std::to_string(i + 1) + "次连接共享内存失败，等待重试");
		Sleep(retryInterval);
	}

	LOG_ERROR("无法连接共享内存");
	connect_status = ConnectStatus::NOT_CONNECTED;
	return false;
}

bool SharedMemory::WaitForCSharpReady()
{
	LOG_CREATE_MODEL_NAME("WaitForCSharp");
	LOG_INFO("等待C#端就绪...");

	// 先通知C#端C++已就绪
	SetInitEvent();  // 先设置事件通知C#

	// 然后等待C#端的响应（或者等待一小段时间）
	DWORD waitResult = WaitForSingleObject(handles.hInitEvent, 5000); // 5秒超时

	if (waitResult == WAIT_OBJECT_0)
	{
		LOG_INFO("收到C#端确认");

		// 重置初始化事件，准备下一次使用
		ResetEvent(handles.hInitEvent);

		connect_status = ConnectStatus::CONNECTED;
		return true;
	}
	else if (waitResult == WAIT_TIMEOUT)
	{
		LOG_WARNING("等待C#端确认超时，但继续执行");
		// 即使超时也继续，因为C#可能已经收到了我们的就绪信号
		connect_status = ConnectStatus::CONNECTED;
		return true;
	}
	else
	{
		DWORD error = GetLastError();
		LOG_ERROR("等待失败，错误码: " + std::to_string(error));
		return false;
	}
}

void SharedMemory::SetInitEvent()
{
	LOG_CREATE_MODEL_NAME("InitEvent");
	if (handles.hInitEvent != NULL)
	{
		// 设置初始化事件，通知C#端C++已就绪
		SetEvent(handles.hInitEvent);
		LOG_INFO("已通知C#端C++就绪");
	}
}

void SharedMemory::ProcessCommand()
{
	LOG_CREATE_MODEL_NAME("ProcessCommand");

	if (smc == NULL) return;

	Command cmd = static_cast<Command>(smc->DefCommand);
	std::string additional = smc->AdditionaCommand;

	LOG_INFO("收到命令: " + std::to_string(static_cast<int>(cmd)) + ", 附加参数: " + additional);

	WriteInSMC(smc);

	// 根据命令类型处理
	switch (cmd)
	{
	case Command::EMPTY_COMMAND:
		LOG_INFO("空命令");
		WriteInSMC(smc);
		break;

	case Command::EXIT:
		LOG_INFO("收到退出命令");
		WriteInSMC(smc);
		exit(0);

	case Command::M_SET_PATH:
		LOG_INFO("设置路径: " + additional);
		// 处理设置路径逻辑

		if (!fs::exists(smc->AdditionaCommand))
		{
			WriteInSMC(smc, (StructType)0, RunStatus::FAILED, "路径错误，无法识别");
		}

		try
		{
			mp.SetInputPath(smc->AdditionaCommand);
			mp.ProcessingPath();
			mp.PathLoadTpye();
			mp.LoadWorldList();
			mp.LoadUserList();
		}
		catch (const std::exception&)
		{
			WriteInSMC(smc, (StructType)0, RunStatus::FAILED, "在输入路径完成后的处理过程中出错");
			break;
		}


		WriteInSMC(smc);
		if (mp.GetPathLoadType() != LoadMode::EMPTY)
		{
			smc->LoadMode = mp.GetPathLoadType();
		}
		break;

	case Command::LIST_WORLD:
		LOG_INFO("列出存档");
		// 处理列出存档逻辑
		WriteInSMC(smc, StructType::WDNL);
		break;

	case Command::LIST_PLAYER:
		LOG_INFO("列出玩家");
		// 处理列出玩家逻辑
		WriteInSMC(smc, StructType::PIWIL);
		break;

	default:
		LOG_WARNING("未知命令: " + std::to_string(static_cast<int>(cmd)));
		WriteInSMC(smc, StructType::EMPTY_STRUCT, RunStatus::FAILED, "未知命令");
		break;
	}
}

void SharedMemory::RunLoop()
{
	LOG_CREATE_MODEL_NAME("RunLoop");

	if (connect_status != ConnectStatus::CONNECTED)
	{
		LOG_ERROR("未连接到共享内存");
		return;
	}

	LOG_INFO("进入主循环，等待命令...");

	while (connect_status == ConnectStatus::CONNECTED)
	{
		// 等待发送事件（C#端发来命令）
		DWORD waitResult = WaitForSingleObject(handles.hEventSend, 100); // 100ms超时，避免无法退出

		if (waitResult == WAIT_OBJECT_0)
		{
			LOG_DEBUG("收到发送事件信号");

			// 重置发送事件
			ResetEvent(handles.hEventSend);

			// 获取互斥锁
			DWORD mutexWait = WaitForSingleObject(handles.hMutex, 5000);
			if (mutexWait == WAIT_OBJECT_0)
			{
				try
				{
					// 检查是否是C#写入的
					if (smc->Writer == WriteStatus::WHITEWITHCS)
					{
						LOG_DEBUG("处理C#命令");

						// 处理命令
						ProcessCommand();

						// 标记为C++写入
						smc->Writer = WriteStatus::WHITEWITHCPP;

						// 触发接收事件，通知C#端有回复
						SetEvent(handles.hEventRecv);
					}
				}
				catch (const std::exception& e)
				{
					LOG_ERROR("处理命令异常: " + std::string(e.what()));
					smc->RunStatus = RunStatus::FAILED;
					//smc->ErrorInfo = e.what();
				}

				// 释放互斥锁
				ReleaseMutex(handles.hMutex);
			}
			else
			{
				LOG_ERROR("获取互斥锁超时");
			}
		}
		else if (waitResult == WAIT_TIMEOUT)
		{
			// 正常超时，继续循环
			continue;
		}
		else
		{
			DWORD error = GetLastError();
			LOG_ERROR("等待事件失败，错误码: " + std::to_string(error));
			break;
		}
	}

	LOG_INFO("退出主循环");
}

void SharedMemory::Clearup()
{
	LOG_CREATE_MODEL_NAME("Clearup");
	LOG_INFO("开始清理资源");

	// 取消映射
	if (smc != NULL)
	{
		UnmapViewOfFile(smc);
		smc = NULL;
	}

	// 关闭句柄
	if (handles.hMapFile != NULL)
	{
		CloseHandle(handles.hMapFile);
		handles.hMapFile = NULL;
	}

	if (handles.hMutex != NULL)
	{
		CloseHandle(handles.hMutex);
		handles.hMutex = NULL;
	}

	if (handles.hEventSend != NULL)
	{
		CloseHandle(handles.hEventSend);
		handles.hEventSend = NULL;
	}

	if (handles.hEventRecv != NULL)
	{
		CloseHandle(handles.hEventRecv);
		handles.hEventRecv = NULL;
	}

	if (handles.hInitEvent != NULL)
	{
		CloseHandle(handles.hInitEvent);
		handles.hInitEvent = NULL;
	}

	connect_status = ConnectStatus::NOT_INITIALIZED;
	LOG_INFO("资源清理完成");
}

void SharedMemory::WriteInSMC(SharedMemoryCommand* smc, StructType StructDataType, RunStatus Runstats, const char* ErrorInfo)
{
	smc->StructDataType = StructDataType;
	smc->RunStatus = Runstats;
	strcpy_s(smc->ErrorInfo, sizeof(smc->ErrorInfo), ErrorInfo);
}
