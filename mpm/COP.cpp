//COP.cpp 实现COP
#include "COP.h"

void COP::RunCommand()
{
	int x = 0;
	PlayerInWorldInfoList piwil;
	piwil.playerinworldinfo_list.resize(GetUserInfoList().size());
	std::string out;

	for (int i = 0; i < GetUserInfoList().size(); i++)
	{
		if (GetLastCommand() == GetUserInfoList()[i].user_name)
		{
			piwil.playerinworldinfo_list[x].player.user_name = GetUserInfoList()[i].user_name;
			piwil.playerinworldinfo_list[x].player.uuid = GetUserInfoList()[i].uuid;
		}
	}

	out = "\n玩家：" + piwil.playerinworldinfo_list[x].player.user_name + "|UUID：" + piwil.playerinworldinfo_list[x].player.uuid;

	for (int i = 0; i < GetWorldList().world_name_list.size(); i++)
	{
		piwil.playerinworldinfo_list[x].world_dir_name.world_name = GetWorldList().world_name_list[i];
		piwil.playerinworldinfo_list[x].world_dir_name.world_directory = GetWorldList().world_directory_list[i];

		out += "\n存档：" + piwil.playerinworldinfo_list[x].world_dir_name.world_name;

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

		for (int j = 0; j < piwil.advancements_list.size(); j++)
		{
			if (piwil.playerinworldinfo_list[x].player.uuid == piwil.advancements_list[i].uuid)
			{
				if (!piwil.advancements_list[j].path.empty())
				{
					piwil.playerinworldinfo_list[x].adv_path = "有";
				}
				else
				{
					piwil.playerinworldinfo_list[x].adv_path = "无";
				}
			}
		}

		for (int j = 0; j < piwil.playerdata_list.size(); j++)
		{
			if (piwil.playerinworldinfo_list[x].player.uuid == piwil.playerdata_list[j].uuid)
			{
				if (!piwil.playerdata_list[j].dat_path.empty())
				{
					piwil.playerinworldinfo_list[x].pd_path = "有";
				}
				else
				{
					piwil.playerinworldinfo_list[x].pd_path = "无";
				}
			}

			if (piwil.playerinworldinfo_list[x].player.uuid == piwil.playerdata_list[i].uuid)
			{
				if (!piwil.playerdata_list[j].dat_old_path.empty())
				{
					piwil.playerinworldinfo_list[x].pd_old_path = "有";
				}
				else
				{
					piwil.playerinworldinfo_list[x].pd_old_path = "无";
				}
			}

			if (piwil.playerinworldinfo_list[x].player.uuid == piwil.playerdata_list[i].uuid)
			{
				if (!piwil.playerdata_list[j].cosarmor_path.empty())
				{
					piwil.playerinworldinfo_list[x].cosarmor_path = "有";
				}
				else
				{
					piwil.playerinworldinfo_list[x].cosarmor_path = "无";
				}
			}
		}

		for (int j = 0; j < piwil.stats_list.size(); j++)
		{
			if (piwil.playerinworldinfo_list[x].player.uuid == piwil.stats_list[j].uuid)
			{
				if (!piwil.stats_list[j].path.empty())
				{
					piwil.playerinworldinfo_list[x].st_path = "有";
				}
				else
				{
					piwil.playerinworldinfo_list[x].st_path = "无";
				}
			}
		}

		out += "|进度：" + piwil.playerinworldinfo_list[x].adv_path + "|数据：" + piwil.playerinworldinfo_list[x].pd_path + piwil.playerinworldinfo_list[x].pd_old_path + piwil.playerinworldinfo_list[x].cosarmor_path + "|统计：" + piwil.playerinworldinfo_list[x].st_path + "\n";
	}

	SetShow(out);
	return;
}
