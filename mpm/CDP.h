//CDP.h 声明CDP类，COMMAND_DEL_PLAYER命令的执行类
#pragma once
#include "piwbd.h"

class CDP : public piwbd
{
public:
	CDP() = default;
	~CDP() = default;

	virtual void RunCommand() override;
};
