// SharedMemoryClient.h
#pragma once

#include <windows.h>
#include <string>
#include <functional>
#include <iostream>

// 定义共享内存缓冲区大小
#ifndef SHARED_MEMORY_BUF_SIZE
#define SHARED_MEMORY_BUF_SIZE 256
#endif

// 定义共享对象名称
#define DEFAULT_MEMORY_NAME     L"ShareMemory"
#define DEFAULT_MUTEX_NAME      L"ShareMutex"
#define DEFAULT_EVENT_A_TO_B    L"EventFromAToB"
#define DEFAULT_EVENT_B_TO_A    L"EventFromBToA"
#define DEFAULT_INIT_EVENT      L"SharedMemoryInitEvent"

/**
 * @brief 基础共享数据结构体，使用1字节对齐确保与C#端兼容
 *
 * @struct SharedDataBase
 * 这是共享内存的基础数据结构，包含基本的消息交换功能。
 * 可以通过继承此结构体来扩展功能。
 */
#pragma pack(push, 1)
struct SharedDataBase {
    char MessageFromA[SHARED_MEMORY_BUF_SIZE];  ///< 从A进程发送到B进程的消息缓冲区
    char ReplyFromB[SHARED_MEMORY_BUF_SIZE];    ///< 从B进程回复到A进程的消息缓冲区
    BOOL NewMessageFromA;                       ///< 新消息标志，A到B，使用BOOL确保与C#的bool兼容
    BOOL NewReplyFromB;                         ///< 新回复标志，B到A，使用BOOL确保与C#的bool兼容
    BOOL ExitFlag;                              ///< 退出标志，用于通知对方进程退出
};
#pragma pack(pop)

/**
 * @brief 扩展共享数据结构体示例
 *
 * @struct ExtendedSharedData
 * 展示如何扩展基础结构体以包含更多数据。
 * 在实际使用中，可以根据需要定义自己的扩展结构体。
 */
#pragma pack(push, 1)
struct ExtendedSharedData : public SharedDataBase {
    // 添加额外的数据字段
    int MessageCount;                           ///< 消息计数器
    long long Timestamp;                        ///< 时间戳
    int ProcessId;                              ///< 进程ID
    BYTE Reserved[100];                         ///< 保留字段，用于未来扩展
};
#pragma pack(pop)

/**
 * @class SharedMemoryClient
 * @brief C++端共享内存客户端类，用于与C#进程通信
 *
 * 这个类封装了共享内存通信的所有细节，包括：
 * 1. 等待共享内存初始化
 * 2. 打开共享内存和同步对象
 * 3. 发送和接收消息
 * 4. 处理退出指令
 * 5. 资源清理
 *
 * 使用模板设计以支持不同的共享数据结构体
 */
template<typename T = SharedDataBase>
class SharedMemoryClient {
public:
    /**
     * @brief 构造函数
     * @param memoryName 共享内存名称
     * @param mutexName  互斥体名称
     * @param eventAToB  A到B事件名称
     * @param eventBToA  B到A事件名称
     * @param initEvent  初始化事件名称
     */
    SharedMemoryClient(
        const wchar_t* memoryName = DEFAULT_MEMORY_NAME,
        const wchar_t* mutexName = DEFAULT_MUTEX_NAME,
        const wchar_t* eventAToB = DEFAULT_EVENT_A_TO_B,
        const wchar_t* eventBToA = DEFAULT_EVENT_B_TO_A,
        const wchar_t* initEvent = DEFAULT_INIT_EVENT
    );

    /**
     * @brief 析构函数，自动清理资源
     */
    ~SharedMemoryClient();

    /**
     * @brief 初始化共享内存客户端
     * @return true-初始化成功，false-初始化失败
     *
     * 执行以下步骤：
     * 1. 等待A进程初始化共享内存
     * 2. 打开共享内存文件映射
     * 3. 映射共享内存视图
     * 4. 打开同步对象（互斥体、事件）
     * 5. 发送就绪通知给A进程
     */
    bool Initialize();

    /**
     * @brief 启动消息处理循环
     * @param messageHandler 消息处理回调函数
     *
     * 循环执行以下操作：
     * 1. 等待来自A进程的事件信号
     * 2. 检查是否有新消息
     * 3. 调用消息处理回调函数
     * 4. 发送回复（如果消息处理函数返回非空字符串）
     * 5. 检查退出标志
     */
    void RunLoop(std::function<std::string(const std::string&)> messageHandler = nullptr);

    /**
     * @brief 检查是否正在运行
     * @return true-正在运行，false-已停止
     */
    bool IsRunning() const { return m_running; }

    /**
     * @brief 获取已处理的消息数量
     * @return 消息计数
     */
    int GetMessageCount() const { return m_messageCount; }

