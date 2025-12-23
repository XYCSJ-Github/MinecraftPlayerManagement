//piwbd.h 声明piwbd类继承p_mpm作为执行拓展类，大多数override RunCommand类都继承此类
#pragma once
#include "p_mpm.h"
class piwbd : public p_mpm
{
public:
	inline piwbd() { this->advancements_list = {}; this->playerdata_list = {}; this->stats_list = {}; this->show = {}; }
	~piwbd() = default;

	inline const std::vector<PlayerInfo_AS> GetAdvancementsList() { if (this->advancements_list.empty()) { throw NullVector(); } return this->advancements_list; }
	inline const std::vector<PlayerInfo_Data> GetPlayerdataList() { if (this->playerdata_list.empty()) { throw NullVector(); } return this->playerdata_list; }
	inline const std::vector<PlayerInfo_AS> GetStatsList() { if (this->stats_list.empty()) { throw NullVector(); } return this->stats_list; }
	inline const std::string GetShow() { if (this->show.empty()) { throw NullString(); } return this->show; }

private:
	inline void SetAdvancementsList(std::vector<PlayerInfo_AS> adv_list) { if (adv_list.empty()) { throw NullVector(); } this->advancements_list = adv_list; }
	inline void SetPlayerdataList(std::vector<PlayerInfo_Data> pd_list) { if (pd_list.empty()) { throw NullVector(); } this->playerdata_list = pd_list; }
	inline void SetStatsList(std::vector<PlayerInfo_AS> st_list) { if (st_list.empty()) { throw NullVector(); } this->stats_list = st_list; }
	inline void SetShow(std::string str_show) { if (str_show.empty()) { throw NullString(); } this->show = str_show; }

	std::vector<PlayerInfo_AS> advancements_list;
	std::vector<PlayerInfo_Data> playerdata_list;
	std::vector<PlayerInfo_AS> stats_list;
	std::vector<playerinworldinfo> piw_list;
	std::string show;
};

