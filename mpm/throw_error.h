//throw_error.h 声明异常类型
#pragma once

#include <iostream>

struct UnknownPath : public std::exception
{
	const char* what() const throw()
	{
		return "路径不存在，请检查后重新输入！";
	}
};

struct NotOpen : public std::exception
{
	const char* what() const throw()
	{
		return "无法打开文件！";
	}
};

struct ReadError : public std::exception
{
	const char* what() const throw()
	{
		return "读取用文件失败！";
	}
};

struct NullString : public std::exception
{
	const char* what() const throw()
	{
		return "空的字符串";
	}
};

struct NullStruct : public std::exception
{
	const char* what() const throw()
	{
		return "空的结构体";
	}
};

struct NullVector : public std::exception
{
	const char* what() const throw()
	{
		return "空的容器";
	}
};

struct NoUserInfo : public std::exception
{
	const char* what() const throw()
	{
		return "没有玩家信息";
	}
};

struct CommandError : public std::exception
{
	const char* what() const throw()
	{
		return "命令错误";
	}
};

struct TypeError : public std::exception
{
	const char* what() const throw()
	{
		return "路径加载类型错误";
	}
};