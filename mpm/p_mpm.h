//p_mpm.h 声明p_mpm类，作为执行类
#pragma once

#include "func.h"
#include "Logout.h"

//所有命令
#define COMMAND_EXIT 201
#define COMMAND_BREAK 202
#define COMMAND_OPEN_WORLD 203
#define COMMAND_OPEN_PLAYER 204
#define COMMAND_LIST_WORLD 205
#define COMMAND_LIST_PLAYER 206
#define COMMAND_DEL_PLAYER 207
#define COMMAND_DEL_WORLD 208
#define COMMAND_DEL_PW 209
#define COMMAND_DEL_JS 210
#define COMMAND_NULL_BACK 211
#define COMMAND_REFRESH 212

class p_mpm
{
public:
	inline p_mpm() { this->input_path = {}; this->Processed_input_path = {}; this->uct_world_list = {}; this->user_list = {}; this->world_list = {}; this->load_type = {}; this->CommandStr = {}; }//初始化所有变量
	~p_mpm() = default;

	inline void SetInputPath(const std::string _path) { this->input_path = _path; };//设置输入路径
	inline const std::string GetInputPath(void) { if (!this->input_path.empty()) { return this->input_path; } throw NullString(); }//获取输入路径
	inline const std::string GetProcessingPath(void) { if (this->Processed_input_path.empty()) { throw NullString(); } return this->Processed_input_path; }//获取处理后的输入路径
	/*[[deprecated("统一列表的使用，所以该函数被废弃")]]*/ inline const WorldDirectoriesNameList GetWorldList(void) { if (this->uct_world_list.world_directory_list.size() == 0 && this->uct_world_list.world_name_list.size() == 0) { throw NullStruct(); } return this->uct_world_list; }//获取世界列表容器结构体
	inline const std::vector<WorldDirectoriesName> GetSTLWorldList(void) { if (this->world_list.size() == 0) { throw NullVector(); } return this->world_list; }//获取世界列表结构体容器
	inline const std::vector<UserInfo> GetUserInfoList(void) { if (this->user_list.size() == 0) { throw NullVector(); } return this->user_list; }//获取玩家列表结构体容器
	inline const int GetPathLoadType() { if (this->load_type == 0) { throw TypeError(); } return this->load_type; }//设置路径加载模式
	inline const std::string GetLastCommand() { return this->CommandStr; }

	void ProcessingPath(void);//处理输入路径
	void ProcessingPath(const std::string _input_path);
	void PathLoadTpye(void);//检测加载方式
	void PathLoadTpye(const std::string _warld_path);
	void LoadWorldList(void);//加载世界列表
	void LoadWorldList(const std::string _world_path);
	void LoadWorldListSTL(void);
	void LoadWorldListSTL(const std::string _world_path);
	void LoadUserList(void);//加载玩家列表
	void LoadUserList(const std::string _JSON_path);
	void ReloadList(void);//重新加载user、world列表
	int ProcessCommand(const std::string _command);//处理命令
	virtual void RunCommand() {};//执行命令，需要重写

protected:
	inline void SetProcessingPath(std::string _processing_path) { if (_processing_path.empty()) { throw NullString(); } this->Processed_input_path = _processing_path; }//设置处理后的路径
	/*[[deprecated("统一列表的使用，所以该函数被废弃")]]*/ inline void SetWorldList(WorldDirectoriesNameList _uct_world_list) { if (_uct_world_list.world_directory_list.size() == 0 && _uct_world_list.world_name_list.size() == 0) { throw NullStruct(); } this->uct_world_list = _uct_world_list; }//设置世界列表容器结构体
	inline void SetSTLWorldList(std::vector<WorldDirectoriesName> _world_list) { if (_world_list.size() == 0) { throw NullVector(); } this->world_list = _world_list; }//设置世界列表结构体容器
	inline void SetUserInfoList(std::vector<UserInfo> _user_list) { if (_user_list.size() == 0) { throw NullVector(); } this->user_list = _user_list; }//设置玩家列表结构体容器
	inline void SetPathLoadType(int type) { if (type == 0) { throw TypeError(); } this->load_type = type; }//设置路径加载模式
	inline void SetLastCommand(const std::string _command) { this->CommandStr = _command; }//设置命令参数

private:
	std::string input_path;//输入路径
	std::string Processed_input_path;//处理后路径
	WorldDirectoriesNameList uct_world_list;//世界列表
	std::vector<WorldDirectoriesName> world_list;//世界列表容器
	std::vector<UserInfo> user_list;//用户数据列表
	int load_type;//路径加载类型
	std::string CommandStr;//指令参数
};
