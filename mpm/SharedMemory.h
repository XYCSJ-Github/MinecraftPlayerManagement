// SharedMemory.h - 修改后的头文件
#pragma once

#include "Enums.h"
#include "Logout.h"
#include "p_mpm.h"
#include "Struct.h"
#include <chrono>
#include <thread>

//共享内存缓冲区大小
#define SHARED_MEMORY_BUF_SIZE 1024

//共享内存名称
constexpr const wchar_t MEMORY_NAME[14] = L"SharedMemory";
//主互斥锁名称
constexpr const wchar_t MUTEX_NAME[11] = L"MutexLock";
//初始事件名称
constexpr const wchar_t EVENT_INIT[23] = L"SharedMemoryInitEvent";
//发送事件名称
constexpr const wchar_t EVENT_SEND[11] = L"EventSend";
//接收事件名称
constexpr const wchar_t EVENT_RECV[11] = L"EventRecv";

class SharedMemory
{
public:
	SharedMemory() : smc(nullptr), connect_status(ConnectStatus::NOT_INITIALIZED)
	{
		memset(&handles, 0, sizeof(handles));
	}

	~SharedMemory() { Clearup(); }

	/*
	* 初始化
	* @return true 成功 false 失败
	*/
	bool Init()
	{
		// 先创建/打开同步对象和共享内存
		if (!OpenSyncObjects() || !ConnectMemory())
			return false;

		// 然后进行握手
		return WaitForCSharpReady();
	}

	/*
	* 打开同步对象
	* @return true 成功 false 失败
	*/
	bool OpenSyncObjects();

	/*
	* 连接共享内存
	* @return true 成功 false 失败
	*/
	bool ConnectMemory();

	/*
	* 等待C#端就绪
	* @return true 成功 false 失败
	*/
	bool WaitForCSharpReady();

	//设置初始化事件（通知C#端C++已就绪）
	void SetInitEvent() const;

	//进入命令循环
	void RunLoop();

	//处理接收到的命令
	void ProcessCommand();

	//清理资源
	void Clearup();

	//获取连接状态
	ConnectStatus GetStatus() const { return connect_status; }

	//快速写入smc
	void WriteInSMC(SharedMemoryCommand* smc, StructType StructDataType = StructType::EMPTY_STRUCT, RunStatus Runstats = RunStatus::SUCCESSFUL, const char* ErrorInfo = "无");

private:
	// 句柄结构体
	struct Handles
	{
		HANDLE hMapFile = NULL;      // 共享内存句柄
		HANDLE hMutex = NULL;        // 主互斥锁句柄
		HANDLE hEventSend = NULL;    // 发送事件句柄
		HANDLE hEventRecv = NULL;    // 接收事件句柄
		HANDLE hInitEvent = NULL;    // 初始化事件句柄
	};

	Handles handles;
	SharedMemoryCommand* smc;         // 共享内存结构体指针
	ConnectStatus connect_status;      // 连接状态
	p_mpm mp;
};