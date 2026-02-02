//piwbd.h 声明piwbd类继承p_mpm作为执行拓展类，大多数override RunCommand类都继承此类
#pragma once
#include "p_mpm.h"
#include <bitset>

class piwbd : public p_mpm
{
public:
	inline piwbd() { this->advancements_list = {}; this->playerdata_list = {}; this->stats_list = {}; this->show = {}; }//初始化
	~piwbd() = default;

	/*
	* 获取Advancements列表
	* @return Advancements列表
	* @throws NullVector 空容器
	*/
	inline const std::vector<PlayerInfo_AS> GetAdvancementsList() { if (this->advancements_list.empty()) { throw NullVector(); } return this->advancements_list; }
	/*
	* 获取Playerdata列表
	* @return Playerdata列表
	* @throws NullVector 空容器
	*/
	inline const std::vector<PlayerInfo_Data> GetPlayerdataList() { if (this->playerdata_list.empty()) { throw NullVector(); } return this->playerdata_list; }
	/*
	* 获取Stats列表
	* @return Stats列表
	* @throws NullVector 空容器
	*/
	inline const std::vector<PlayerInfo_AS> GetStatsList() { if (this->stats_list.empty()) { throw NullVector(); } return this->stats_list; }
	/*
	* 获取执行输出
	* @return 输出
	* @throws NullString 空字符串
	*/
	inline const std::string GetShow() { if (this->show.empty()) { throw NullString(); } return this->show; }

	/*
	* 加载指定世界的所有list
	* @param world_path 存档路径
	*/
	void LoadAllPlayerdata(std::string world_path);
	/*
	* 加载指定世界的所有Advancement
	* @param world_path 存档路径
	*/
	void LoadAdvancementList(std::string world_path);
	/*
	* 加载指定世界的Playerdata
	* @param world_path 存档路径
	*/
	void LoadPlayerdataList(std::string world_path);
	/*
	* 加载指定世界的Stats
	* @param world_path 存档路径
	*/
	void LoadStatsList(std::string world_path);

private:
	/*
	* 获取Advancements列表
	* @param Advancements列表
	* @throws NullVector 空容器
	*/
	inline void SetAdvancementsList(std::vector<PlayerInfo_AS> adv_list) { if (adv_list.empty()) { throw NullVector(); } this->advancements_list = adv_list; }
	/*
	* 获取Playerdata列表
	* @param Playerdata列表
	* @throws NullVector 空容器
	*/
	inline void SetPlayerdataList(std::vector<PlayerInfo_Data> pd_list) { if (pd_list.empty()) { throw NullVector(); } this->playerdata_list = pd_list; }
	/*
	* 获取Stats列表
	* @param Stats列表
	* @throws NullVector 空容器
	*/
	inline void SetStatsList(std::vector<PlayerInfo_AS> st_list) { if (st_list.empty()) { throw NullVector(); } this->stats_list = st_list; }

protected:
	/*
	* 获取执行输出
	* @param 输出字符串
	* @throws NullString 空字符串
	*/
	inline void SetShow(std::string str_show) { if (str_show.empty()) { throw NullString(); } this->show = str_show; }
	/*
	* 用PowerShell删除文件
	* @param _piwil 填充有数据的结构体
	* @param _out 输出日志
	* @param 取容器元素的序列号
	*/
	void DeletePlayersFiles(PlayerInWorldInfoList _piwil, std::string& _out, int x);

private:
	//Advancements结构体STL
	std::vector<PlayerInfo_AS> advancements_list;
	//Playerdata结构体STL
	std::vector<PlayerInfo_Data> playerdata_list;
	//Stats结构体STL
	std::vector<PlayerInfo_AS> stats_list;
	//单个玩家数据结构体STL
	std::vector<PlayerInWorldInfo> piw_list;
	//输出到控制台的字符串
	std::string show;

public:
	//重载>>运算符使两个同piwbd或p_mpm父类对象可以传递基础数据
	piwbd& operator>>(p_mpm& p)
	{
		try
		{
			this->SetInputPath(p.GetInputPath());
			this->SetProcessingPath(p.GetProcessingPath());
			this->SetWorldList(p.GetWorldList());
			this->SetSTLWorldList(p.GetSTLWorldList());
			this->SetUserInfoList(p.GetUserInfoList());
			this->SetPathLoadType(p.GetPathLoadType());
			this->SetLastCommand(p.GetLastCommand());
		}
		catch (const std::exception& e)
		{
			throw e;
		}

		return *this;
	}
};

