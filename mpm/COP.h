//COP.h COP类声明 是COMMAND_OPEN_PLAYER命令的执行类 
#pragma once
#include "piwbd.h"

class COP : public piwbd
{
public:
	COP() = default;
	~COP() = default;
	
	virtual void RunCommand() override;
}; 
