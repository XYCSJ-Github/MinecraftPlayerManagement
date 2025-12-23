//COW.h COW类声明 是COMMAND_OPEN_WORLD命令的实现类
#pragma once
#include "piwbd.h"

class COW : public piwbd//COW公开继承piwbd
{
public:
	COW() = default;
	~COW() = default;

	[[noreturn]]virtual void RunCommand() const override;

private:

};

