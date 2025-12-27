//CLW.cpp 实现CLW
#include "CLW.h"

void CLW::RunCommand()
{
	int x = 0;
	PlayerInWorldInfoList piwil;
	piwil.playerinworldinfo_list.resize(GetWorldList().world_name_list.size());
	std::string out;

	for (int i = 0; i < GetWorldList().world_name_list.size(); i++)
	{
		LoadAdvancementList(GetWorldList().world_directory_list[i]);
		piwil.advancements_list = GetAdvancementsList();

		out += "\n存档：" + GetWorldList().world_name_list[i];

		for (const PlayerInfo_AS& w : piwil.advancements_list)
		{
			for (const UserInfo& b : GetUserInfoList())
			{
				if (w.uuid == b.uuid)
				{
					out += "\n玩家：" + b.user_name + "｜UUID：" + b.uuid;
				}
			}
		}
	}

	SetShow(out + "\n");
	return;
}