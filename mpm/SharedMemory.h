#pragma once
#include "Struct.h"
#include "p_mpm.h"

/*
*连接状态
*@param CONNECTED 已连接
*@param NOT_CONNECTED 未连接
*@param NOT_INITIALIZED 未初始化
*@param NITIALIZED 已初始换
*/
enum ConnectStatus
{
	//未连接
	NOT_CONNECTED,
	//已连接
	CONNECTED,
	//未初始化
	NOT_INITIALIZED,
	//已初始化
	INITIALIZED
};

/*
* 写入状态
* @param EMPTY 没有上一任写入者
* @param WHITEWITHCPP 上一任由mpm写入
* @param WHITEWITHCS 上一任由g_mpm写入
*/
enum WriteStatus
{
	//没有上一任写入者
	EMPTY_WRITER,
	//上一任由mpm写入
	WHITEWITHCPP,
	//上一任由g_mpm写入
	WHITEWITHCS
};

/*
* 通信命令
* @param EMPTY 无命令
* @param SET_PATH 设置加载路径
*/
enum MemoryCommand
{
	//无命令
	EMPTY_COMMAND,
	//设置加载路径
	SET_PATH
};

/*
* 执行状态
* @param EMPTY_STATUS 无状态
* @param SUCCESSFUL 成功
* @param FAILED 失败
*/
enum RunStatus
{
	//无状态
	EMPTY_STATUS,
	//成功
	SUCCESSFUL,
	//失败
	FAILED
};

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
	inline SharedMemory() { this->smc->Writer = WriteStatus::EMPTY_WRITER; this->smc->DefCommand = MemoryCommand::EMPTY_COMMAND; this->smc->RunStatus = RunStatus::EMPTY_STATUS; }
	inline ~SharedMemory() { Clearup(); }

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

 