    /**
     * @brief 获取共享数据指针
     * @return 指向共享数据的指针
     *
     * @warning 直接操作共享数据时需要考虑线程安全
     */
    T* GetSharedData() { return m_pSharedData; }

    /**
     * @brief 发送回复消息给A进程
     * @param reply 回复内容
     *
     * 执行以下步骤：
     * 1. 获取互斥体锁
     * 2. 清空回复缓冲区
     * 3. 复制回复内容
     * 4. 设置回复标志
     * 5. 刷新内存视图
     * 6. 释放互斥体锁
     * 7. 触发B到A事件
     */
    void SendReply(const std::string& reply);

    /**
     * @brief 强制停止消息循环
     */
    void Stop();

    /**
     * @brief 获取最后错误码
     * @return Windows系统错误码
     */
    DWORD GetLastError() const { return m_lastError; }

private:
    /**
     * @brief 等待共享内存初始化完成
     * @return true-等待成功，false-等待失败或超时
     */
    bool WaitForSharedMemoryInitialization();

    /**
     * @brief 打开共享内存文件映射
     * @param maxRetries 最大重试次数
     * @param retryInterval 重试间隔（毫秒）
     * @return true-打开成功，false-打开失败
     */
    bool OpenSharedMemory(int maxRetries = 10, DWORD retryInterval = 1000);

    /**
     * @brief 打开同步对象（互斥体、事件）
     * @param maxRetries 最大重试次数
     * @param retryInterval 重试间隔（毫秒）
     * @return true-打开成功，false-打开失败
     */
    bool OpenSyncObjects(int maxRetries = 10, DWORD retryInterval = 1000);

    /**
     * @brief 发送就绪通知给A进程
     */
    void SendReadyNotification();

    /**
     * @brief 清理所有资源
     */
    void Cleanup();

    /**
     * @brief 默认消息处理函数
     * @param message 收到的消息
     * @return 回复消息
     */
    std::string DefaultMessageHandler(const std::string& message);

private:
    // 共享内存相关句柄
    HANDLE m_hMapFile = NULL;
    T* m_pSharedData = NULL;

    // 同步对象句柄
    HANDLE m_hMutex = NULL;
    HANDLE m_hEventAtoB = NULL;
    HANDLE m_hEventBtoA = NULL;
    HANDLE m_hInitEvent = NULL;

    // 配置参数
    std::wstring m_memoryName;
    std::wstring m_mutexName;
    std::wstring m_eventAToB;
    std::wstring m_eventBtoA;
    std::wstring m_initEvent;

    // 运行状态
    bool m_running = false;
    int m_messageCount = 0;
    DWORD m_lastError = ERROR_SUCCESS;
};

// ========== 模板类的实现部分 ==========
// 注意：模板类的实现必须放在头文件中

template<typename T>
SharedMemoryClient<T>::SharedMemoryClient(
    const wchar_t* memoryName,
    const wchar_t* mutexName,
    const wchar_t* eventAToB,
    const wchar_t* eventBToA,
    const wchar_t* initEvent
) :
    m_memoryName(memoryName ? memoryName : DEFAULT_MEMORY_NAME),
    m_mutexName(mutexName ? mutexName : DEFAULT_MUTEX_NAME),
    m_eventAToB(eventAToB ? eventAToB : DEFAULT_EVENT_A_TO_B),
    m_eventBtoA(eventBToA ? eventBToA : DEFAULT_EVENT_B_TO_A),
    m_initEvent(initEvent ? initEvent : DEFAULT_INIT_EVENT)
{
    printf("====== SharedMemoryClient 已创建 ======\n");
    printf("[B] 进程ID: %d\n", GetCurrentProcessId());
}

template<typename T>
SharedMemoryClient<T>::~SharedMemoryClient()
{
    Cleanup();
}

template<typename T>
bool SharedMemoryClient<T>::Initialize()
{
    printf("[B] 正在初始化共享内存客户端...\n");

    // 第一步：等待A进程初始化共享内存
    if (!WaitForSharedMemoryInitialization())
    {
        printf("[B] 等待共享内存初始化失败，退出\n");
        return false;
    }

    // 第二步：打开共享内存
    if (!OpenSharedMemory())
    {
        printf("[B] 打开共享内存失败\n");
        return false;
    }

    // 第三步：打开同步对象
    if (!OpenSyncObjects())
    {
        printf("[B] 打开同步对象失败\n");
        return false;
    }

    // 第四步：发送就绪通知
    SendReadyNotification();

    printf("[B] 共享内存通信已初始化完成\n");
    m_running = true;

    return true;
}

