//p_mpm.cpp 实现p_mpm执行类
#include "p_mpm.h"

p_mpm::p_mpm()
{
	//初始化所有变量
	this->input_path = {};
	this->Processed_input_path = {};
	this->uct_world_list = {};
	this->user_list = {};
	this->world_list = {};
	this->load_type = {};
}

void p_mpm::ProcessingPath()
{
	if (this->input_path.empty())
	{
		throw NullString();
	}

	try
	{
		this->SetProcessingPath(ProcessingInputPath(this->input_path));
	}
	catch (const std::exception& e)
	{
		throw e;
	}
}

void p_mpm::ProcessingPath(const std::string _input_path)
{
	if (_input_path.empty())
	{
		throw NullString();
	}

	try
	{
		this->SetProcessingPath(ProcessingInputPath(_input_path));
	}
	catch (const std::exception& e)
	{
		throw e;
	}
}

void p_mpm::PathLoadTpye()
{
	try
	{
		if (folderExists(GetProcessingPath(), "saves"))
		{
			this->SetPathLoadType(MOD_CLIENT);
		}
		else
		{
			this->SetPathLoadType(MOD_SERVER);
		}
	}
	catch (const std::exception& e)
	{
		throw e;
	}
}

void p_mpm::PathLoadTpye(const std::string _warld_path)
{
	if (_warld_path.empty())
	{
		throw NullString();
	}

	try
	{
		if (folderExists(_warld_path, "saves"))
		{
			this->SetPathLoadType(MOD_CLIENT);
		}
		else
		{
			this->SetPathLoadType(MOD_SERVER);
		}
	}
	catch (const std::exception& e)
	{
		throw e;
	}
}

void p_mpm::LoadWorldList(void)
{
	WorldDirectoriesNameList wnl = {};

	if (GetPathLoadType() == MOD_CLIENT)
	{
		wnl = GetWorldDirectoriesList(GetProcessingPath(), MOD_CLIENT);
	}
	else
	{
		wnl = GetWorldDirectoriesList(GetProcessingPath(), MOD_SERVER);
	}

	if (wnl.world_directory_list.size() == 0 && wnl.world_name_list.size() == 0)
	{
		throw NullStruct();
	}

	this->SetWorldList(wnl);
}

void p_mpm::LoadWorldList(const std::string _world_path)
{
	if (_world_path.empty())
	{
		throw NullString();
	}

	WorldDirectoriesNameList wnl = {};

	if (GetPathLoadType() == MOD_CLIENT)
	{
		wnl = GetWorldDirectoriesList(_world_path, MOD_CLIENT);
	}
	else
	{
		wnl = GetWorldDirectoriesList(_world_path, MOD_SERVER);
	}

	if (wnl.world_directory_list.size() == 0 && wnl.world_name_list.size() == 0)
	{
		throw NullStruct();
	}

	this->SetWorldList(wnl);
}

void p_mpm::LoadUserList(void)
{
	std::vector<UserInfo> uil = {};

	try
	{
		uil = GetUserInfo(GetProcessingPath());
		if (uil.empty())
		{
			throw NoUserInfo();
		}

		this->SetUserInfoList(uil);
	}
	catch (const std::exception& e)
	{
		throw e;
	}
}

void p_mpm::LoadUserList(const std::string _JSON_path)
{
	if (_JSON_path.empty())
	{
		throw NullString();
	}

	std::vector<UserInfo> uil = {};

	try
	{
		uil = GetUserInfo(GetProcessingPath());
		if (uil.empty())
		{
			throw NoUserInfo();
		}

		this->SetUserInfoList(uil);
	}
	catch (const std::exception& e)
	{
		throw e;
	}
}

void p_mpm::ReloadList(void)
{
	try
	{
		this->LoadWorldList();
		this->LoadUserList();
	}
	catch (const std::exception& e)
	{
		throw e;
	}
}

int p_mpm::ProcessCommand(const std::string _command)
{
	if (_command == "exit")
	{
		return COMMAND_EXIT;
	}

	if (_command == "break")
	{
		return COMMAND_BREAK;
	}

	std::string l1 = _command.substr(0, 4), l2 = {}, l3 = {}, l4 = {};
	if (l1 == "open")
	{
		try
		{
			l2 = _command.substr(5,5);
		}
		catch (const std::exception&)
		{
			throw CommandError();
		}

		if (l2 == "world")
		{
			try
			{
				l3 = _command.substr(11);
			}
			catch (const std::exception&)
			{
				throw CommandError();
			}

			//TODO 返回命令类别，并保存参数
		}
	}
}
