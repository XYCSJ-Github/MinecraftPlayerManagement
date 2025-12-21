//p_mpm.h 声明p_mpm类，作为执行类
#pragma once

#include "func.h"
#include "Logout.h"

class p_mpm
{
public:
	p_mpm();
	~p_mpm() {};

	void SetInputPath(const std::string _path) { this->input_path = _path; };//设置输入路径
	const std::string GetInputPath(void) { if (!this->input_path.empty()) { return this->input_path; } throw NullString(); }//获取输入路径
	const std::string GetProcessingPath(void) { if (this->Processed_input_path.empty()) { throw NullString(); } return this->Processed_input_path; }//获取处理后的输入路径
	[[deprecated("统一列表的使用，所以该函数被废弃")]]
	const WorldDirectoriesNameList GetWorldList(void) { if (this->uct_world_list.world_directory_list.size() == 0 && this->uct_world_list.world_name_list.size() == 0) { throw NullStruct(); return this->uct_world_list; } }//获取世界列表容器结构体
	const std::vector<WorldDirectoriesName> GetSTLWorldList(void) { if (this->world_list.size() == 0) { throw NullVector(); } return this->world_list; }//获取世界列表结构体容器
	const std::vector<UserInfo> GetUserInfoList(void) { if (this->user_list.size() == 0) { throw NullVector(); } return this->user_list; }//获取玩家列表结构体容器
 
	void ProcessingPath(const std::string);//处理输入路径
	void LoadWorldList(void);//加载世界列表
	void LoadWorldList(const std::string _world_path);
	void LoadUserList(void);//加载玩家列表
	void LoadUserList(const std::string _JSON_path);
	void ReloadList(void);//重新加载user、world列表
	void ProcessCommand(const std::string _command);//处理命令
	virtual void RunCommand() const = 0;//执行命令，需要重写

private:
	void SetProcessingPath(std::string _processing_path) { if (_processing_path.empty()) { throw NullString(); } this->Processed_input_path = _processing_path; }//设置处理后的路径
	[[deprecated("统一列表的使用，所以该函数被废弃")]]
	void SetWorldList(WorldDirectoriesNameList _uct_world_list) { if (_uct_world_list.world_directory_list.size() == 0 && _uct_world_list.world_name_list.size() == 0) { throw NullStruct(); } this->uct_world_list = _uct_world_list; }//设置世界列表容器结构体
	void SetSTLWorldList(std::vector<WorldDirectoriesName> _world_list) { if (_world_list.size() == 0) { throw NullVector(); } this->world_list = _world_list; }//设置世界列表结构体容器
	void SetUserInfoList(std::vector<UserInfo> _user_list) { if (_user_list.size() == 0) { throw NullVector(); } this->user_list = _user_list; }//设置玩家列表结构体容器

private:
	std::string input_path;//输入路径
	std::string Processed_input_path;//处理后路径
	WorldDirectoriesNameList uct_world_list;//世界列表
	std::vector<WorldDirectoriesName> world_list;//世界列表容器
	std::vector<UserInfo> user_list;//用户数据列表
};

