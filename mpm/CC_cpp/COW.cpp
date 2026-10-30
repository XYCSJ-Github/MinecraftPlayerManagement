//COW.cpp 实现COW（打开存档：列出该存档内玩家的数据存在情况）
#include "COW.h"

void COW::RunCommand()
{
	const std::string target_world = GetLastCommand();
	if (target_world.empty())
	{
		throw CommandError();
	}

	// 世界列表
	WorldDirectoriesNameList wl;
	try { wl = GetWorldList(); }
	catch (const std::exception&) { wl = {}; }

	// 找到目标存档
	const size_t n = wl.world_directory_list.size() < wl.world_name_list.size()
		? wl.world_directory_list.size() : wl.world_name_list.size();
	size_t world_index = n;
	for (size_t i = 0; i < n; i++)
	{
		if (wl.world_name_list[i] == target_world)
		{
			world_index = i;
			break;
		}
	}
	if (world_index == n)
	{
		throw CommandError();
	}

	// 目标存档的文件
	std::vector<PlayerInfo_AS> adv, st;
	std::vector<PlayerInfo_Data> pd;
	try { adv = GetWorldPlayerAdvancements(wl.world_directory_list[world_index]); }
	catch (const std::exception&) { adv.clear(); }
	try { pd = GetWorldPlayerData(wl.world_directory_list[world_index]); }
	catch (const std::exception&) { pd.clear(); }
	try { st = GetWorldPlayerStats(wl.world_directory_list[world_index]); }
	catch (const std::exception&) { st.clear(); }

	// 玩家列表
	std::vector<UserInfo> users;
	try { users = GetUserInfoList(); }
	catch (const std::exception&) { users.clear(); }

	std::vector<PlayerInWorldInfo> rows;
	std::string out;
	out = "\n存档：" + wl.world_name_list[world_index] + "|路径：" + wl.world_directory_list[world_index];

	for (const auto& u : users)
	{
		PlayerInWorldInfo row;
		row.world_dir_name.world_directory = wl.world_directory_list[world_index];
		row.world_dir_name.world_name = wl.world_name_list[world_index];
		row.player = u;

		for (const auto& a : adv)
		{
			if (a.uuid == u.uuid && !a.path.empty()) { row.adv_path = "有"; break; }
		}
		for (const auto& d : pd)
		{
			if (d.uuid == u.uuid)
			{
				if (!d.dat_path.empty()) row.pd_path = "有";
				if (!d.dat_old_path.empty()) row.pd_old_path = "有";
				if (!d.cosarmor_path.empty()) row.cosarmor_path = "有";
			}
		}
		for (const auto& s : st)
		{
			if (s.uuid == u.uuid && !s.path.empty()) { row.st_path = "有"; break; }
		}

		if (!row.adv_path.empty() || !row.pd_path.empty() || !row.pd_old_path.empty()
			|| !row.cosarmor_path.empty() || !row.st_path.empty())
		{
			rows.push_back(row);
			out += "\n玩家：" + u.user_name + "|UUID：" + u.uuid
				+ "|进度：" + row.adv_path + "|数据：" + row.pd_path + "|旧数据：" + row.pd_old_path
				+ "|盔甲：" + row.cosarmor_path + "|统计：" + row.st_path;
		}
	}

	PlayerInWorldInfoList piwil;
	piwil.playerinworldinfo_list = rows;

	this->SetPlayerInWorldInfoList(piwil);
	SetShow(out.empty() ? "无数据" : out);
	return;
}
