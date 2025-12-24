//COP.cpp 实现COP
#include "COP.h"

void COP::RunCommand()
{
	PlayerInWorldInfoList piwil;
	playerinworldinfo piwi;

	for (int i = 0; i < GetUserInfoList().size(); i++)
	{
		if (GetLastCommand() == GetUserInfoList()[i].user_name)
		{
			piwi.player.user_name = GetUserInfoList()[i].user_name;
			piwi.player.uuid = GetUserInfoList()[i].uuid;
		}
	}

	std::string out = "\n玩家：" + piwi.player.user_name + "|UUID：" + piwi.player.uuid;

	for (int i = 0; i < GetWorldList().world_name_list.size(); i++)
	{
		piwi.world_dir_name.world_name = GetWorldList().world_name_list[i];
		piwi.world_dir_name.world_directory = GetWorldList().world_directory_list[i];

		out += "\n存档：" + piwi.world_dir_name.world_name;

		try
		{
			LoadAllPlayerdata(piwi.world_dir_name.world_directory);
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
			if (piwi.player.uuid == piwil.advancements_list[i].uuid)
			{
				if (!piwil.advancements_list[j].path.empty())
				{
					piwi.adv_path = "有";
				}
				else
				{
					piwi.adv_path = "无";
				}
			}
		}

		for (int j = 0; j < piwil.playerdata_list.size(); j++)
		{
			if (piwi.player.uuid == piwil.playerdata_list[j].uuid)
			{
				if (!piwil.playerdata_list[j].dat_path.empty())
				{
					piwi.pd_path = "有";
				}
				else
				{
					piwi.pd_path = "无";
				}
			}

			if (piwi.player.uuid == piwil.playerdata_list[i].uuid)
			{
				if (!piwil.playerdata_list[j].dat_old_path.empty())
				{
					piwi.pd_old_path = "有";
				}
				else
				{
					piwi.pd_old_path = "无";
				}
			}

			if (piwi.player.uuid == piwil.playerdata_list[i].uuid)
			{
				if (!piwil.playerdata_list[j].cosarmor_path.empty())
				{
					piwi.cosarmor_path = "有";
				}
				else
				{
					piwi.cosarmor_path = "无";
				}
			}
		}

		for (int j = 0; j < piwil.stats_list.size(); j++)
		{
			if (piwi.player.uuid == piwil.stats_list[j].uuid)
			{
				if (!piwil.stats_list[j].path.empty())
				{
					piwi.st_path = "有";
				}
				else
				{
					piwi.st_path = "无";
				}
			}
		}

		out += "\n进度：" + piwi.adv_path + "|数据：" + piwi.pd_path + piwi.pd_old_path + piwi.cosarmor_path + "|统计：" + piwi.st_path + "\n";
	}

	SetShow(out);
	return;
}
