#pragma once
#include "piwbd.h"
class CDW : public piwbd
{
public:
	CDW() = default;
	~CDW() = default;

	virtual void RunCommand() override;
};

