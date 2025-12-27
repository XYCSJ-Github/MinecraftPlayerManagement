//CDPW.h 声明CDPW类，是COMMAND_DEL_PW命令的执行类
#pragma once
#include "piwbd.h"

class CDPW : public piwbd
{
public:
	CDPW() = default;
	~CDPW() = default;

	virtual void RunCommand() override;
};

