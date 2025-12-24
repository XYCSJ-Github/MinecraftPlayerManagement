//COW.cpp 实现COW
#include "COW.h"

void COW::RunCommand()
{
	PlayerInWorldInfoList piwil = {};
	playerinworldinfo piwi;

	for (int i = 0; i < GetWorldList().world_name_list.size(); i++)
	{
		if (GetLastCommand() == GetWorldList().world_name_list[i])
		{
			piwi.world_dir_name.world_name = GetWorldList().world_name_list[i];
			piwi.world_dir_name.world_directory = GetWorldList().world_directory_list[i];

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
		}
	}

	std::string out = "\n存档：" + piwi.world_dir_name.world_name + "\n路径：" + piwi.world_dir_name.world_directory + "\n";

	for (int i = 0; i < GetUserInfoList().size(); i++)
	{
		piwi = {};

		piwi.player.user_name = GetUserInfoList()[i].user_name;
		piwi.player.uuid = GetUserInfoList()[i].uuid;

		for (int j = 0; j < piwil.advancements_list.size(); j++)
		{
			if (GetUserInfoList()[i].uuid == piwil.advancements_list[i].uuid)
			{
				piwi.adv_path = piwil.advancements_list[i].path;
			}
		}

		for (int j = 0; j < piwil.playerdata_list.size(); j++)
		{
			if (GetUserInfoList()[i].uuid == piwil.playerdata_list[i].uuid)
			{
				piwi.pd_path = piwil.playerdata_list[i].dat_path;
				piwi.pd_old_path = piwil.playerdata_list[i].dat_old_path;
				piwi.cosarmor_path = piwil.playerdata_list[i].cosarmor_path;
			}
		}

		for (int j = 0; j < piwil.stats_list.size(); j++)
		{
			if (GetUserInfoList()[i].uuid == piwil.stats_list[i].uuid)
			{
				piwi.st_path = piwil.stats_list[i].uuid;
			}
		}

		if (piwi.adv_path.length() != 0 || piwi.pd_path.length() != 0 || piwi.st_path.length() != 0 || piwi.pd_old_path.length() != 0)
		{
			out += "\n玩家：" + piwi.player.user_name + "|UUID：" + piwi.player.uuid + "\n进度：" + piwi.adv_path + "\n玩家数据：" + piwi.pd_path + "\n旧玩家数据：" + piwi.pd_old_path;
			if (piwi.cosarmor_path.length() != 0)
			{
				out += "\n装饰盔甲数据：" + piwi.cosarmor_path;
			}
			out += "\n统计数据：" + piwi.st_path + "\n";

			piwil.playerinworldinfo_list.push_back(piwi);
		}
	}

	SetShow(out);
	return;
}
