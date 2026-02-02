#include "SharedMemory.h"

bool SharedMemory::WaittingForCreateMemory()
{
	LOG_CREATE_MODEL_NAME("WaittingInit");

	LOG_INFO("等待创建方初始化");
	m_hInitEvent = CreateEvent(NULL, TRUE, FALSE, EVENT_INIT);
	if (m_hInitEvent == NULL)
	{
		LOG_ERROR("互斥锁创建失败失败");
		return false;
	}

	//互斥锁等待状态
	DWORD waitResult = WaitForSingleObject(m_hInitEvent, 30000);
	if (waitResult == WAIT_OBJECT_0)
	{
		LOG_INFO("创建方初始化完成");
		this->connect_status = ConnectStatus::INITIALIZED;
		return true;
	}
	else if (waitResult == WAIT_TIMEOUT)
	{
		LOG_WARNING("等待超时");
		return false;
	}
	else
	{
		LOG_ERROR("等待失败");
		return false;
	}

	return false;
}

bool SharedMemory::ConnectMemory(int maxRetries, DWORD retryInterval)
{
	LOG_CREATE_MODEL_NAME("ConnectMemory");

	LOG_INFO("开始连接内存");
	for (int i = 0; i < maxRetries; i++)
	{

		m_hMapFlie = OpenFileMapping(FILE_MAP_ALL_ACCESS, FALSE, MEMORY_NAME);
		if (m_hMapFlie == NULL)
			break;

		std::string out = "第" + i;
		out += "次连接内存失败，在" + retryInterval;
		out += "（ms）后重试";
		LOG_INFO(out);

		Sleep(retryInterval);
	}

	if (m_hMapFlie == NULL)
	{
		LOG_ERROR("无法打开共享内存");
		this->connect_status = ConnectStatus::NOT_CONNECTED;
		return false;
	}

	smc = (SharedMemoryCommand*)MapViewOfFile(m_hMapFlie, FILE_MAP_ALL_ACCESS, 0, 0, sizeof(SharedMemoryCommand));
	if (smc == NULL)
	{
		LOG_ERROR("无法连接共享内存");
		CloseHandle(m_hMapFlie);
		m_hMapFlie = NULL;
		return false;
	}
	else
	{
		LOG_INFO("已连接至内存");
		this->connect_status = ConnectStatus::CONNECTED;
		return true;
	}

	return false;
}

bool SharedMemory::OpenSyncObjects(int maxRetries, DWORD retryInterval)
{
	LOG_CREATE_MODEL_NAME("OpenSyncObj");

	for (int i = 0; i < maxRetries; i++)
	{
		m_hMutex = OpenMutex(MUTEX_MODIFY_STATE | SYNCHRONIZE, FALSE, MUTEX_NAME);
		m_hEvent_Send = OpenMutex(MUTEX_MODIFY_STATE | SYNCHRONIZE, FALSE, EVENT_SEND);
		m_hEvent_Recv = OpenMutex(MUTEX_MODIFY_STATE | SYNCHRONIZE, FALSE, EVENT_RECV);

		if (m_hMutex != NULL && m_hEvent_Send != NULL && m_hEvent_Recv != NULL)
			break;

		if (m_hMutex != NULL) CloseHandle(m_hMutex);
		if (m_hEvent_Send != NULL) CloseHandle(m_hEvent_Send);
		if (m_hEvent_Recv != NULL) CloseHandle(m_hEvent_Recv);

		m_hMutex = m_hEvent_Send = m_hEvent_Recv = NULL;

		std::string out = "第" + i;
		out += "次连接内存失败，在" + retryInterval;
		out += "（ms）后重试";
		LOG_WARNING(out);
	}

	if (m_hMutex == NULL || m_hEvent_Send == NULL || m_hEvent_Recv == NULL)
	{
		LOG_ERROR("无法打开同步对象");
		return false;
	}
	else
	{
		this->connect_status = ConnectStatus::CONNECTED;
		return true;
	}

	return false;
}

void SharedMemory::RunLoop()
{
	LOG_CREATE_MODEL_NAME("Main");

	if (connect_status == ConnectStatus::NOT_INITIALIZED)
	{
		LOG_WARNING("未初始化");
		return;
	}
	if (connect_status == ConnectStatus::NOT_CONNECTED)
	{
		LOG_WARNING("未连接");
	}

	LOG_INFO("进入消息处理循环...");

	while (connect_status == ConnectStatus::CONNECTED)
	{
		DWORD waitResult = WaitForSingleObject(m_hEvent_Send, INFINITE);

		//判断互斥锁
		if (waitResult == WAIT_OBJECT_0)
		{
			//判断写入者
			if (smc->Writer == WriteStatus::WHITEWITHCS)
			{
				LOG_DEBUG("g_mpm已更改");

				//判断命令不为空
				if (smc->DefCommand != MemoryCommand::EMPTY_COMMAND)
				{
					LOG_DEBUG("指令不为空");

					//重置命令执行状态
					smc->RunStatus = RunStatus::EMPTY_STATUS;//重置执行状态
					smc->ErrorInfo = "";//删除报错信息
					smc->StructDataType = StructType::EMPTY_STRUCT;//重置回复结构体

					//解析命令
					//switch (p_mpm::ProcessCommand(smc->DefCommand));

					//更改写入者
					smc->Writer = WriteStatus::WHITEWITHCPP;
					ReleaseMutex(m_hMutex);
				}

			}
			else
			{
				ReleaseMutex(m_hMutex);
			}
		}
		else
		{
			LOG_ERROR("等待失败");
			break;
		}
	}
}

void SharedMemory::Clearup()
{
	LOG_CREATE_MODEL_NAME("Clearup");

	LOG_INFO("清理资源");

	if (m_hEvent_Recv != NULL)
	{
		CloseHandle(m_hEvent_Recv);
		m_hEvent_Recv = NULL;
	}

	if (m_hEvent_Send != NULL)
	{
		CloseHandle(m_hEvent_Send);
		m_hEvent_Send = NULL;
	}

	if (m_hInitEvent != NULL)
	{
		CloseHandle(m_hInitEvent);
		m_hInitEvent = NULL;
	}

	if (m_hMapFlie != NULL)
	{
		CloseHandle(m_hMapFlie);
		m_hMapFlie = NULL;
	}

	if (m_hMutex != NULL)
	{
		CloseHandle(m_hMutex);
		m_hMutex = NULL;
	}

	if (smc != NULL)
	{
		UnmapViewOfFile(smc);
		m_hMutex = NULL;
	}
}
