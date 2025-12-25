#pragma once
#include "piwbd.h"
#include <bitset>
class CDP : public piwbd
{
public:
	CDP() = default;
	~CDP() = default;

	virtual void RunCommand() override;
};
