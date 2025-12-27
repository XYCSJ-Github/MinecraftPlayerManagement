#pragma once
#include "piwbd.h"
class CLP : public piwbd
{
public:
	CLP() = default;
	~CLP() = default;

	virtual void RunCommand() override;
};