template<typename T>
void SharedMemoryClient<T>::RunLoop(std::function<std::string(const std::string&)> messageHandler)
{
    if (!m_running)
    {
        printf("[B] 客户端未初始化或已停止\n");
        return;
    }

    printf("[B] 进入消息处理循环...\n");

    while (m_running)
    {
        DWORD waitResult = WaitForSingleObject(m_hEventAtoB, INFINITE);

        if (waitResult == WAIT_OBJECT_0)
        {
            WaitForSingleObject(m_hMutex, INFINITE);

            if (m_pSharedData->NewMessageFromA)
            {
                m_messageCount++;

                // 确保字符串以null结尾
                m_pSharedData->MessageFromA[SHARED_MEMORY_BUF_SIZE - 1] = '\0';
                std::string message = m_pSharedData->MessageFromA;

                printf("[B] 收到消息 #%d: %s (长度: %zu)\n",
                    m_messageCount, message.c_str(), message.length());

                // 检查退出指令
                if (m_pSharedData->ExitFlag)
                {
                    printf("[B] 收到退出指令\n");

                    std::string exitReply = "B进程已收到退出指令，正在退出...";
                    SendReply(exitReply);

                    ReleaseMutex(m_hMutex);
                    m_running = false;
                    break;
                }

                // 重置消息标志
                m_pSharedData->NewMessageFromA = FALSE;
                ReleaseMutex(m_hMutex);

                // 处理消息
                std::string reply;
                if (messageHandler)
                {
                    reply = messageHandler(message);
                }
                else
                {
                    reply = DefaultMessageHandler(message);
                }

                // 发送回复（如果消息处理函数返回非空字符串）
                if (!reply.empty())
                {
                    SendReply(reply);
                }
            }
            else
            {
                ReleaseMutex(m_hMutex);
            }
        }
        else
        {
            m_lastError = GetLastError();
            printf("[B] 等待事件失败 (错误: %d)\n", m_lastError);
            break;
        }
    }
}

template<typename T>
void SharedMemoryClient<T>::SendReply(const std::string& reply)
{
    WaitForSingleObject(m_hMutex, INFINITE);

    // 记录写入前的状态
    printf("[B] 写入前 - ReplyFromB: [%s], NewReplyFromB: %d\n",
        m_pSharedData->ReplyFromB, m_pSharedData->NewReplyFromB);

    // 清空缓冲区
    memset(m_pSharedData->ReplyFromB, 0, SHARED_MEMORY_BUF_SIZE);

    // 复制字符串
    size_t copyLen = min(reply.length(), SHARED_MEMORY_BUF_SIZE - 1);
    memcpy(m_pSharedData->ReplyFromB, reply.c_str(), copyLen);
    m_pSharedData->ReplyFromB[copyLen] = '\0';

    m_pSharedData->NewReplyFromB = TRUE;
    m_pSharedData->NewMessageFromA = FALSE;

    // 验证写入
    printf("[B] 写入后 - ReplyFromB: [%s], NewReplyFromB: %d\n",
        m_pSharedData->ReplyFromB, m_pSharedData->NewReplyFromB);

    // 强制刷新内存
    FlushViewOfFile(m_pSharedData, sizeof(T));

    ReleaseMutex(m_hMutex);

    SetEvent(m_hEventBtoA);

    printf("[B] 发送回复: %s (长度: %zu)\n", reply.c_str(), reply.length());
}

template<typename T>
void SharedMemoryClient<T>::Stop()
{
    m_running = false;
    if (m_hEventAtoB)
    {
        SetEvent(m_hEventAtoB); // 唤醒等待
    }
}

template<typename T>
bool SharedMemoryClient<T>::WaitForSharedMemoryInitialization()
{
    printf("[B] 等待A进程初始化共享内存...\n");

    m_hInitEvent = CreateEvent(NULL, TRUE, FALSE, m_initEvent.c_str());
    if (m_hInitEvent == NULL)
    {
        m_lastError = GetLastError();
        printf("[B] 创建初始化事件失败 (错误: %d)\n", m_lastError);
        return false;
    }

    DWORD waitResult = WaitForSingleObject(m_hInitEvent, 30000);

    if (waitResult == WAIT_OBJECT_0)
    {
        printf("[B] 收到初始化完成信号\n");
        return true;
    }
    else if (waitResult == WAIT_TIMEOUT)
    {
        printf("[B] 等待初始化超时\n");
        return false;
    }
    else
    {
        m_lastError = GetLastError();
        printf("[B] 等待初始化失败 (错误: %d)\n", m_lastError);
        return false;
    }
}

