#pragma once
#include "p_mpm.h"
#include "Struct.h"

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
	//就绪
	READY,
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

/*
* 结构体类型
* @param WDNL 存档路径列表与名称列表
* @param WDN 存档路径与名称
* @param UI 玩家信息
* @param PI_AS 玩家信息（进度与统计）
* @param PI_D 玩家信息（数据）
* @param PIWI 一次性存储单个玩家的所有数据
* @param PIWIL 存储玩家所有数据的容器结构体
*/
enum StructType
{
	/*
	* WorldDirectoriesNameList 存档路径列表与名称列表
	* @param world_directory_list 存档路径列表
	* @param world_name_list 存档名称列表
	*/
	WDNL,
	/*
	* WorldDirectoriesName 存档路径与名称
	* @param world_directory 存档路径
	* @param world_name 存档名称
	*/
	WDN,
	/*
	* UserInfo 玩家信息
	* @param user_name 玩家昵称
	* @param uuid 玩家UUID
	* @param expiresOn 玩家令牌过期时间
	*/
	UI,
	/*
	* PlayerInfo_AS 玩家信息（进度与统计）
	* @param path 文件路径
	* @param uuid 文件UUID（文件名）
	*/
	PI_AS,
	/*
	* PlayerInfo_Data 玩家信息（数据）
	* @param dat_path 数据文件路径
	* @param dat_old_path 旧数据文件路径
	* @param cosarmor_path 装饰盔甲数据文件路径
	* @param uuid 数据文件UUID
	* @param old_uuid 旧数据文件UUID
	* @param cosarmor_uuid 装饰盔甲数据文件UUID
	*/
	PI_D,
	/*
	* playerinworldinfo 一次性存储单个玩家的所有数据
	* @param world_dir_name 存档信息
	* @param player 玩家信息
	* @param adv_path 进度文件路径
	* @param pd_path 数据文件路径
	* @param pd_old_path 旧数据文件路径
	* @param cosarmor_path 装饰盔甲数据文件路径
	* @param st_path 统计文件路径
	*/
	PIWI,
	/*
	* PlayerInWorldInfoList 存储玩家所有数据的容器结构体
	* @param advancements_list 进度文件信息
	* @param playerdata_list 玩家信息（数据）
	* @param stats_list 进度文件信息
	* @param playerinworldinfo_list 一次性存储单个玩家的所有数据
	*/
	PIWIL
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

