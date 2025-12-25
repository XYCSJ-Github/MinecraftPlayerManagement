#include "CDW.h"

void CDW::RunCommand()
{
	int x = 0;
	PlayerInWorldInfoList piwil;
	std::string out;

	for (int i = 0; i < GetWorldList().world_directory_list.size(); i++)
	{
		piwil.playerinworldinfo_list[x].world_dir_name.world_name = GetWorldList().world_name_list[i];
		piwil.playerinworldinfo_list[x].world_dir_name.world_directory = GetWorldList().world_directory_list[i];

		try
		{
			LoadAllPlayerdata(piwil.playerinworldinfo_list[x].world_dir_name.world_directory);
			piwil.advancements_list = GetAdvancementsList();
			piwil.playerdata_list = GetPlayerdataList();
			piwil.stats_list = GetStatsList();
		}
		catch (const std::exception& e)
		{
			throw e;
		}
	}

	out += "\n存档：" + piwil.playerinworldinfo_list[x].world_dir_name.world_name + "\n路径：" + piwil.playerinworldinfo_list[x].world_dir_name.world_directory + "\n";

	if (piwil.advancements_list.size() == 0 && piwil.playerdata_list.size() == 0 && piwil.stats_list.size() == 0)
	{
		out += "无数据\n";
		return;
	}

	size_t maxnum;//遍历容器没有遍历计数，所以创一个
	if (piwil.advancements_list.size() > piwil.playerdata_list.size())
	{
		if (piwil.advancements_list.size() > piwil.stats_list.size())
		{
			maxnum = piwil.advancements_list.size();
		}
		else
		{
			maxnum = piwil.stats_list.size();
		}
	}
	else
	{
		if (piwil.playerdata_list.size() > piwil.stats_list.size())
		{
			maxnum = piwil.playerdata_list.size();
		}
		else
		{
			maxnum = piwil.stats_list.size();
		}
	}

	for (int i = 0; i <= maxnum; i++)
	{
		for (const UserInfo& v : GetUserInfoList())
		{
			piwil.playerinworldinfo_list[x].player.user_name = v.user_name;
			piwil.playerinworldinfo_list[x].player.uuid = v.uuid;

			if (piwil.advancements_list.size() != 0)
			{
				for (int j = 0; j < piwil.advancements_list.size(); j++)
				{
					if (piwil.playerinworldinfo_list[x].player.uuid == piwil.advancements_list[j].uuid && !piwil.advancements_list[j].path.empty())
					{
						piwil.playerinworldinfo_list[x].adv_path = piwil.advancements_list[j].path;
					}
				}
			}
		}
	}

	return;
}