template<typename T>
bool SharedMemoryClient<T>::OpenSharedMemory(int maxRetries, DWORD retryInterval)
{
    printf("[B] 正在打开共享内存...\n");

    // 尝试打开共享内存
    for (int i = 0; i < maxRetries; i++)
    {
        m_hMapFile = OpenFileMapping(FILE_MAP_ALL_ACCESS, FALSE, m_memoryName.c_str());
        if (m_hMapFile != NULL)
            break;

        printf("[B] 第%d次尝试打开共享内存失败，等待%u毫秒后重试...\n",
            i + 1, retryInterval);
        Sleep(retryInterval);
    }

    if (m_hMapFile == NULL)
    {
        m_lastError = GetLastError();
        printf("[B] 无法打开共享内存 (错误: %d)\n", m_lastError);
        return false;
    }

    m_pSharedData = (T*)MapViewOfFile(m_hMapFile, FILE_MAP_ALL_ACCESS, 0, 0, sizeof(T));
    if (m_pSharedData == NULL)
    {
        m_lastError = GetLastError();
        printf("[B] 无法映射共享内存 (错误: %d)\n", m_lastError);
        CloseHandle(m_hMapFile);
        m_hMapFile = NULL;
        return false;
    }

    return true;
}

template<typename T>
bool SharedMemoryClient<T>::OpenSyncObjects(int maxRetries, DWORD retryInterval)
{
    printf("[B] 正在打开同步对象...\n");

    for (int i = 0; i < maxRetries; i++)
    {
        m_hMutex = OpenMutex(MUTEX_MODIFY_STATE | SYNCHRONIZE, FALSE, m_mutexName.c_str());
        m_hEventAtoB = OpenEvent(EVENT_MODIFY_STATE | SYNCHRONIZE, FALSE, m_eventAToB.c_str());
        m_hEventBtoA = OpenEvent(EVENT_MODIFY_STATE | SYNCHRONIZE, FALSE, m_eventBtoA.c_str());

        if (m_hMutex != NULL && m_hEventAtoB != NULL && m_hEventBtoA != NULL)
            break;

        if (m_hMutex != NULL) CloseHandle(m_hMutex);
        if (m_hEventAtoB != NULL) CloseHandle(m_hEventAtoB);
        if (m_hEventBtoA != NULL) CloseHandle(m_hEventBtoA);

        m_hMutex = m_hEventAtoB = m_hEventBtoA = NULL;

        printf("[B] 第%d次尝试打开同步对象失败，等待%u毫秒后重试...\n",
            i + 1, retryInterval);
        Sleep(retryInterval);
    }

    if (m_hMutex == NULL || m_hEventAtoB == NULL || m_hEventBtoA == NULL)
    {
        printf("[B] 无法打开同步对象\n");
        return false;
    }

    return true;
}

template<typename T>
void SharedMemoryClient<T>::SendReadyNotification()
{
    WaitForSingleObject(m_hMutex, INFINITE);
    memset(m_pSharedData->ReplyFromB, 0, SHARED_MEMORY_BUF_SIZE);
    strcpy_s(m_pSharedData->ReplyFromB, SHARED_MEMORY_BUF_SIZE, "B进程已就绪");
    m_pSharedData->NewReplyFromB = TRUE;
    m_pSharedData->NewMessageFromA = FALSE;
    ReleaseMutex(m_hMutex);
    SetEvent(m_hEventBtoA);
}

template<typename T>
std::string SharedMemoryClient<T>::DefaultMessageHandler(const std::string& message)
{
    std::string reply;

    if (message == "ping" || message == "测试")
    {
        reply = "B进程已收到: " + message + "，回复: pong";
    }
    else if (message.find("你好") != std::string::npos)
    {
        reply = "B进程: 你好，我是后台服务进程";
    }
    else if (message.find("状态") != std::string::npos)
    {
        reply = "B进程运行正常，已处理 " + std::to_string(m_messageCount) + " 条消息";
    }
    else
    {
        reply = "B进程已收到消息: \"" + message + "\"\n";
        reply += "消息长度: " + std::to_string(message.length()) + "\n";
        reply += "接收时间: " + std::to_string(GetTickCount()) + "\n";
        reply += "消息序号: " + std::to_string(m_messageCount);
    }

    return reply;
}

template<typename T>
void SharedMemoryClient<T>::Cleanup()
{
    printf("[B] 清理资源...\n");

    if (m_pSharedData != NULL)
    {
        UnmapViewOfFile(m_pSharedData);
        m_pSharedData = NULL;
    }

    if (m_hMapFile != NULL)
    {
        CloseHandle(m_hMapFile);
        m_hMapFile = NULL;
    }

    if (m_hMutex != NULL)
    {
        CloseHandle(m_hMutex);
        m_hMutex = NULL;
    }

    if (m_hEventAtoB != NULL)
    {
        CloseHandle(m_hEventAtoB);
        m_hEventAtoB = NULL;
    }

    if (m_hEventBtoA != NULL)
    {
        CloseHandle(m_hEventBtoA);
        m_hEventBtoA = NULL;
    }

    if (m_hInitEvent != NULL)
    {
        CloseHandle(m_hInitEvent);
        m_hInitEvent = NULL;
    }

    printf("[B] 已退出，共处理 %d 条消息\n", m_messageCount);
    printf("====== SharedMemoryClient 已清理 ======\n");
}