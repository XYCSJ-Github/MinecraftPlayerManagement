#pragma warning(disable: 4005) //禁用警告C4005: 宏重定义
#pragma once
#include <string>
#include <iostream>
#include <Windows.h>
#include <ctime>
#include <chrono>

#define LOG_MOD_INFO     0
#define LOG_MOD_WARNING  1
#define LOG_MOD_ERROR    2
#define LOG_MOD_DEBUG    3

#define LOG(msg, model, out_mod, join_time) LO::Logout(msg, model, out_mod, join_time)

#define LOG_INFO(msg, model)    LO::Logout(msg, model, LOG_MOD_INFO)
#define LOG_WARNING(msg, model) LO::Logout(msg, model, LOG_MOD_WARNING)
#define LOG_ERROR(msg, model)   LO::Logout(msg, model, LOG_MOD_ERROR)
#define LOG_DEBUG(msg, model)   LO::Logout(msg, model, LOG_MOD_DEBUG)

#define LOG_DEBUG_OUT   LO::SetDebugLogOut();

#define LOG_CREATE_MODEL_NAME(model, name) std::string model = name;

namespace LO {
    //定义颜色枚举
    enum ConsoleColor {
        Black = 0,
        Blue = 1,
        Green = 2,
        Cyan = 3,
        Red = 4,
        Magenta = 5,
        Yellow = 6,
        White = 7,
        Gray = 8,
        BrightBlue = 9,
        BrightGreen = 10,
        BrightCyan = 11,
        BrightRed = 12,
        BrightMagenta = 13,
        BrightYellow = 14,
        BrightWhite = 15
    };

    void Logout(const std::string msg, const std::string model, const int out_mod = 0, const bool join_time = true);
    std::tm* GetTime();
	long long GetMSTime();
    void SetDebugLogOut();
	bool GetIsDebug();
}
