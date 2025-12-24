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
