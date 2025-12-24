//CLW.cpp  µœ÷CLW
#include "CLW.h"

void CLW::RunCommand()
{
	PlayerInWorldInfoList piwil;
	playerinworldinfo piwi;
	std::string out;

	for (int i = 0; i < GetWorldList().world_name_list.size(); i++)
	{
		piwi.world_dir_name.world_name = GetWorldList().world_name_list[i];
		piwi.world_dir_name.world_directory = GetWorldList().world_directory_list[i];

		LoadAdvancementList(piwi.world_dir_name.world_directory);
		piwil.advancements_list = GetAdvancementsList();	

		out += "\n¥Êµµ£∫" + piwi.world_dir_name.world_name;

		for (const PlayerInfo_AS& w : piwil.advancements_list)
		{
			for (const UserInfo& x : GetUserInfoList())
			{
				if (w.uuid == x.uuid)
				{
					out += "\nÕÊº“£∫" + piwi.player.user_name + "£¸UUID£∫" + piwi.player.uuid + "\n";
				}
			}
		}

		out += "\n";
	}

	SetShow(out);
	return;
}