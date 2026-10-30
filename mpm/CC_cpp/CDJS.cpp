//实现CDJS类
#include "CDJS.h"

void CDJS::RunCommand()
{
	std::string out;

	if (GetLastCommand() == "_ALL_PJS_")
	{
		if (MoveToRecycleBinWithPS(GetProcessingPath() + "\\usercache.json") && MoveToRecycleBinWithPS(GetProcessingPath() + "\\usernamecache.json"))
		{
			out += "已删除全部玩家名称缓存";
		}
		else
		{
			out += "删除全部缓存失败";
		}
	}
	else if (DeletePlayerJSON(GetProcessingPath(), GetLastCommand()))
	{
		out += "删除：" + GetLastCommand();
	}
	else
	{
		out += "删除失败或只删了其中一个";
	}

	SetShow(out);
	return;
}
