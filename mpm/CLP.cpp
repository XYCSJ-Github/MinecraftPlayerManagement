#include "CLP.h"

void CLP::RunCommand()
{
	std::string out;

	try
	{
		if (GetUserInfoList().size() == 0)
		{
			throw NoUserInfo();
		}
		else
		{
			for (const UserInfo& x : GetUserInfoList())
			{
				out += "\n玩家：" + x.user_name + "|UUID：" + x.uuid + "|过期时间：" + x.expiresOn;
			}
		}
	}
	catch (const std::exception& e)
	{
		throw e;
	}

	SetShow(out + "\n");
	return;
}
