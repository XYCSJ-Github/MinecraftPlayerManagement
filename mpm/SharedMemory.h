#pragma once

#include "Enums.h"
#include "p_mpm.h"
#include "Struct.h"

//共享内存缓冲区大小
#define SHARED_MEMORY_BUF_SIZE 1024

//共享内存名称
#define MEMORY_NAME L"SharedMemory"
//主互斥锁名称
#define MUTEX_NAME L"MutexLock"
//初始事件名称
#define EVENT_INIT L"SharedMemoryInitEvent"
//发送事件名称
#define EVENT_SEND L"EventSend"
//接收事件参名称
#define EVENT_RECV L"EventRecv"

class SharedMemory
{
public:
	inline SharedMemory() { this->smc = {}; } //{ this->smc->Writer = WriteStatus::EMPTY_WRITER; this->smc->DefCommand = MemoryCommand::EMPTY_COMMAND; this->smc->RunStatus = RunStatus::EMPTY_STATUS; }
	inline ~SharedMemory() { Clearup(); }

	/*
	* 初始化
	* @return false
	*/
	bool Init()
	{
		return WaittingForCreateMemory() && ConnectMemory(5, 300) && OpenSyncObjects(5, 300);
	}

	/*
	* 连接共享内存
	* @return false 互斥锁创建、等待等导致的失败
	* @return true 等待创建方成功
	*/
	bool WaittingForCreateMemory();

	/*
	* 连接共享内存
	* @param maxRetries 最大重试次数
	* @param retryInterval 重试间隔（ms）
	* @return false 打开失败
	* @return true 打开成功
	*/
	bool ConnectMemory(int maxRetries, DWORD retryInterval);

	/*
	* 同步状态
	* @param maxRetries 最大重试次数
	* @param retryInterval 重试间隔（ms）
	* @return false 打开失败
	* @return true 成功打开同步
	*/
	bool OpenSyncObjects(int maxRetries, DWORD retryInterval);

	//进入命令循环
	void RunLoop();

	//清理资源
	void Clearup();


private:
	/*
	*连接状态
	*@param CONNECTED 已连接
	*@param NOT_CONNECTED 未连接
	*@param NOT_INITIALIZED 未初始化
	*@param INITIALIZED 已初始换
	*/
	ConnectStatus connect_status = ConnectStatus::NOT_INITIALIZED;

	//初始化互斥锁句柄
	HANDLE m_hInitEvent = NULL;
	//共享内存句柄
	HANDLE m_hMapFlie = NULL;
	//主互斥锁句柄
	HANDLE m_hMutex = NULL;
	//发送互斥锁句柄
	HANDLE m_hEvent_Send = NULL;
	//接受互斥锁句柄
	HANDLE m_hEvent_Recv = NULL;

	//共享内存结构体指针
	SharedMemoryCommand* smc;
};

