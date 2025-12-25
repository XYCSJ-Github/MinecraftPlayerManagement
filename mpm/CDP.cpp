#include "CDP.h"

void CDP::RunCommand()
{
	int x = 0;
	std::vector<UserInfo> ui;
	std::string find_player_uuid;
	PlayerInWorldInfoList piwil;
	std::string out = "从所有时间删除";

	try
	{
		ui = GetUserInfoList();
	}
	catch (const std::exception& e)
	{
		throw e;
	}

	for (const UserInfo& s : ui)
	{
		if (GetLastCommand() == s.user_name)
		{
			find_player_uuid = s.uuid;
			piwil.playerinworldinfo_list[x].player.uuid = s.uuid;
			piwil.playerinworldinfo_list[x].player.user_name = s.user_name;
		}
	}

	if (find_player_uuid.empty())
	{
		throw NoUserInfo();
	}

	out += piwil.playerinworldinfo_list[x].player.user_name + "|UUID：" + piwil.playerinworldinfo_list[x].player.uuid;

	for (int i = 0; i < GetWorldList().world_name_list.size(); i++)
	{
		piwil.playerinworldinfo_list[x].world_dir_name.world_directory = GetWorldList().world_directory_list[i];
		piwil.playerinworldinfo_list[x].world_dir_name.world_name = GetWorldList().world_name_list[i];

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
			if (find_player_uuid == piwil.advancements_list[j].uuid)
			{
				if (!piwil.advancements_list[j].path.empty())
				{
					piwil.playerinworldinfo_list[x].adv_path = piwil.advancements_list[j].path;
				}
			}
		}

		for (int j = 0; j < piwil.playerdata_list.size(); j++)
		{
			if (find_player_uuid == piwil.playerdata_list[j].uuid)
			{
				if (!piwil.playerdata_list[j].dat_path.empty())
				{
					piwil.playerinworldinfo_list[x].pd_path = piwil.advancements_list[j].path;
				}
			}

			if (find_player_uuid == piwil.playerdata_list[j].old_uuid)
			{
				if (!piwil.playerdata_list[j].dat_old_path.empty())
				{
					piwil.playerinworldinfo_list[x].pd_old_path = piwil.playerdata_list[j].dat_old_path;
				}
			}

			if (find_player_uuid == piwil.playerdata_list[j].cosarmor_uuid)
			{
				if (!piwil.playerdata_list[j].cosarmor_path.empty())
				{
					piwil.playerinworldinfo_list[x].cosarmor_path = piwil.playerdata_list[j].cosarmor_path;
				}
			}
		}

		for (int j = 0; j < piwil.stats_list.size(); j++)
		{
			if (find_player_uuid == piwil.stats_list[j].uuid)
			{
				if (!piwil.stats_list[j].path.empty())
				{
					piwil.playerinworldinfo_list[x].st_path = piwil.stats_list[j].path;
				}
			}
		}

		out += "\n存档：" + piwil.playerinworldinfo_list[x].world_dir_name.world_name + "\n";
		std::bitset<5> is_del;
		is_del.set(0, MoveToRecycleBinWithPS(piwil.playerinworldinfo_list[x].adv_path));
		is_del.set(1, MoveToRecycleBinWithPS(piwil.playerinworldinfo_list[x].pd_path));
		is_del.set(2, MoveToRecycleBinWithPS(piwil.playerinworldinfo_list[x].pd_old_path));
		is_del.set(3, MoveToRecycleBinWithPS(piwil.playerinworldinfo_list[x].cosarmor_path));
		is_del.set(4, MoveToRecycleBinWithPS(piwil.playerinworldinfo_list[x].st_path));

		if (is_del[0] == true)
		{
			out += "删除：" + piwil.playerinworldinfo_list[x].adv_path + "\n";
		}
		else
		{
			out += "失败：文件已删除或不存在\n";
		}
		if (is_del[1] == true)
		{
			out += "删除：" + piwil.playerinworldinfo_list[x].pd_path + "\n";
		}
		else
		{
			out += "失败：文件已删除或不存在\n";
		}
		if (is_del[2] == true)
		{
			out += "删除：" + piwil.playerinworldinfo_list[x].pd_old_path + "\n";
		}
		else
		{
			out += "失败：文件已删除或不存在\n";
		}
		if (is_del[3] == true)
		{
			out += "删除：" + piwil.playerinworldinfo_list[x].cosarmor_path + "\n";
		}
		else
		{
			out += "失败：文件已删除或不存在\n";
		}
		if (is_del[4] == true)
		{
			out += "删除：" + piwil.playerinworldinfo_list[x].st_path + "\n";
		}
		else
		{
			out += "失败：文件已删除或不存在\n";
		}
	}

	if (DeletePlayerJSON(GetProcessingPath(), piwil.playerinworldinfo_list[x].player.user_name))
	{
		out += "删除usercache和usernamecache\n";
	}

	SetShow(out);

	return;
}
