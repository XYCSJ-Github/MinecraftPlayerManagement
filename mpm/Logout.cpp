#include <iomanip>
#include "Logout.h"

bool is_debug = false;

namespace LO {

	void Logout(const std::string msg, const std::string model, const int out_mod, const bool join_time)
	{
		HANDLE console = GetStdHandle(STD_OUTPUT_HANDLE);
		switch (out_mod)
		{
		case LOG_MOD_INFO:
		{
			SetConsoleTextAttribute(console, ConsoleColor::White);
			if (join_time)
			{
				std::tm* time = GetTime();
				long long millis = GetMSTime();
				if (time == nullptr)
				{
					std::cout << "[Error getting time]";
					break;
				}
				std::cout << "[" << std::setfill('0') << std::setw(2) << time->tm_hour << ":" << std::setfill('0') << std::setw(2) << time->tm_min << ":" << std::setfill('0') << std::setw(2) << time->tm_sec << "." << std::setfill('0') << std::setw(3) << millis << "]";
			}
			std::cout << "[Info]" << "[" << model << "]: " << msg << std::endl;
			SetConsoleTextAttribute(console, ConsoleColor::White);
			break;
		}

		case LOG_MOD_WARNING:
		{
			SetConsoleTextAttribute(console, ConsoleColor::Yellow);
			if (join_time)
			{
				std::tm* time = GetTime();
				long long millis = GetMSTime();
				if (time == nullptr)
				{
					std::cout << "[Error getting time]";
					break;
				}
				std::cout << "[" << std::setfill('0') << std::setw(2) << time->tm_hour << ":" << std::setfill('0') << std::setw(2) << time->tm_min << ":" << std::setfill('0') << std::setw(2) << time->tm_sec << "." << std::setfill('0') << std::setw(3) << millis << "]";
			}
			std::cout << "[Warning]" << "[" << model << "]: " << msg << std::endl;
			SetConsoleTextAttribute(console, ConsoleColor::White);
			break;
		}

		case LOG_MOD_ERROR:
		{
			SetConsoleTextAttribute(console, ConsoleColor::Red);
			if (join_time)
			{
				std::tm* time = GetTime();
				long long millis = GetMSTime();
				if (time == nullptr)
				{
					std::cerr << "[Error getting time]";
					break;
				}
				std::cerr << "[" << std::setfill('0') << std::setw(2) << time->tm_hour << ":" << std::setfill('0') << std::setw(2) << time->tm_min << ":" << std::setfill('0') << std::setw(2) << time->tm_sec << "." << std::setfill('0') << std::setw(3) << millis << "]";
			}
			std::cerr << "[Error]" << "[" << model << "]: " << msg << std::endl;
			SetConsoleTextAttribute(console, ConsoleColor::White);
			break;
		}

		case LOG_MOD_DEBUG:
		{
			if (is_debug == false) break;

			SetConsoleTextAttribute(console, ConsoleColor::BrightYellow);
			if (join_time)
			{
				std::tm* time = GetTime();
				long long millis = GetMSTime();
				if (time == nullptr)
				{
					std::cout << "[Error getting time]";
					break;
				}
				std::cout << "[" << std::setfill('0') << std::setw(2) << time->tm_hour << ":" << std::setfill('0') << std::setw(2) << time->tm_min << ":" << std::setfill('0') << std::setw(2) << time->tm_sec << "." << std::setfill('0') << std::setw(3) << millis << "]";
			}
			std::cout << "[Debug]" << "[" << model << "]: " << msg << std::endl;
			SetConsoleTextAttribute(console, ConsoleColor::White);
			break;
		}

		default:
			break;
		}
	}

	std::tm* GetTime()
	{
		auto now = std::chrono::system_clock::now();

		std::time_t now_time = std::chrono::system_clock::to_time_t(now);

		static std::tm timeinfo;
		if (localtime_s(&timeinfo, &now_time) != 0) {
			return nullptr;
		}
		return &timeinfo;
	}
	long long GetMSTime()
	{
		auto now = std::chrono::system_clock::now();
		auto duration_since_epoch = now.time_since_epoch();
		auto millis = std::chrono::duration_cast<std::chrono::milliseconds>(duration_since_epoch).count() % 1000;

		return millis;
	}
	void SetDebugLogOut()
	{
		is_debug = true;
	}
	bool GetIsDebug()
	{
		return is_debug;
	}
}