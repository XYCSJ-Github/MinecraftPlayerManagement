//CDPW.cpp 实现CDPW类
#include "CDPW.h"

void CDPW::RunCommand()
{
 	std::vector<std::string> pc = splitString(GetLastCommand(), ' ');
	if (pc[0].empty() && pc[1].empty())
	{
		throw CommandError();
	}

	int x = 0;
	std::string out;
	PlayerInWorldInfoList piwil;
	piwil.playerinworldinfo_list.resize(GetWorldList().world_name_list.size());

	piwil.playerinworldinfo_list[x].player.user_name = pc[0];
	piwil.playerinworldinfo_list[x].world_dir_name.world_name = pc[1];

	out += "\n从" + piwil.playerinworldinfo_list[x].world_dir_name.world_name + "中删除" + piwil.playerinworldinfo_list[x].player.user_name + "\n";

	for (const UserInfo& a : GetUserInfoList())
	{
		if (piwil.playerinworldinfo_list[x].player.user_name == a.user_name)
		{
			piwil.playerinworldinfo_list[x].player.uuid = a.uuid;
		}
	}

	for (int i = 0; i < GetWorldList().world_name_list.size(); i++)
	{
		if (piwil.playerinworldinfo_list[x].world_dir_name.world_name == GetWorldList().world_name_list[i])
		{
			piwil.playerinworldinfo_list[x].world_dir_name.world_directory = GetWorldList().world_directory_list[i];
		}
	}

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

	for (const PlayerInfo_AS& c : piwil.advancements_list)
	{
		if (piwil.playerinworldinfo_list[x].player.uuid == c.uuid && !c.path.empty())
		{
			piwil.playerinworldinfo_list[x].adv_path = c.path;
		}
	}

	for (const PlayerInfo_Data& c : piwil.playerdata_list)
	{
		if (piwil.playerinworldinfo_list[x].player.uuid == c.uuid && !c.dat_path.empty())
		{
			piwil.playerinworldinfo_list[x].pd_path = c.dat_path;
		}

		if (piwil.playerinworldinfo_list[x].player.uuid == c.old_uuid && !c.dat_old_path.empty())
		{
			piwil.playerinworldinfo_list[x].pd_old_path = c.dat_old_path;
		}

		if (piwil.playerinworldinfo_list[x].player.uuid == c.cosarmor_uuid && !c.cosarmor_path.empty())
		{
			piwil.playerinworldinfo_list[x].cosarmor_path = c.cosarmor_path;
		}
	}

	for (const PlayerInfo_AS& c : piwil.stats_list)
	{
		if (piwil.playerinworldinfo_list[x].player.uuid == c.uuid && !c.path.empty())
		{
			piwil.playerinworldinfo_list[x].st_path = c.path;
		}
	}

	out += "\n玩家：" + piwil.playerinworldinfo_list[x].player.uuid + "|UUID：" + piwil.playerinworldinfo_list[x].player.uuid + "\n存档：" + piwil.playerinworldinfo_list[x].world_dir_name.world_name + "|路径：" + piwil.playerinworldinfo_list[x].world_dir_name.world_directory + "\n";

	if (!piwil.playerinworldinfo_list[x].adv_path.empty() && !piwil.playerinworldinfo_list[x].cosarmor_path.empty() && !piwil.playerinworldinfo_list[x].pd_old_path.empty() && !piwil.playerinworldinfo_list[x].pd_path.empty() && !piwil.playerinworldinfo_list[x].st_path.empty())
	{
		DeletePlayersFiles(piwil, out, x);
	}
	else
	{
		out += "无数据\n";
	}

	SetShow(out);
	return;
}
