#include "CDP.h"

void CDP::RunCommand()
{
	std::vector<UserInfo> ui;
	std::string find_player_uuid;
	PlayerInWorldInfoList piwil;
	playerinworldinfo del_piwi;
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
			del_piwi.player.uuid = s.uuid;
			del_piwi.player.user_name = s.user_name;
		}
	}

	if (find_player_uuid.empty())
	{
		throw NoUserInfo();
	}

	out += del_piwi.player.user_name + "|UUID：" + del_piwi.player.uuid;

	for (int i = 0; i < GetWorldList().world_name_list.size(); i++)
	{
		del_piwi.world_dir_name.world_directory = GetWorldList().world_directory_list[i];
		del_piwi.world_dir_name.world_name = GetWorldList().world_name_list[i];

		try
		{
			LoadAllPlayerdata(del_piwi.world_dir_name.world_directory);
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
					del_piwi.adv_path = piwil.advancements_list[j].path;
				}
			}
		}

		for (int j = 0; j < piwil.playerdata_list.size(); j++)
		{
			if (find_player_uuid == piwil.playerdata_list[j].uuid)
			{
				if (!piwil.playerdata_list[j].dat_path.empty())
				{
					del_piwi.pd_path = piwil.advancements_list[j].path;
				}
			}

			if (find_player_uuid == piwil.playerdata_list[j].old_uuid)
			{
				if (!piwil.playerdata_list[j].dat_old_path.empty())
				{
					del_piwi.pd_old_path = piwil.playerdata_list[j].dat_old_path;
				}
			}

			if (find_player_uuid == piwil.playerdata_list[j].cosarmor_uuid)
			{
				if (!piwil.playerdata_list[j].cosarmor_path.empty())
				{
					del_piwi.cosarmor_path = piwil.playerdata_list[j].cosarmor_path;
				}
			}
		}

		for (int j = 0; j < piwil.stats_list.size(); j++)
		{
			if (find_player_uuid == piwil.stats_list[j].uuid)
			{
				if (!piwil.stats_list[j].path.empty())
				{
					del_piwi.st_path = piwil.stats_list[j].path;
				}
			}
		} 

		out += "\n存档：" + del_piwi.world_dir_name.world_name + "\n";
		std::bitset<5> is_del;
		is_del.set(0, MoveToRecycleBinWithPS(del_piwi.adv_path));
		is_del.set(1, MoveToRecycleBinWithPS(del_piwi.pd_path));
		is_del.set(2, MoveToRecycleBinWithPS(del_piwi.pd_old_path));
		is_del.set(3, MoveToRecycleBinWithPS(del_piwi.cosarmor_path));
		is_del.set(4, MoveToRecycleBinWithPS(del_piwi.st_path));

		if (is_del[0] == true)
		{
			out += "删除：" + del_piwi.adv_path + "\n";
		}
		else
		{
			out += "失败：文件已删除或不存在\n";
		}
		if (is_del[1] == true)
		{
			out += "删除：" + del_piwi..pd_path + "\n";
		}
		else
		{
			out += "失败：文件已删除或不存在\n";
		}
		if (is_del[2] == true)
		{
			out += "删除：" + del_piwi.pd_old_path + "\n";
		}
		else
		{
			out += "失败：文件已删除或不存在\n";
		}
		if (is_del[3] == true)
		{
			out += "删除：" + del_piwi.cosarmor_path + "\n";
		}
		else
		{
			out += "失败：文件已删除或不存在\n";
		}
		if (is_del[4] == true)
		{
			out += "删除：" + del_piwi.st_path + "\n";
		}
		else
		{
			out += "失败：文件已删除或不存在\n";
		}
	}

	if (DeletePlayerJSON(GetProcessingPath(), del_piwi.player.user_name))
	{
		out += "删除usercache和usernamecache\n";
	}

	SetShow(out);

	return;
}
