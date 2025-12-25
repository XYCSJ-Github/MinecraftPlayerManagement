//CLW.cpp  µœ÷CLW
#include "CLW.h"

void CLW::RunCommand()
{
	int x = 0;
	PlayerInWorldInfoList piwil;
	std::string out;

	for (int i = 0; i < GetWorldList().world_name_list.size(); i++)
	{
		piwil.playerinworldinfo_list[x].world_dir_name.world_name = GetWorldList().world_name_list[i];
		piwil.playerinworldinfo_list[x].world_dir_name.world_directory = GetWorldList().world_directory_list[i];

		LoadAdvancementList(piwil.playerinworldinfo_list[x].world_dir_name.world_directory);
		piwil.advancements_list = GetAdvancementsList();

		out += "\n¥Êµµ£∫" + piwil.playerinworldinfo_list[x].world_dir_name.world_name;

		for (const PlayerInfo_AS& w : piwil.advancements_list)
		{
			for (const UserInfo& b : GetUserInfoList())
			{
				if (w.uuid == b.uuid)
				{
					out += "\nÕÊº“£∫" + piwil.playerinworldinfo_list[x].player.user_name + "£¸UUID£∫" + piwil.playerinworldinfo_list[x].player.uuid + "\n";
				}
			}
		}

		out += "\n";
	}

	SetShow(out);
	return;
}