//CDJS.h 声明CDJS类，是COMMAND_DEL_JS命令的执行类
#pragma once
#include "piwbd.h"
class CDJS : public piwbd
{
public:
	CDJS() = default;
	~CDJS() = default;

	virtual void RunCommand() override;
};

