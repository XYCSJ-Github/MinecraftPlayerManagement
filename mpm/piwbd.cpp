#include "piwbd.h"

void piwbd::LoadAllPlayerdata(std::string world_path)
{
	if (world_path.empty()) { throw NullString(); }
	this->LoadAdvancementList(world_path);
	this->LoadPlayerdataList(world_path);
	this->LoadStatsList(world_path);
}

void piwbd::LoadAdvancementList(std::string world_path)
{
	if (world_path.empty()) { throw NullString(); }
	SetAdvancementsList(GetWorldPlayerAdvancements(world_path));
}

void piwbd::LoadPlayerdataList(std::string world_path)
{
	if (world_path.empty()) { throw NullString(); }
	SetPlayerdataList(GetWorldPlayerData(world_path));
}

void piwbd::LoadStatsList(std::string world_path)
{
	if (world_path.empty()) { throw NullString(); }
	SetStatsList(GetWorldPlayerStats(world_path));
}

void piwbd::DeletePlayersFiles(PlayerInWorldInfoList _piwil, std::string* _out)
{
	PlayerInWorldInfoList piwil = _piwil;
	std::string &out = out;
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
