//piwbd.h 声明piwbd类继承p_mpm作为执行拓展类，大多数override RunCommand类都继承此类
#pragma once
#include "p_mpm.h"
#include <bitset>

class piwbd : public p_mpm//piwbd公开继承p_mpm
{
public:
	inline piwbd() { this->advancements_list = {}; this->playerdata_list = {}; this->stats_list = {}; this->show = {}; }//初始化
	~piwbd() = default;

	inline const std::vector<PlayerInfo_AS> GetAdvancementsList() { if (this->advancements_list.empty()) { throw NullVector(); } return this->advancements_list; }//获取adv、playerdata、sta等数据
	inline const std::vector<PlayerInfo_Data> GetPlayerdataList() { if (this->playerdata_list.empty()) { throw NullVector(); } return this->playerdata_list; }
	inline const std::vector<PlayerInfo_AS> GetStatsList() { if (this->stats_list.empty()) { throw NullVector(); } return this->stats_list; }
	inline const std::string GetShow() { if (this->show.empty()) { throw NullString(); } return this->show; }//获取输出字符串

	//加载指定世界的所有list
	void LoadAllPlayerdata(std::string world_path);
	void LoadAdvancementList(std::string world_path);
	void LoadPlayerdataList(std::string world_path);
	void LoadStatsList(std::string world_path);

private:
	inline void SetAdvancementsList(std::vector<PlayerInfo_AS> adv_list) { if (adv_list.empty()) { throw NullVector(); } this->advancements_list = adv_list; }//设置adv、playerdata、sta等数据
	inline void SetPlayerdataList(std::vector<PlayerInfo_Data> pd_list) { if (pd_list.empty()) { throw NullVector(); } this->playerdata_list = pd_list; }
	inline void SetStatsList(std::vector<PlayerInfo_AS> st_list) { if (st_list.empty()) { throw NullVector(); } this->stats_list = st_list; }

protected:
	inline void SetShow(std::string str_show) { if (str_show.empty()) { throw NullString(); } this->show = str_show; }//设置输出字符串
	void DeletePlayersFiles(PlayerInWorldInfoList _piwil, std::string &_out, int x);//用PowerShell删除文件

private:
	std::vector<PlayerInfo_AS> advancements_list;//Advancements结构体STL
	std::vector<PlayerInfo_Data> playerdata_list;//Playerdata结构体STL
	std::vector<PlayerInfo_AS> stats_list;//Stats结构体STL
	std::vector<playerinworldinfo> piw_list;//单个玩家数据结构体STL
	std::string show;//输出到控制台的字符串
};

