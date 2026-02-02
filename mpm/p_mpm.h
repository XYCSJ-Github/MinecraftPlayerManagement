//p_mpm.h 声明p_mpm类，作为执行类
#pragma once

#include "func.h"
#include "Logout.h"

/*
* 命令
* @param EMPTY 空命令
* @param EXIT 退出
* @param BREAK 返回
* @param OPEN_WORLD
* @param OPEN_PLAYER
* @param LIST_WORLD
* @param LIST_PLAYER
* @param DEL_PLAYER
* @param DEL_WORLD
* @param DEL_PW
* @param DEL_JS
* @param NULL_BACK
* @param REFRESH
*/
enum Command
{
	//空命令
	EMPTY_COM,
	//退出
	EXIT,
	//返回
	BREAK,
	//open world
	OPEN_WORLD,
	//open player
	OPEN_PLAYER,
	//list world
	LIST_WORLD,
	//list player
	LIST_PLAYER,
	//del player
	DEL_PLAYER,
	//del world
	DEL_WORLD,
	//del pw
	DEL_PW,
	//del js
	DEL_JS,
	//unknown command
	NULL_BACK,
	//refresh
	REFRESH
};

class p_mpm
{
public:
	inline p_mpm() { this->input_path = {}; this->Processed_input_path = {}; this->uct_world_list = {}; this->user_list = {}; this->world_list = {}; this->load_type = {}; this->CommandStr = {}; }//初始化所有变量
	~p_mpm() = default;

	/*
	* 设置输入路径
	* @param _path 路径参数
	*/
	inline void SetInputPath(const std::string _path) { this->input_path = _path; };
	/*
	* 获取输入路径
	* @return 用户输入的路径，由 SetInputPath(const std::string) 设置
	* @throws NullString 空字符串
	*/
	inline const std::string GetInputPath(void) { if (!this->input_path.empty()) { return this->input_path; } throw NullString(); }
	/*
	* 获取处理后的输入路径
	* @return 处理后的路径字符串
	* @throws NullString 空字符串
	*/
	inline const std::string GetProcessingPath(void) { if (this->Processed_input_path.empty()) { throw NullString(); } return this->Processed_input_path; }
	/*
	* 获取世界列表容器结构体
	* @return 世界列表容器结构体
	* @throws NullStruct 空结构体
	*/
	inline const WorldDirectoriesNameList GetWorldList(void) { if (this->uct_world_list.world_directory_list.size() == 0 && this->uct_world_list.world_name_list.size() == 0) { throw NullStruct(); } return this->uct_world_list; }
	/*
	* 获取世界列表结构体容器
	* @return 世界列表结构体容器
	* @throw NullStruct 空结构体
	*/
	inline const std::vector<WorldDirectoriesName> GetSTLWorldList(void) { if (this->world_list.size() == 0) { throw NullVector(); } return this->world_list; }
	/*
	* 获取玩家列表结构体容器
	* @return 玩家列表结构体容器
	* @throw NullStruct 空结构体
	*/
	inline const std::vector<UserInfo> GetUserInfoList(void) { if (this->user_list.size() == 0) { throw NullVector(); } return this->user_list; }
	/*
	* 获取路径加载模式
	* @return 路径加载模式
	* @throws TypeError 未定义加载模式
	*/
	inline const LoadMode GetPathLoadType() { if (this->load_type == LoadMode::EMPTY) { throw TypeError(); } return this->load_type; }
	/*
	* 获取最后一条命令
	* @return 命令字符串
	*/
	inline const std::string GetLastCommand() { return this->CommandStr; }
 
	//处理输入路径
	void ProcessingPath(void);
	/*
	* 处理输入路径
	* @param _input_path 输入路径
	*/
	void ProcessingPath(const std::string _input_path);
	//检测加载方式
	void PathLoadTpye(void);
	/*
	* 检测加载方式
	* @param _warld_path 处理后的输入路径
	*/
	void PathLoadTpye(const std::string _warld_path);
	//加载世界列表
	void LoadWorldList(void);
	/*
	* 加载世界列表
	* @param _world_path 处理后的输入路径
	*/
	void LoadWorldList(const std::string _world_path);
	//加载世界列表
	void LoadWorldListSTL(void);
	/*
	* 加载世界列表
	* @param _world_path 处理后的输入路径
	*/
	void LoadWorldListSTL(const std::string _world_path);
	//加载玩家列表
	void LoadUserList(void);
	/*
	* 加载玩家列表
	* @param _JSON_path 处理后的客户端路径
	*/
	void LoadUserList(const std::string _JSON_path);
	//重新加载user、world列表
	void ReloadList(void);
	/*
	* 处理命令
	* @param _command 用户输入的完整命令
	*/
	int ProcessCommand(const std::string _command);
	virtual void RunCommand() {};

protected:
	/*
	* 设置处理后的路径
	* @param _processing_path 处理后的路径
	* @throws NullString 空字符串
	*/
	inline void SetProcessingPath(std::string _processing_path) { if (_processing_path.empty()) { throw NullString(); } this->Processed_input_path = _processing_path; }
	/*
	* 设置世界列表容器结构体
	* @param _uct_world_list 世界列表容器结构体
	* @throws NullStruct 空结构体
	*/
	inline void SetWorldList(WorldDirectoriesNameList _uct_world_list) { if (_uct_world_list.world_directory_list.size() == 0 && _uct_world_list.world_name_list.size() == 0) { throw NullStruct(); } this->uct_world_list = _uct_world_list; }
	/*
	* 设置世界列表结构体容器
	* @param _world_list 世界列表结构体容器
	* @throws NullVector 空容器
	*/
	inline void SetSTLWorldList(std::vector<WorldDirectoriesName> _world_list) { if (_world_list.size() == 0) { throw NullVector(); } this->world_list = _world_list; }
	/*
	* 设置玩家列表结构体容器
	* @param _user_list 玩家列表结构体容器
	* @throws NullVector 空容器
	*/
	inline void SetUserInfoList(std::vector<UserInfo> _user_list) { if (_user_list.size() == 0) { throw NullVector(); } this->user_list = _user_list; }
	/*
	* 设置路径加载模式
	* @param type 路径加载模式
	* @throws TypeError 类型错误
	*/
	inline void SetPathLoadType(LoadMode type) { if (type == LoadMode::EMPTY) { throw TypeError(); } this->load_type = type; }
	/*
	* 设置命令参数
	* @param _command 命令参数
	*/
	inline void SetLastCommand(const std::string _command) { this->CommandStr = _command; }

private:
	//输入路径
	std::string input_path;
	//处理后路径
	std::string Processed_input_path;
	//世界列表
	WorldDirectoriesNameList uct_world_list;
	//世界列表容器
	std::vector<WorldDirectoriesName> world_list;
	//用户数据列表
	std::vector<UserInfo> user_list;
	//路径加载类型
	LoadMode load_type;
	//指令参数
	std::string CommandStr;
};
