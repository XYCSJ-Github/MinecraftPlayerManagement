//COW.cpp 实现COW
#include "COW.h"

void COW::RunCommand()
{
	int x = 0;
	PlayerInWorldInfoList piwil;
	piwil.playerinworldinfo_list.resize(GetWorldList().world_name_list.size());

	for (int i = 0; i < GetWorldList().world_name_list.size(); i++)
	{
		if (GetLastCommand() == GetWorldList().world_name_list[i])
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
	}

	std::string out = "\n存档：" + piwil.playerinworldinfo_list[x].world_dir_name.world_name + "\n路径：" + piwil.playerinworldinfo_list[x].world_dir_name.world_directory + "\n";

	for (int i = 0; i < GetUserInfoList().size(); i++)
	{
		piwil.playerinworldinfo_list[x] = {};

		piwil.playerinworldinfo_list[x].player.user_name = GetUserInfoList()[i].user_name;
		piwil.playerinworldinfo_list[x].player.uuid = GetUserInfoList()[i].uuid;

		for (int j = 0; j < piwil.advancements_list.size(); j++)
		{
			if (GetUserInfoList()[i].uuid == piwil.advancements_list[j].uuid)
			{
				piwil.playerinworldinfo_list[x].adv_path = piwil.advancements_list[j].path;
			}
		}

		for (int j = 0; j < piwil.playerdata_list.size(); j++)
		{
			if (GetUserInfoList()[i].uuid == piwil.playerdata_list[j].uuid)
			{
				piwil.playerinworldinfo_list[x].pd_path = piwil.playerdata_list[j].dat_path;
				piwil.playerinworldinfo_list[x].pd_old_path = piwil.playerdata_list[j].dat_old_path;
				piwil.playerinworldinfo_list[x].cosarmor_path = piwil.playerdata_list[j].cosarmor_path;
			}
		}

		for (int j = 0; j < piwil.stats_list.size(); j++)
		{
			if (GetUserInfoList()[i].uuid == piwil.stats_list[j].uuid)
			{
				piwil.playerinworldinfo_list[x].st_path = piwil.stats_list[j].path;
			}
		}

		if (piwil.playerinworldinfo_list[x].adv_path.length() != 0 || piwil.playerinworldinfo_list[x].pd_path.length() != 0 || piwil.playerinworldinfo_list[x].st_path.length() != 0 || piwil.playerinworldinfo_list[x].pd_old_path.length() != 0)
		{
			out += "\n玩家：" + piwil.playerinworldinfo_list[x].player.user_name + "|UUID：" + piwil.playerinworldinfo_list[x].player.uuid + "\n进度：" + piwil.playerinworldinfo_list[x].adv_path + "\n玩家数据：" + piwil.playerinworldinfo_list[x].pd_path + "\n旧玩家数据：" + piwil.playerinworldinfo_list[x].pd_old_path;
			if (piwil.playerinworldinfo_list[x].cosarmor_path.length() != 0)
			{
				out += "\n装饰盔甲数据：" + piwil.playerinworldinfo_list[x].cosarmor_path;
			}
			out += "\n统计数据：" + piwil.playerinworldinfo_list[x].st_path + "\n";
		}
	}

	SetShow(out);
	return;
}
