//COW.h COW类声明 是COMMAND_OPEN_WORLD命令的执行类
#pragma once
#include "piwbd.h"

class COW : public piwbd
{
public:
	COW() = default;
	~COW() = default;

	virtual void RunCommand() override;
};
