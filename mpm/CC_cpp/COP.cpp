//COP.cpp 实现COP（打开玩家：列出该玩家在各存档中的数据存在情况）
#include "COP.h"

void COP::RunCommand()
{
	const std::string target_name = GetLastCommand();
	if (target_name.empty())
	{
		throw CommandError();
	}

	// 目标玩家（名称来自 usercache/usernamecache）
	UserInfo target_user;
	bool found_user = false;
	try
	{
		for (const UserInfo& u : GetUserInfoList())
		{
			if (u.user_name == target_name)
			{
				target_user = u;
				found_user = true;
				break;
			}
		}
	}
	catch (const std::exception&)
	{
		found_user = false;
	}

	if (!found_user)
	{
		throw NoUserInfo();
	}

	// 世界列表
	WorldDirectoriesNameList wl;
	try { wl = GetWorldList(); }
	catch (const std::exception&) { wl = {}; }

	std::vector<PlayerInWorldInfo> rows;
	std::string out;
	out = "\n玩家：" + target_user.user_name + "|UUID：" + target_user.uuid;

	const size_t n = wl.world_directory_list.size() < wl.world_name_list.size()
		? wl.world_directory_list.size() : wl.world_name_list.size();

	for (size_t i = 0; i < n; i++)
	{
		std::vector<PlayerInfo_AS> adv, st;
		std::vector<PlayerInfo_Data> pd;

		try { adv = GetWorldPlayerAdvancements(wl.world_directory_list[i]); }
		catch (const std::exception&) { adv.clear(); }
		try { pd = GetWorldPlayerData(wl.world_directory_list[i]); }
		catch (const std::exception&) { pd.clear(); }
		try { st = GetWorldPlayerStats(wl.world_directory_list[i]); }
		catch (const std::exception&) { st.clear(); }

		PlayerInWorldInfo row;
		row.world_dir_name.world_directory = wl.world_directory_list[i];
		row.world_dir_name.world_name = wl.world_name_list[i];
		row.player = target_user;

		for (const auto& a : adv)
		{
			if (a.uuid == target_user.uuid && !a.path.empty()) { row.adv_path = "有"; break; }
		}
		for (const auto& d : pd)
		{
			if (d.uuid == target_user.uuid)
			{
				if (!d.dat_path.empty()) row.pd_path = "有";
				if (!d.dat_old_path.empty()) row.pd_old_path = "有";
				if (!d.cosarmor_path.empty()) row.cosarmor_path = "有";
			}
		}
		for (const auto& s : st)
		{
			if (s.uuid == target_user.uuid && !s.path.empty()) { row.st_path = "有"; break; }
		}

		if (!row.adv_path.empty() || !row.pd_path.empty() || !row.pd_old_path.empty()
			|| !row.cosarmor_path.empty() || !row.st_path.empty())
		{
			rows.push_back(row);
			out += "\n存档：" + row.world_dir_name.world_name
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
