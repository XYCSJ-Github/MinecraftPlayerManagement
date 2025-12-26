//p_mpm.cpp 实现p_mpm执行类
#include "p_mpm.h"

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

	try
	{
		if (GetPathLoadType() == MOD_CLIENT)
		{
			wnl = GetWorldDirectoriesList(GetProcessingPath(), MOD_CLIENT);
		}
		else
		{
			wnl = GetWorldDirectoriesList(GetProcessingPath(), MOD_SERVER);
		}
	}
	catch (const std::exception& e)
	{
		throw e;
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

void p_mpm::LoadWorldListSTL(void)
{
	std::vector<WorldDirectoriesName> wdnl = {};

	try
	{
		if (true)
		{
			if (GetPathLoadType() == MOD_CLIENT)
			{
				wdnl = GetWorldDirectories(GetProcessingPath(), MOD_CLIENT);
			}
			else
			{
				wdnl = GetWorldDirectories(GetProcessingPath(), MOD_SERVER);
			}
		}
	}
	catch (const std::exception& e)
	{
		throw e;
	}


	if (wdnl.size() == 0 && wdnl.size() == 0)
	{
		throw NullVector();
	}

	this->SetSTLWorldList(wdnl);
}

void p_mpm::LoadWorldListSTL(const std::string _world_path)
{
	std::vector<WorldDirectoriesName> wdnl = {};

	try
	{
		if (true)
		{
			if (GetPathLoadType() == MOD_CLIENT)
			{
				wdnl = GetWorldDirectories(_world_path, MOD_CLIENT);
			}
			else
			{
				wdnl = GetWorldDirectories(_world_path, MOD_SERVER);
			}
		}
	}
	catch (const std::exception& e)
	{
		throw e;
	}


	if (wdnl.size() == 0 && wdnl.size() == 0)
	{
		throw NullVector();
	}

	this->SetSTLWorldList(wdnl);
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
	std::string l1 = {}, l2 = {}, l3 = {}, l4 = {};

	l1 = _command.substr(0, 4);
	if (_command == "exit")
	{
		return COMMAND_EXIT;
	}

	l1 = _command.substr(0, 5);
	if (_command == "break")
	{
		return COMMAND_BREAK;
	}

	l1 = _command.substr(0, 4);
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
				this->SetLastCommand(_command.substr(11));
			}
			catch (const std::exception&)
			{
				throw CommandError();
			}

			return COMMAND_OPEN_WORLD;
		}

		if (l2 == "player")
		{
			try
			{
				this->SetLastCommand(_command.substr(12));
			}
			catch (const std::exception&)
			{
				throw CommandError();
			}

			return COMMAND_OPEN_PLAYER;
		}
	}

	l1 = _command.substr(0, 4);
	if (l1 == "list")
	{
		try
		{
			l2 = _command.substr(6);
		}
		catch (const std::exception&)
		{
			throw CommandError();
		}
		if (l2 == "player")
		{
			return COMMAND_LIST_PLAYER;
		}

		try
		{
			l2 = _command.substr(5);
		}
		catch (const std::exception&)
		{
			throw CommandError();
		}
		if (l2 == "world")
		{
			return COMMAND_LIST_WORLD;
		}
	}

	l1 = _command.substr(0, 6);
	if (l1 == "delete")
	{
		try
		{
			l2 = _command.substr(7, 6);
		}
		catch (const std::exception&)
		{
			throw CommandError();
		}

		if (l2 == "player")
		{
			try
			{
				this->SetLastCommand(_command.substr(14));
			}
			catch (const std::exception&)
			{
				throw CommandError();
			}

			return COMMAND_DEL_PLAYER;
		}

		try
		{
			l2 = _command.substr(7, 5);
		}
		catch (const std::exception&)
		{
			throw CommandError();
		}
		if (l2 == "world")
		{
			try
			{
				this->SetLastCommand(_command.substr(13));
			}
			catch (const std::exception&)
			{
				throw CommandError();
			}

			return COMMAND_DEL_WORLD;
		}

		try
		{
			l2 = _command.substr(7, 2);
		}
		catch (const std::exception&)
		{
			throw CommandError();
		}
		if (l2 == "pw")
		{
			try
			{
				this->SetLastCommand(_command.substr(9));
			}
			catch (const std::exception&)
			{
				throw CommandError();
			}

			return COMMAND_DEL_PW;
		}

		try
		{
			l2 = _command.substr(7, 2);
		}
		catch (const std::exception&)
		{
			throw CommandError();
		}
		if (l2 == "js")
		{
			try
			{
				this->SetLastCommand(_command.substr(9));
			}
			catch (const std::exception&)
			{
				throw CommandError();
			}

			return COMMAND_DEL_JS;
		}
	}

	return COMMAND_NULL_BACK;
}
