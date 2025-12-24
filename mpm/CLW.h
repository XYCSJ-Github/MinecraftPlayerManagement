//CLW.h 声明CLW类，COMMAND_LIST_WORLD命令执行类
#pragma once
#include "piwbd.h"
class CLW : public piwbd
{
public:
	CLW() = default;
	~CLW() = default;

	virtual void RunCommand() override;
};

