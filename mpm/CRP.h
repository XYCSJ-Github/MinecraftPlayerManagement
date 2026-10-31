#include "piwbd.h"
class CRP : public piwbd
{
public:
	CRP() = default;
	~CRP() = default;

	virtual void RunCommand() override;
};
